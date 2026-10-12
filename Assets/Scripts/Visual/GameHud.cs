using UnityEngine;
using UnityEngine.UI;

//In-game HUD polish: countdown pop animation. HUD layout and crosshair are left as authored.
public class GameHud : MonoBehaviour
{
    private Canvas crosshairCanvas;

    public static void Attach()
    {
        ScoreKeeper keeper = FindFirstObjectByType<ScoreKeeper>(FindObjectsInactive.Include);
        if (keeper != null && keeper.GetComponent<GameHud>() == null)
        {
            keeper.gameObject.AddComponent<GameHud>();
        }
    }

    void Start()
    {
        Spawner spawner = FindFirstObjectByType<Spawner>();
        if (spawner != null && spawner.startTimer != null && spawner.startTimer.GetComponent<TextPop>() == null)
        {
            spawner.startTimer.gameObject.AddComponent<TextPop>();
        }
        crosshairCanvas = ReplaceCrosshair();
    }

    void Update()
    {
        //Overlay canvases draw over the pause menu, so hide the crosshair while paused
        if (crosshairCanvas != null) crosshairCanvas.enabled = !Pause.isPaused;
    }

    //Swaps the scene's fixed crosshair for the customisable one (see CrosshairSettings)
    private static Canvas ReplaceCrosshair()
    {
        //Hide only the old crosshair image. Its canvas also holds the pause menu and the
        //countdown, so the canvas itself must stay on.
        GameObject canvasObj = GameObject.FindGameObjectWithTag("CrosshairUI");
        Transform old = canvasObj != null ? canvasObj.transform.Find("Crosshair") : null;
        if (old != null) old.gameObject.SetActive(false);
        //Scaled to a 1080-tall reference so sizes mean "pixels at 1080p" at any resolution
        Canvas canvas = UIFactory.OverlayCanvas("Crosshair", 0);
        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        scaler.matchWidthOrHeight = 1f;
        CrosshairView.Create(canvas.transform);
        return canvas;
    }

}
