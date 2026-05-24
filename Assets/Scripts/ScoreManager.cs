using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    public int Score { get; private set; }
    public string LastMessage { get; private set; }

    private float messageClearTime;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        LastMessage = "Hit the correct targets to score.";
    }

    private void Update()
    {
        if (!string.IsNullOrEmpty(LastMessage) && Time.time > messageClearTime)
        {
            LastMessage = string.Empty;
        }
    }

    public void AddScore(int amount, string message)
    {
        Score += Mathf.Max(0, amount);
        SetMessage(message);
    }

    public void SetMessage(string message)
    {
        LastMessage = message;
        messageClearTime = Time.time + 1.8f;
    }
}
