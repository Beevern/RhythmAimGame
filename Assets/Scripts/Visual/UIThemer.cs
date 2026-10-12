using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

//Restyles every scene's uGUI at load time so the scenes themselves don't need editing.
//Only colours, sprites and fonts are changed; layout is left as authored (except a few HUD hooks).
public static class UIThemer
{
    public static bool Enabled = true;

    //Built-in uGUI sprite names that get swapped for the theme's sprites
    private static readonly HashSet<string> defaultSprites = new HashSet<string>
    {
        "UISprite", "Background", "InputFieldBackground", "Knob", "UIMask", "Checkmark", "DropdownArrow"
    };

    private static readonly HashSet<int> themed = new HashSet<int>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Init()
    {
        SceneManager.sceneLoaded += (scene, mode) => ApplyScene();
        ApplyScene();
    }

    private static int lastSceneHandle = -1;

    public static void ApplyScene()
    {
        if (!Enabled) return;
        //The first scene can arrive through both the load event and Init
        int handle = SceneManager.GetActiveScene().handle;
        if (handle == lastSceneHandle) return;
        lastSceneHandle = handle;
        bool isGame = Object.FindFirstObjectByType<ScoreKeeper>(FindObjectsInactive.Include) != null;

        StyleCameras(isGame);

        //Song select arrows keep their original look (themed panels overlapped the covers)
        SongSelect2 select = Object.FindFirstObjectByType<SongSelect2>();
        if (select != null)
        {
            Exclude(select.left);
            Exclude(select.right);
        }

        if (SceneManager.GetActiveScene().name == "Menu")
        {
            AddLevelEditorButton();
            MultiplayerLobby.AddMenuButton();
        }

        foreach (Canvas canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            //Already-styled UI saved into a scene (e.g. the Level Editor)
            if (canvas.GetComponentInParent<NoTheme>(true) != null) continue;
            StyleHierarchy(canvas.transform);
        }

        //Scene-specific polish
        foreach (Settings settings in Object.FindObjectsByType<Settings>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            SettingsCard.Apply(settings);
        }
        if (isGame)
        {
            GameHud.Attach();
        }
        else
        {
            MenuBackdrop.Create();
            if (SceneManager.GetActiveScene().name == "Menu")
            {
                MenuLogo.Create();
                ClearMenuButtonBackgrounds();
            }
        }
        SongSelect2 songSelect = Object.FindFirstObjectByType<SongSelect2>();
        if (songSelect != null && songSelect.GetComponent<SongSelectFx>() == null)
        {
            songSelect.gameObject.AddComponent<SongSelectFx>();
        }
        GameOver gameOver = Object.FindFirstObjectByType<GameOver>();
        if (gameOver != null && gameOver.GetComponent<ResultsScreen>() == null)
        {
            gameOver.gameObject.AddComponent<ResultsScreen>();
        }
        SceneFade.Play();
    }

    //Marks an element and everything under it as already themed so it's left untouched
    private static void Exclude(Component c)
    {
        if (c == null) return;
        foreach (Component part in c.GetComponentsInChildren<Component>(true))
        {
            if (part is Selectable || part is Graphic || part is Shadow) themed.Add(part.GetInstanceID());
        }
    }

    //Main menu: buttons are just their text (no panel behind them); hover/press still scale them.
    //Buttons inside the settings panel keep their background.
    private static void ClearMenuButtonBackgrounds()
    {
        var settingsPanels = new List<Transform>();
        foreach (Settings st in Object.FindObjectsByType<Settings>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (st.settingsMenu != null) settingsPanels.Add(st.settingsMenu.transform);

        foreach (Button b in Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            bool inSettings = false;
            foreach (Transform panel in settingsPanels)
                if (b.transform.IsChildOf(panel)) { inSettings = true; break; }
            if (inSettings || b.GetComponentInParent<NoTheme>(true) != null) continue;

            ColorBlock cb = b.colors;
            cb.normalColor = cb.highlightedColor = cb.pressedColor = cb.selectedColor = cb.disabledColor = Color.clear;
            b.colors = cb;
            //Keep the (invisible) image so the whole button area stays clickable
            if (b.targetGraphic != null) b.targetGraphic.color = Color.white;
        }
    }

    //Main menu: a "Level Editor" button, copied from the Level Maker button, under Quit
    private static void AddLevelEditorButton()
    {
        GameObject maker = GameObject.Find("LevelMaker");
        if (maker == null || GameObject.Find("LevelEditorButton") != null) return;
        GameObject copy = Object.Instantiate(maker, maker.transform.parent);
        copy.name = "LevelEditorButton";
        SwitchScene sw = copy.GetComponent<SwitchScene>();
        if (sw != null) sw.sceneName = "LevelEditor";
        TMP_Text label = copy.GetComponentInChildren<TMP_Text>(true);
        if (label != null) label.text = "Level Editor";
        RectTransform rt = (RectTransform)copy.transform;
        rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, -100f);
    }

    private static void StyleCameras(bool isGame)
    {
        foreach (Camera cam in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
        {
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Theme.Background;
        }
        //Menu scenes have a big lit black plane behind the UI; hide it so the themed background shows.
        //(Its AudioSource is still used for button sounds, so only the renderer is disabled.)
        if (!isGame)
        {
            foreach (GameObject bg in GameObject.FindGameObjectsWithTag("Background"))
            {
                Renderer r = bg.GetComponent<Renderer>();
                if (r != null) r.enabled = false;
            }
        }
    }

    public static void StyleHierarchy(Transform root)
    {
        //Selectables first so their child graphics are claimed before the generic passes
        foreach (Selectable s in root.GetComponentsInChildren<Selectable>(true))
        {
            if (!themed.Add(s.GetInstanceID())) continue;
            if (s is TMP_InputField input) StyleInput(input);
            else if (s is TMP_Dropdown dropdown) StyleDropdown(dropdown);
            else if (s is Slider slider) StyleSlider(slider);
            else if (s is Scrollbar scrollbar) StyleScrollbar(scrollbar);
            else if (s is Toggle toggle) StyleToggle(toggle);
            else if (s is Button button) StyleButton(button);
        }

        foreach (Image img in root.GetComponentsInChildren<Image>(true))
        {
            if (!themed.Add(img.GetInstanceID())) continue;
            StylePanel(img);
        }

        foreach (TextMeshProUGUI t in root.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            if (!themed.Add(t.GetInstanceID())) continue;
            StyleText(t);
        }

        foreach (Text t in root.GetComponentsInChildren<Text>(true))
        {
            if (!themed.Add(t.GetInstanceID())) continue;
            t.color = Theme.Text;
        }

        //Flat look: no drop shadows/outlines
        foreach (Shadow sh in root.GetComponentsInChildren<Shadow>(true))
        {
            if (!themed.Add(sh.GetInstanceID())) continue;
            sh.enabled = false;
        }
    }

    //---------------- Element styles ----------------

    private static bool IsDefault(Image img)
    {
        return img != null && (img.sprite == null || defaultSprites.Contains(img.sprite.name));
    }

    private static void Rounded(Image img, Color color)
    {
        img.sprite = Theme.RoundedRect;
        img.type = Image.Type.Sliced;
        //Aim for ~10px corners at 1080p whatever the canvas reference resolution is
        img.pixelsPerUnitMultiplier = 14f / (10f * UIFactory.Unit(img));
        img.color = color;
        themed.Add(img.GetInstanceID());
    }

    private static ColorBlock Block(Color normal, Color hover, Color pressed)
    {
        ColorBlock cb = ColorBlock.defaultColorBlock;
        cb.normalColor = normal;
        cb.highlightedColor = hover;
        cb.pressedColor = pressed;
        cb.selectedColor = normal;
        cb.disabledColor = normal.WithAlpha(normal.a * 0.4f);
        cb.colorMultiplier = 1f;
        cb.fadeDuration = 0.08f;
        return cb;
    }

    private static void StyleButton(Button b)
    {
        Image img = b.targetGraphic as Image;
        if (IsDefault(img))
        {
            Rounded(img, Color.white);
            b.colors = Block(Theme.Surface, Theme.SurfaceHover, Theme.AccentPressed);
        }
        else
        {
            //Custom-art buttons (e.g. cover images) just get a subtle tint response
            b.colors = Block(Color.white, new Color(0.88f, 0.88f, 0.88f), new Color(0.7f, 0.7f, 0.7f));
        }
        if (b.GetComponent<ButtonFeedback>() == null) b.gameObject.AddComponent<ButtonFeedback>();
    }

    private static void StyleInput(TMP_InputField f)
    {
        Image img = f.targetGraphic as Image;
        if (IsDefault(img)) Rounded(img, Color.white);
        f.colors = Block(Theme.Field, Theme.Field.WithAlpha(1f), Theme.Field.WithAlpha(1f));
        f.caretColor = Theme.Accent;
        f.customCaretColor = true;
        f.selectionColor = Theme.Accent.WithAlpha(0.35f);
        if (f.placeholder is TextMeshProUGUI ph)
        {
            StyleText(ph);
            ph.color = Theme.TextMuted;
            themed.Add(ph.GetInstanceID());
        }
    }

    private static void StyleDropdown(TMP_Dropdown d)
    {
        Image img = d.targetGraphic as Image;
        if (IsDefault(img)) Rounded(img, Color.white);
        d.colors = Block(Theme.Surface, Theme.SurfaceHover, Theme.AccentPressed);

        if (d.template != null)
        {
            Image templateBg = d.template.GetComponent<Image>();
            if (IsDefault(templateBg)) Rounded(templateBg, Theme.Surface.WithAlpha(0.98f));
        }
        //The arrow is the dropdown's only non-text child image
        foreach (Image child in d.GetComponentsInChildren<Image>(true))
        {
            if (child == img || themed.Contains(child.GetInstanceID())) continue;
            if (child.sprite != null && child.sprite.name == "DropdownArrow")
            {
                child.color = Theme.TextMuted;
                themed.Add(child.GetInstanceID());
            }
        }
        if (d.GetComponent<ButtonFeedback>() == null) d.gameObject.AddComponent<ButtonFeedback>();
    }

    private static void StyleToggle(Toggle t)
    {
        //Mostly dropdown items: transparent rows that light up on hover, accent check dot
        Image bg = t.targetGraphic as Image;
        if (IsDefault(bg)) Rounded(bg, Color.white);
        t.colors = Block(Color.clear, Color.white.WithAlpha(0.08f), Theme.Accent.WithAlpha(0.4f));
        if (t.graphic is Image check)
        {
            check.sprite = Theme.Circle;
            check.type = Image.Type.Simple;
            check.preserveAspect = true;
            check.color = Theme.Accent;
            check.rectTransform.sizeDelta = new Vector2(8, 8) * UIFactory.Unit(check);
            themed.Add(check.GetInstanceID());
        }
    }

    private static void StyleSlider(Slider s)
    {
        if (s.fillRect != null)
        {
            Image fill = s.fillRect.GetComponent<Image>();
            if (IsDefault(fill)) Rounded(fill, Theme.Accent);
            //Track is the slider's own background (first image that isn't fill or handle)
            foreach (Image img in s.GetComponentsInChildren<Image>(true))
            {
                if (img == fill || img == s.targetGraphic || themed.Contains(img.GetInstanceID())) continue;
                if (IsDefault(img)) Rounded(img, Theme.Track);
            }
        }
        if (s.handleRect != null)
        {
            Image handle = s.handleRect.GetComponent<Image>();
            if (IsDefault(handle))
            {
                handle.sprite = Theme.Circle;
                handle.type = Image.Type.Simple;
                handle.preserveAspect = true;
                handle.color = Color.white;
                themed.Add(handle.GetInstanceID());
            }
        }
        s.colors = Block(Color.white, Theme.Text, Theme.Accent);
    }

    private static void StyleScrollbar(Scrollbar sb)
    {
        Image bg = sb.GetComponent<Image>();
        if (IsDefault(bg)) Rounded(bg, Color.white.WithAlpha(0.04f));
        Image handle = sb.handleRect != null ? sb.handleRect.GetComponent<Image>() : null;
        if (IsDefault(handle)) Rounded(handle, Color.white);
        sb.colors = Block(Color.white.WithAlpha(0.22f), Color.white.WithAlpha(0.4f), Theme.Accent);
    }

    private static void StylePanel(Image img)
    {
        if (!IsDefault(img)) return;
        //Masks (e.g. scroll view viewports) keep their shape but shouldn't show
        if (img.GetComponent<Mask>() != null && img.sprite != null && img.sprite.name == "UIMask") return;
        //Leave progress fills etc. that use null sprites with Filled type to their owners
        if (img.type == Image.Type.Filled) return;

        RectTransform rt = img.rectTransform;
        bool fullScreen = rt.anchorMin == Vector2.zero && rt.anchorMax == Vector2.one && rt.sizeDelta.sqrMagnitude < 4f;
        if (fullScreen)
        {
            //Full-screen tints (pause menu etc.) become a dark scrim
            img.sprite = null;
            img.color = Theme.Overlay;
        }
        else if (img.sprite != null)
        {
            Rounded(img, Theme.Surface);
        }
    }

    //Only recolours; fonts, sizes and layout stay as authored
    public static void StyleText(TextMeshProUGUI t)
    {
        t.color = Theme.Text;
    }
}
