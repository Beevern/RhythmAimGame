using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;

//Tools > Rebuild Level Editor UI
//Saves the Level Editor's UI into LevelEditor.unity as normal GameObjects (visible and editable
//before Play) and adds a real "Level Editor" button to the Menu scene.
//Running it again replaces the UI with a fresh copy (any hand edits to it are lost).
public static class LevelEditorBuilder
{
    const string EditorScenePath = "Assets/Scenes/LevelEditor.unity";
    const string MenuScenePath = "Assets/Scenes/Menu.unity";

    [MenuItem("Tools/Rebuild Level Editor UI")]
    public static void Rebuild()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog("Rebuild Level Editor UI", "Stop Play mode first.", "OK");
            return;
        }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        //Menu: a real "Level Editor" button
        var menu = EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);
        if (AddMenuButton())
        {
            EditorSceneManager.MarkSceneDirty(menu);
            EditorSceneManager.SaveScene(menu);
        }

        //Level Editor scene: replace the UI with a fresh build
        var scene = EditorSceneManager.OpenScene(EditorScenePath, OpenSceneMode.Single);
        foreach (LevelEditorView old in Object.FindObjectsByType<LevelEditorView>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            Object.DestroyImmediate(old.gameObject);
        LevelEditorView view = LevelEditorView.Build();
        EnsureCameraAndEventSystem();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Selection.activeGameObject = view.gameObject;
        EditorGUIUtility.PingObject(view.gameObject);
        Debug.Log("Level Editor UI rebuilt and saved into " + EditorScenePath + " (and the Menu's Level Editor button).");
    }

    //Copies the Level Maker button, points it at the LevelEditor scene and puts it under Quit
    static bool AddMenuButton()
    {
        if (GameObject.Find("LevelEditorButton") != null) return false;
        GameObject maker = GameObject.Find("LevelMaker");
        if (maker == null)
        {
            Debug.LogWarning("Couldn't find the LevelMaker button in the Menu scene; no Level Editor button added.");
            return false;
        }
        GameObject copy = Object.Instantiate(maker, maker.transform.parent);
        copy.name = "LevelEditorButton";
        SwitchScene sw = copy.GetComponent<SwitchScene>();
        if (sw != null) sw.sceneName = "LevelEditor";
        TMP_Text label = copy.GetComponentInChildren<TMP_Text>(true);
        if (label != null) label.text = "Level Editor";
        RectTransform rt = (RectTransform)copy.transform;
        rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, -100f);
        Undo.RegisterCreatedObjectUndo(copy, "Add Level Editor button");
        return true;
    }

    static void EnsureCameraAndEventSystem()
    {
        if (Object.FindFirstObjectByType<Camera>() == null)
        {
            var cam = new GameObject("Main Camera").AddComponent<Camera>();
            cam.tag = "MainCamera";
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Theme.Background;
            cam.gameObject.AddComponent<AudioListener>();
        }
        if (Object.FindFirstObjectByType<EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }
    }
}
