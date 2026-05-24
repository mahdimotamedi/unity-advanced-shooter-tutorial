using UnityEngine;

public class DemoHUD : MonoBehaviour
{
    public ShooterController shooter;
    public SwingController swing;
    public ScoreManager scoreManager;

    private GUIStyle boxStyle;
    private GUIStyle labelStyle;
    private Texture2D crosshairTexture;

    private void Awake()
    {
        if (shooter == null) shooter = GetComponent<ShooterController>();
        if (swing == null) swing = GetComponent<SwingController>();
        if (scoreManager == null) scoreManager = FindObjectOfType<ScoreManager>();
        CreateCrosshair();
    }

    private void OnGUI()
    {
        EnsureStyles();

        if (scoreManager == null) scoreManager = ScoreManager.Instance;

        string mode = shooter != null && shooter.CurrentMode == WeaponMode.Spear ? "Spear Throw" : "Gun";
        string swingState = swing != null && swing.IsSwinging ? "Attached to Chain" : "Free";
        int score = scoreManager != null ? scoreManager.Score : 0;
        string message = scoreManager != null ? scoreManager.LastMessage : string.Empty;

        string help =
            "Demo Controls\n" +
            "WASD: Move\n" +
            "Mouse: Look / Aim\n" +
            "Left Mouse: Shoot / Throw\n" +
            "Q or Mouse Wheel: Switch fire mode\n" +
            "Hold E near the chain: Grab and swing\n" +
            "R / F: Shorten or extend chain\n" +
            "Space: Jump or soft-release while swinging\n" +
            "Left Shift: Sprint\n" +
            "Esc: Unlock mouse cursor\n" +
            "Targets: Red=Gun, Blue=Spear, Yellow=Any\n\n" +
            "Fire Mode: " + mode + "\n" +
            "Swing State: " + swingState + "\n" +
            "Score: " + score;

        if (!string.IsNullOrEmpty(message))
        {
            help += "\n" + message;
        }

        GUI.Box(new Rect(14, 14, 485, 315), GUIContent.none, boxStyle);
        GUI.Label(new Rect(28, 26, 455, 298), help, labelStyle);

        DrawCrosshair();
    }

    private void EnsureStyles()
    {
        if (boxStyle != null) return;

        Texture2D bg = new Texture2D(1, 1);
        bg.SetPixel(0, 0, new Color(0f, 0f, 0f, 0.55f));
        bg.Apply();

        boxStyle = new GUIStyle(GUI.skin.box);
        boxStyle.normal.background = bg;

        labelStyle = new GUIStyle(GUI.skin.label);
        labelStyle.fontSize = 15;
        labelStyle.normal.textColor = Color.white;
        labelStyle.alignment = TextAnchor.UpperLeft;
        labelStyle.wordWrap = true;
        labelStyle.richText = true;
    }

    private void CreateCrosshair()
    {
        crosshairTexture = new Texture2D(1, 1);
        crosshairTexture.SetPixel(0, 0, Color.white);
        crosshairTexture.Apply();
    }

    private void DrawCrosshair()
    {
        if (crosshairTexture == null) CreateCrosshair();
        float cx = Screen.width * 0.5f;
        float cy = Screen.height * 0.5f;
        GUI.DrawTexture(new Rect(cx - 10f, cy, 20f, 2f), crosshairTexture);
        GUI.DrawTexture(new Rect(cx, cy - 10f, 2f, 20f), crosshairTexture);
    }
}
