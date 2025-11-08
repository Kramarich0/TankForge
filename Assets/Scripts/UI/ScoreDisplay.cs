using UnityEngine;
using TMPro;

public class ScoreDisplay : MonoBehaviour
{
    public TextMeshProUGUI scoreText;

    public void UpdateScore(int score)
    {
        if (scoreText != null)
            scoreText.text = "Очки: " + score.ToString();
    }
}


