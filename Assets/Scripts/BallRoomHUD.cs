using UnityEngine;

public class BallRoomHUD : MonoBehaviour
{
    public BallClickShooter shooter;

    public int panelWidth = 430;
    public int panelHeight = 190;

    private GUIStyle boxStyle;
    private GUIStyle labelStyle;
    private GUIStyle smallStyle;

    private void OnGUI()
    {
        EnsureStyles();

        float charge = shooter != null ? shooter.CurrentCharge01 : 0f;
        bool charging = shooter != null && shooter.IsCharging;

        GUI.Box(new Rect(14, 14, panelWidth, panelHeight), GUIContent.none, boxStyle);

        string help =
            "Ball Room Demo\n" +
            "Left Mouse on ball: start charging\n" +
            "Hold Left Mouse: increase shot power\n" +
            "Release Left Mouse: kick the ball\n" +
            "Click different parts of the ball to change direction and spin\n" +
            "Right Mouse Drag: orbit camera\n" +
            "Mouse Wheel: zoom camera\n" +
            "R: reset ball";

        GUI.Label(new Rect(28, 24, panelWidth - 32, 105), help, labelStyle);

        Rect barBack = new Rect(28, 142, panelWidth - 56, 22);
        Rect barFill = new Rect(barBack.x + 2, barBack.y + 2, (barBack.width - 4) * charge, barBack.height - 4);

        GUI.color = new Color(0f, 0f, 0f, 0.45f);
        GUI.DrawTexture(barBack, Texture2D.whiteTexture);
        GUI.color = charging ? new Color(0.2f, 0.75f, 1f, 0.92f) : new Color(0.35f, 0.35f, 0.35f, 0.62f);
        GUI.DrawTexture(barFill, Texture2D.whiteTexture);
        GUI.color = Color.white;

        string powerText = "Shot Power: " + Mathf.RoundToInt(charge * 100f) + "%";
        GUI.Label(new Rect(28, 166, panelWidth - 56, 22), powerText, smallStyle);
    }

    private void EnsureStyles()
    {
        if (boxStyle != null) return;

        boxStyle = new GUIStyle(GUI.skin.box);
        boxStyle.normal.background = MakeTexture(new Color(0f, 0f, 0f, 0.56f));

        labelStyle = new GUIStyle(GUI.skin.label);
        labelStyle.fontSize = 14;
        labelStyle.normal.textColor = Color.white;
        labelStyle.wordWrap = true;
        labelStyle.richText = false;

        smallStyle = new GUIStyle(GUI.skin.label);
        smallStyle.fontSize = 13;
        smallStyle.normal.textColor = Color.white;
        smallStyle.alignment = TextAnchor.MiddleLeft;
    }

    private Texture2D MakeTexture(Color color)
    {
        Texture2D tex = new Texture2D(1, 1);
        tex.hideFlags = HideFlags.HideAndDontSave;
        tex.SetPixel(0, 0, color);
        tex.Apply();
        return tex;
    }
}
