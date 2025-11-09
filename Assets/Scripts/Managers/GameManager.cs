using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public int score = 0;
    private int enemiesRemaining = 0;
    public System.Action<int> OnScoreChanged;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void InitializeLevel()
    {
        TankAI[] enemies = FindObjectsOfType<TankAI>();
        enemiesRemaining = 0;
        foreach (var enemy in enemies)
        {
            if (enemy.GetComponent<TeamComponent>()?.team == Team.Enemy)
            {
                enemiesRemaining++;
            }
        }

        score = 0;
        Debug.Log($"[GameManager] Уровень запущен. Врагов: {enemiesRemaining}");
    }

    public void OnEnemyDestroyed(GameObject enemy)
    {
        int scoreValue = 200;

        if (enemy.TryGetComponent<TankAI>(out TankAI ai))
        {
            switch (ai.tankClass)
            {
                case TankAI.TankClass.Light: scoreValue = 100; break;
                case TankAI.TankClass.Medium: scoreValue = 200; break;
                case TankAI.TankClass.Heavy: scoreValue = 300; break;
            }
        }

        score += scoreValue;
        OnScoreChanged?.Invoke(score);
        enemiesRemaining--;

        Debug.Log($"Враг уничтожен! Осталось: {enemiesRemaining}, Счёт: {score}");

        if (enemiesRemaining <= 0)
        {
            CompleteLevel();
        }

    }
    void CompleteLevel()
    {
        int stars = CalculateStars(score);
        string sceneName = SceneManager.GetActiveScene().name;

        int levelIndex = 0;
        if (sceneName.StartsWith("Level"))
        {
            string numPart = sceneName[5..];
            if (int.TryParse(numPart, out int idx)) levelIndex = idx;
        }

        // Сохраняем результат
        PlayerPrefs.SetInt($"Level{levelIndex}_Score", score);
        PlayerPrefs.SetInt($"Level{levelIndex}_Stars", stars);
        PlayerPrefs.SetInt($"Level{levelIndex}_Completed", 1);
        PlayerPrefs.SetInt($"Level{levelIndex + 1}_Unlocked", 1);
        PlayerPrefs.Save();

        Debug.Log($"✅ Уровень {levelIndex} завершён! Звёзд: {stars}, Счёт: {score}");

        // Позже сюда можно добавить: загрузку экрана победы
        // SceneManager.LoadScene("VictoryScreen");
    }

    int CalculateStars(int score)
    {
        if (score >= 4000) return 3;
        if (score >= 2500) return 2;
        if (score >= 1000) return 1;
        return 0;
    }

    public void OnPlayerTankDestroyed()
    {
        Debug.Log("💀 Игрок уничтожен. Игра окончена.");

        // Здесь можно:
        // - показать экран поражения
        // - перезапустить уровень
        // - вернуться в меню

        // Например, перезапуск текущего уровня:
        // SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}