using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

//Duo mode in the Game scene (added by MultiplayerLobby when a session is active):
//  - holds the level frozen until both players have loaded the song, then starts both countdowns together
//  - shows the partner's live score and the team total
//  - mirrors pause/resume between the two PCs
public class DuoGame : MonoBehaviour
{
    //True while waiting for the partner to load; the level is frozen and Esc does nothing
    public static bool Waiting { get; private set; }

    //Host gives up waiting after this long and starts anyway
    private const float MaxWait = 20f;
    private const float ScoreInterval = 0.2f;

    private Pause pauseMenu;
    private Canvas canvas;
    private TextMeshProUGUI waitText, partnerText, teamText;
    private bool sentReady;
    private float waitStarted, nextScore;
    private int shownPartner = -1, shownTeam = -1;
    private bool shownConnected;

    void Awake()
    {
        //Nothing that runs on scaled time (countdown, video start, notes) moves until Go()
        Waiting = true;
        Time.timeScale = 0f;
        waitStarted = Time.unscaledTime;
        NetSession.OnMessage += OnMessage;
        BuildHud();
    }

    void Start()
    {
        pauseMenu = FindFirstObjectByType<Pause>(FindObjectsInactive.Include);
    }

    void OnDestroy()
    {
        Waiting = false;
        NetSession.OnMessage -= OnMessage;
    }

    void Update()
    {
        if (Waiting)
        {
            if (!sentReady && playMusic.Prepared)
            {
                sentReady = true;
                NetSession.Send("READY");
            }
            //Host starts both once both are loaded. If the partner is gone, just play solo.
            bool partnerGone = !NetSession.Connected;
            if (NetSession.IsHost && sentReady && (NetSession.PartnerReady || partnerGone || Time.unscaledTime - waitStarted > MaxWait))
            {
                NetSession.Send("GO");
                Go();
            }
            else if (!NetSession.IsHost && partnerGone)
            {
                Go();
            }
        }
        else if (NetSession.Connected && Time.unscaledTime >= nextScore)
        {
            nextScore = Time.unscaledTime + ScoreInterval;
            NetSession.SendScore();
        }
        UpdateHud();
    }

    void Go()
    {
        if (!Waiting) return;
        Waiting = false;
        Time.timeScale = 1f;
    }

    void OnMessage(string verb, string payload)
    {
        switch (verb)
        {
            case "GO":
                Go();
                break;
            case "MISSING":
                //The partner doesn't have this level: back to the lobby, which shows why
                Waiting = false;
                Time.timeScale = 1f;
                MultiplayerLobby.Notice = "Your partner doesn't have \"" + payload + "\". Pick another song (or send them the level folder).";
                MultiplayerLobby.OpenOnLoad = true;
                SceneManager.LoadScene("Menu");
                break;
            case "PAUSE":
                if (pauseMenu != null && !Pause.isPaused && !Waiting) pauseMenu.pause(false);
                break;
            case "RESUME":
                if (pauseMenu != null && Pause.isPaused) pauseMenu.resume(false);
                break;
        }
    }

    //---------------- HUD ----------------

    void BuildHud()
    {
        canvas = DuoUI.Canvas("DuoHud", 1);
        Transform root = canvas.transform;
        waitText = DuoUI.Label(root, "Waiting for your partner to load the song...", 0, 120, 1200, 80, 40, Theme.Text, true);
        string role = NetSession.RoleName(NetSession.MyRole).ToUpperInvariant();
        string partnerRole = NetSession.RoleName(NetSession.PartnerRole).ToUpperInvariant();
        DuoUI.Label(root, "YOU: " + role, 0, 500, 600, 36, 22, Theme.Accent, true);
        partnerText = DuoUI.Label(root, "", 0, 468, 900, 34, 24, Theme.TextMuted);
        teamText = DuoUI.Label(root, "", 0, 434, 900, 36, 28, Theme.Text, true);
        partnerText.text = "PARTNER (" + partnerRole + "): 0";
    }

    void UpdateHud()
    {
        //The pause menu is on a scene canvas underneath this one
        canvas.enabled = !Pause.isPaused || Waiting;
        waitText.gameObject.SetActive(Waiting);
        int team = ScoreKeeper.score + NetSession.PartnerScore;
        if (NetSession.PartnerScore != shownPartner || NetSession.Connected != shownConnected)
        {
            shownPartner = NetSession.PartnerScore;
            shownConnected = NetSession.Connected;
            string partnerRole = NetSession.RoleName(NetSession.PartnerRole).ToUpperInvariant();
            string combo = NetSession.PartnerCombo > 2 ? "   x" + NetSession.PartnerCombo : "";
            partnerText.text = NetSession.Connected
                ? "PARTNER (" + partnerRole + "): " + DuoUI.Thousands(shownPartner) + combo
                : "PARTNER DISCONNECTED";
        }
        if (team != shownTeam)
        {
            shownTeam = team;
            teamText.text = "TEAM " + DuoUI.Thousands(team);
        }
    }
}
