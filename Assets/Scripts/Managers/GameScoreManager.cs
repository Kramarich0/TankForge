using UnityEngine;

public class GameScoreManager : MonoBehaviour
{
    public static GameScoreManager Instance;

    public int playerScore = 0;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public void AddScore(int points)
    {
        playerScore += points;
        Debug.Log($"Очки: +{points}. Всего: {playerScore}");

        if (scoreDisplay != null)
            scoreDisplay.UpdateScore(playerScore);
    }

    [Header("UI")]
    public ScoreDisplay scoreDisplay; 
}
