using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;

//Online duo mode: two PCs, one plays the vocals chart (notes.csv), the other the instrumental (drums.csv).
//Each player only sees their own notes, so the only things sent are: which song, when to start,
//pause/resume, and scores. That keeps it to one small TCP connection (host/join by IP, no packages).
//
//Protocol: one line of UTF-8 text per message, "VERB payload"
//    HELLO <version>        first thing both sides send; versions must match
//    ROLE <vocals|instrumental>   the sender's role (the receiver takes the other one)
//    START <song>           host -> client: load this level now
//    MISSING <song>         client -> host: I don't have that level
//    READY                  this side's level is loaded and its video prepared
//    GO                     host -> client: both ready, start the countdown
//    PAUSE / RESUME         either side paused or resumed (the other follows)
//    SCORE <score> <combo> <participation>   live, a few times a second
//    FINAL <score> <highestCombo> <participation>   song finished
//    LOBBY                  the sender quit to the lobby; the receiver goes there too
//    PING                   keep-alive
//    BYE                    leaving
public class NetSession : MonoBehaviour
{
    public enum Role { Vocals, Instrumental }
    public enum State { Offline, Hosting, Connecting, Connected }

    public const int DefaultPort = 7777;
    private const int ProtocolVersion = 1;
    private const float PingInterval = 2f;
    private const float Timeout = 10f;

    //---------------- Public state (main thread only) ----------------

    public static State Status { get; private set; } = State.Offline;
    public static bool IsHost { get; private set; }
    public static bool Connected => Status == State.Connected;
    //True while a duo session exists (even if the partner just dropped), so the game keeps duo rules
    public static bool Active => Status != State.Offline;
    public static Role MyRole { get; private set; } = Role.Vocals;
    public static Role PartnerRole => MyRole == Role.Vocals ? Role.Instrumental : Role.Vocals;
    //Human-readable line for the lobby ("Waiting for a player on 192.168.1.5:7777", errors, ...)
    public static string Message { get; private set; } = "";

    //Partner's latest numbers
    public static int PartnerScore, PartnerCombo;
    public static double PartnerParticipation;
    public static bool PartnerReady;
    public static bool PartnerFinished;
    public static int PartnerFinalScore, PartnerFinalCombo;
    public static double PartnerFinalParticipation;

    //Fired on the main thread for every message (verb, payload); DuoGame/lobby listen to this
    public static event Action<string, string> OnMessage;
    //Fired when the connection is made or lost
    public static event Action OnStatusChanged;

    //---------------- Internals ----------------

    private static NetSession instance;
    private static readonly ConcurrentQueue<string> inbox = new ConcurrentQueue<string>();
    private static readonly ConcurrentQueue<string> events = new ConcurrentQueue<string>();
    private static readonly object writeLock = new object();
    private static TcpListener listener;
    private static TcpClient client;
    private static StreamWriter writer;
    private static Thread worker;
    //Bumped on every Close, so a reader thread from an old connection can't report on a new one
    private static volatile int generation;
    private static float lastReceived, lastPing;

    public static string RoleName(Role r) => r == Role.Vocals ? "Vocals" : "Instrumental";

    static void EnsureInstance()
    {
        if (instance != null) return;
        GameObject go = new GameObject("NetSession");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<NetSession>();
    }

    //Wait for one player to join on this port
    public static void Host(int port = DefaultPort)
    {
        Leave();
        EnsureInstance();
        IsHost = true;
        ResetPartner();
        try
        {
            listener = new TcpListener(IPAddress.Any, port);
            listener.Start();
        }
        catch (Exception e)
        {
            listener = null;
            SetStatus(State.Offline, "Couldn't host on port " + port + ": " + e.Message);
            return;
        }
        SetStatus(State.Hosting, "Waiting for a player. Your address: " + string.Join("  or  ", LocalAddresses()) + "  (port " + port + ")");
        int gen = generation;
        TcpListener l = listener;
        worker = new Thread(() =>
        {
            try
            {
                TcpClient c = l.AcceptTcpClient();
                l.Stop();
                Attach(c, gen);
            }
            catch (Exception e)
            {
                if (gen == generation) events.Enqueue("ERROR Hosting stopped: " + e.Message);
            }
        }) { IsBackground = true, Name = "NetSession host" };
        worker.Start();
    }

    //Connect to a host. address is "ip" or "ip:port"
    public static void Join(string address)
    {
        Leave();
        EnsureInstance();
        IsHost = false;
        ResetPartner();
        string host = (address ?? "").Trim();
        int port = DefaultPort;
        int colon = host.LastIndexOf(':');
        if (colon > 0 && host.IndexOf(':') == colon && int.TryParse(host.Substring(colon + 1), out int p))
        {
            port = p;
            host = host.Substring(0, colon);
        }
        if (host.Length == 0)
        {
            SetStatus(State.Offline, "Type the host's address first.");
            return;
        }
        SetStatus(State.Connecting, "Connecting to " + host + ":" + port + "...");
        int gen = generation;
        worker = new Thread(() =>
        {
            try
            {
                TcpClient c = new TcpClient();
                IAsyncResult r = c.BeginConnect(host, port, null, null);
                if (!r.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(6)) || !c.Connected)
                {
                    c.Close();
                    if (gen == generation) events.Enqueue("ERROR Couldn't reach " + host + ":" + port + ". Check the address, and that the host's firewall/router lets the port through.");
                    return;
                }
                c.EndConnect(r);
                Attach(c, gen);
            }
            catch (Exception e)
            {
                if (gen == generation) events.Enqueue("ERROR Couldn't connect: " + e.Message);
            }
        }) { IsBackground = true, Name = "NetSession join" };
        worker.Start();
    }

    //Close everything and go back to solo play
    public static void Leave()
    {
        if (Status == State.Offline && client == null && listener == null) return;
        Send("BYE");
        Close();
        SetStatus(State.Offline, "");
    }

    public static void Send(string verb, string payload = null)
    {
        StreamWriter w = writer;
        if (w == null) return;
        try
        {
            lock (writeLock)
            {
                w.Write(payload == null ? verb : verb + " " + payload.Replace('\n', ' ').Replace('\r', ' '));
                w.Write('\n');
                w.Flush();
            }
        }
        catch (Exception)
        {
            events.Enqueue("LOST");
        }
    }

    //Pick this side's role; the partner takes the other one
    public static void SetRole(Role role)
    {
        MyRole = role;
        Send("ROLE", role == Role.Vocals ? "vocals" : "instrumental");
        OnStatusChanged?.Invoke();
    }

    //Host: start a level on both PCs
    public static void StartSong(string song)
    {
        if (!IsHost) return;
        PartnerReady = false;
        PartnerFinished = false;
        Send("START", song);
    }

    public static void SendScore() => Send("SCORE", ScoreKeeper.score + " " + ScoreKeeper.combo + " " + ScoreKeeper.participation.ToString(System.Globalization.CultureInfo.InvariantCulture));
    public static void SendFinal() => Send("FINAL", ScoreKeeper.score + " " + ScoreKeeper.highestCombo + " " + ScoreKeeper.participation.ToString(System.Globalization.CultureInfo.InvariantCulture));

    //IPv4 addresses of this PC (to tell the other player), e.g. 192.168.1.5
    public static List<string> LocalAddresses()
    {
        var list = new List<string>();
        try
        {
            foreach (IPAddress a in Dns.GetHostAddresses(Dns.GetHostName()))
                if (a.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(a)) list.Add(a.ToString());
        }
        catch (Exception) { }
        if (list.Count == 0) list.Add("127.0.0.1");
        return list;
    }

    //---------------- Connection ----------------

    //Runs on the worker thread once a socket is connected: set up writing, then read until it closes
    static void Attach(TcpClient c, int gen)
    {
        if (gen != generation)
        {
            c.Close();
            return;
        }
        client = c;
        c.NoDelay = true;
        NetworkStream stream = c.GetStream();
        writer = new StreamWriter(stream, new UTF8Encoding(false));
        events.Enqueue("CONNECTED");
        Send("HELLO", ProtocolVersion.ToString());
        try
        {
            using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
            {
                string line;
                while (gen == generation && (line = reader.ReadLine()) != null)
                {
                    if (line.Length > 0) inbox.Enqueue(line);
                }
            }
        }
        catch (Exception) { }
        if (gen == generation) events.Enqueue("LOST");
    }

    static void Close()
    {
        generation++;
        try { listener?.Stop(); } catch (Exception) { }
        try { client?.Close(); } catch (Exception) { }
        listener = null;
        client = null;
        writer = null;
        worker = null;
        while (inbox.TryDequeue(out _)) { }
        while (events.TryDequeue(out _)) { }
    }

    static void SetStatus(State s, string message)
    {
        Status = s;
        Message = message;
        OnStatusChanged?.Invoke();
    }

    static void ResetPartner()
    {
        PartnerScore = PartnerCombo = 0;
        PartnerParticipation = 0;
        PartnerReady = PartnerFinished = false;
    }

    void Update()
    {
        while (events.TryDequeue(out string e))
        {
            if (e == "CONNECTED")
            {
                lastReceived = lastPing = Time.unscaledTime;
                SetStatus(State.Connected, IsHost ? "A player joined." : "Connected to the host.");
                //Host's role decides; the joiner takes whatever is left
                if (IsHost) SetRole(MyRole);
            }
            else if (e == "LOST")
            {
                if (Status == State.Offline) continue;
                Close();
                SetStatus(State.Offline, "Lost connection to your partner.");
            }
            else if (e.StartsWith("ERROR "))
            {
                Close();
                SetStatus(State.Offline, e.Substring(6));
            }
        }

        while (inbox.TryDequeue(out string line))
        {
            lastReceived = Time.unscaledTime;
            int space = line.IndexOf(' ');
            string verb = space < 0 ? line : line.Substring(0, space);
            string payload = space < 0 ? "" : line.Substring(space + 1);
            Handle(verb, payload);
            if (Status == State.Offline) break;
        }

        if (Connected)
        {
            if (Time.unscaledTime - lastPing >= PingInterval)
            {
                lastPing = Time.unscaledTime;
                Send("PING");
            }
            if (Time.unscaledTime - lastReceived > Timeout)
            {
                Close();
                SetStatus(State.Offline, "Your partner stopped responding.");
            }
        }
    }

    static void Handle(string verb, string payload)
    {
        switch (verb)
        {
            case "HELLO":
                if (payload != ProtocolVersion.ToString())
                {
                    Send("BYE");
                    Close();
                    SetStatus(State.Offline, "Your partner has a different version of the game.");
                    return;
                }
                break;
            case "ROLE":
                MyRole = payload == "vocals" ? Role.Instrumental : Role.Vocals;
                OnStatusChanged?.Invoke();
                break;
            case "START":
                if (!IsHost) StartFromHost(payload);
                break;
            case "READY":
                PartnerReady = true;
                break;
            case "SCORE":
                ParseScore(payload, out PartnerScore, out PartnerCombo, out PartnerParticipation);
                break;
            case "FINAL":
                ParseScore(payload, out PartnerFinalScore, out PartnerFinalCombo, out PartnerFinalParticipation);
                PartnerScore = PartnerFinalScore;
                PartnerFinished = true;
                break;
            case "LOBBY":
                if (SceneManager.GetActiveScene().name != "Menu")
                {
                    Time.timeScale = 1f;
                    MultiplayerLobby.OpenOnLoad = true;
                    SceneManager.LoadScene("Menu");
                }
                break;
            case "BYE":
                Close();
                SetStatus(State.Offline, "Your partner left.");
                return;
        }
        OnMessage?.Invoke(verb, payload);
    }

    //Client: the host picked a song. Load it if we have it, otherwise tell the host.
    static void StartFromHost(string song)
    {
        if (!LevelHolder.levels.ContainsKey(song))
        {
            LevelHolder.Rescan();
            if (!LevelHolder.levels.ContainsKey(song))
            {
                Send("MISSING", song);
                Message = "The host picked \"" + song + "\", which you don't have.";
                OnStatusChanged?.Invoke();
                return;
            }
        }
        PartnerReady = false;
        PartnerFinished = false;
        SongSelect2.theSong = song;
        ScoreKeeper.combo = 0;
        Time.timeScale = 1f;
        SceneManager.LoadScene("Game");
    }

    static void ParseScore(string payload, out int score, out int combo, out double participation)
    {
        string[] p = payload.Split(' ');
        score = combo = 0;
        participation = 0;
        if (p.Length > 0) int.TryParse(p[0], out score);
        if (p.Length > 1) int.TryParse(p[1], out combo);
        if (p.Length > 2) double.TryParse(p[2], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out participation);
    }

    void OnApplicationQuit()
    {
        Leave();
    }
}
