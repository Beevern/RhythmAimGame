using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

//Duo mode lobby, opened from the main menu's "Multiplayer" button: host or join, pick roles, then the
//host picks a song in Song Select. Also adds DuoGame to the Game scene while a session is active.
public class MultiplayerLobby : MonoBehaviour
{
    //Open the lobby the next time the menu loads (after leaving a duo song)
    public static bool OpenOnLoad;
    //One-off message shown in the lobby (e.g. the partner is missing a level)
    public static string Notice;

    private const string AddressPref = "duoAddress";

    private static MultiplayerLobby open;
    private Canvas canvas;
    private RectTransform card;
    private NetSession.State builtFor = (NetSession.State)(-1);
    private NetSession.Role builtRole;
    private TMP_InputField address;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
        SceneManager.sceneLoaded += (scene, mode) => OnSceneLoaded(scene);
        OnSceneLoaded(SceneManager.GetActiveScene());
    }

    static void OnSceneLoaded(Scene scene)
    {
        if (scene.name == "Menu")
        {
            if (OpenOnLoad || NetSession.Active) Open();
            OpenOnLoad = false;
            return;
        }
        if (!NetSession.Active) return;
        if (FindFirstObjectByType<ScoreKeeper>(FindObjectsInactive.Include) != null && FindFirstObjectByType<DuoGame>() == null)
            new GameObject("DuoGame").AddComponent<DuoGame>();
        //(The results screen adds its own DuoResults card; GameOver sends the final score)
    }

    //Main menu: a "Multiplayer" button, copied from the Level Maker button, under Level Editor (see UIThemer)
    public static void AddMenuButton()
    {
        GameObject maker = GameObject.Find("LevelMaker");
        if (maker == null || GameObject.Find("MultiplayerButton") != null) return;
        GameObject copy = Instantiate(maker, maker.transform.parent);
        copy.name = "MultiplayerButton";
        //Its SwitchScene would load the Level Maker; remove it before it starts
        SwitchScene sw = copy.GetComponent<SwitchScene>();
        if (sw != null) DestroyImmediate(sw);
        Button button = copy.GetComponentInChildren<Button>(true);
        if (button != null)
        {
            button.onClick = new Button.ButtonClickedEvent();
            button.onClick.AddListener(Open);
        }
        TMP_Text label = copy.GetComponentInChildren<TMP_Text>(true);
        if (label != null) label.text = ""; //No text will show rn
        RectTransform rt = (RectTransform)copy.transform;
        rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, -130f);
    }

    public static void Open()
    {
        if (open != null) return;
        open = new GameObject("MultiplayerLobby").AddComponent<MultiplayerLobby>();
    }

    void Awake()
    {
        canvas = DuoUI.Canvas("MultiplayerLobby", 50);
        Image dim = UIFactory.FullScreenImage(canvas.transform, Theme.Overlay);
        dim.raycastTarget = true;
        NetSession.OnStatusChanged += Rebuild;
    }

    void OnDestroy()
    {
        NetSession.OnStatusChanged -= Rebuild;
        if (canvas != null) Destroy(canvas.gameObject);
        if (open == this) open = null;
    }

    void Update()
    {
        if (NetSession.Status != builtFor || NetSession.MyRole != builtRole) Rebuild();
        if (Input.GetKeyDown(KeyCode.Escape)) Destroy(gameObject);
        if (address != null && address.isFocused && Input.GetKeyDown(KeyCode.Return)) Join();
    }

    void Rebuild()
    {
        if (this == null || canvas == null) return;
        builtFor = NetSession.Status;
        builtRole = NetSession.MyRole;
        if (card != null) Destroy(card.gameObject);
        address = null;

        card = DuoUI.Panel(canvas.transform, 0, 0, 820, 620, Theme.Surface).rectTransform;
        DuoUI.Label(card, "DUO MODE", 0, 250, 700, 60, 44, Theme.Text, true);
        DuoUI.Label(card, "Two PCs, one song. One of you plays the vocals, the other plays the instrumental (the drums). You each only see your own notes.",
            0, 185, 720, 70, 22, Theme.TextMuted);
        DuoUI.Button(card, "X", 370, 270, 48, 48, false, () => Destroy(gameObject));

        switch (NetSession.Status)
        {
            case NetSession.State.Offline: BuildOffline(); break;
            case NetSession.State.Hosting:
            case NetSession.State.Connecting: BuildWaiting(); break;
            case NetSession.State.Connected: BuildConnected(); break;
        }

        string message = !string.IsNullOrEmpty(Notice) ? Notice : NetSession.Message;
        DuoUI.Label(card, message, 0, -250, 740, 70, 20, Theme.TextMuted);
    }

    void BuildOffline()
    {
        DuoUI.Label(card, "HOST A GAME", 0, 105, 700, 36, 22, Theme.Text, true);
        DuoUI.Button(card, "HOST", 0, 55, 300, 64, true, () => { Notice = null; NetSession.Host(); });

        DuoUI.Label(card, "OR JOIN A FRIEND", 0, -35, 700, 36, 22, Theme.Text, true);
        address = DuoUI.Input(card, "Host's address, e.g. 192.168.1.5", PlayerPrefs.GetString(AddressPref, ""), -90, -95, 480, 60);
        DuoUI.Button(card, "JOIN", 250, -95, 180, 60, false, Join);
        DuoUI.Label(card, "Same Wi-Fi works as is. Over the internet the host needs port " + NetSession.DefaultPort +
            " forwarded, or you both use a virtual LAN (Tailscale, ZeroTier, Radmin VPN).", 0, -170, 720, 60, 18, Theme.TextMuted);
    }

    void Join()
    {
        if (address == null) return;
        Notice = null;
        PlayerPrefs.SetString(AddressPref, address.text.Trim());
        NetSession.Join(address.text);
    }

    void BuildWaiting()
    {
        bool hosting = NetSession.Status == NetSession.State.Hosting;
        DuoUI.Label(card, hosting ? "WAITING FOR A PLAYER..." : "CONNECTING...", 0, 70, 700, 50, 30, Theme.Text, true);
        if (hosting)
            DuoUI.Label(card, "Tell your friend to press JOIN and type:\n<b>" + string.Join("</b>  or  <b>", NetSession.LocalAddresses()) + "</b>",
                0, -10, 720, 90, 26, Theme.Text);
        DuoUI.Button(card, "CANCEL", 0, -120, 260, 60, false, NetSession.Leave);
    }

    void BuildConnected()
    {
        DuoUI.Label(card, "CONNECTED", 0, 110, 700, 44, 30, Theme.Great, true);
        DuoUI.Label(card, "You play:  <b>" + NetSession.RoleName(NetSession.MyRole).ToUpperInvariant() + "</b>", -120, 45, 460, 50, 30, Theme.Text,
            false, TextAlignmentOptions.MidlineLeft);
        DuoUI.Button(card, "SWAP ROLES", 220, 45, 240, 56, false, () => NetSession.SetRole(NetSession.PartnerRole));

        if (NetSession.IsHost)
        {
            DuoUI.Button(card, "PICK SONG", 0, -50, 320, 68, true, () =>
            {
                Notice = null;
                SceneManager.LoadScene("SongSelect");
            });
        }
        else
        {
            DuoUI.Label(card, "Waiting for the host to pick a song...", 0, -50, 700, 50, 26, Theme.TextMuted);
        }
        DuoUI.Button(card, "LEAVE", 0, -150, 220, 56, false, () => { Notice = null; NetSession.Leave(); });
    }
}
