using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LevelSelectManager : MonoBehaviour
{
    [Header("Level Select")]
    public GameObject levelSelectCanvas;   // Canvas с кнопками выбора уровней
    public Button[] levelButtons;
    public Image[] levelCheckmarks;

    [Header("Loading Screen")]
    public GameObject loadingScreen;       // панель загрузки с картинкой
    public Slider progressBar;             // прогресс-бар

    void Awake()
    {
        // Деактивируем кнопки и чекмарки до загрузки
        foreach (var btn in levelButtons)
            btn.interactable = false;

        if (levelCheckmarks != null)
        {
            foreach (var chk in levelCheckmarks)
                chk.gameObject.SetActive(false);
        }

        if (loadingScreen != null)
            loadingScreen.SetActive(false); // скрываем экран загрузки
    }

    void Start()
    {
        UpdateLevelButtons();
    }

    public void PlayLevel(int level)
    {
        if (!IsLevelUnlocked(level)) return;

        string sceneName = $"Level{level}";
        StartCoroutine(LoadLevelAsync(sceneName));
    }

    private IEnumerator LoadLevelAsync(string sceneName)
    {
        if (levelSelectCanvas != null)
            levelSelectCanvas.SetActive(false);

        if (loadingScreen != null)
            loadingScreen.SetActive(true);

        if (progressBar != null)
        {
            progressBar.gameObject.SetActive(true);
            progressBar.value = 0f;
        }

        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
        op.allowSceneActivation = false;

        float displayedProgress = 0f;
        float minDisplayTime = 1.5f; // Минимальное время отображения прогресса
        float timer = 0f;

        while (!op.isDone)
        {
            timer += Time.unscaledDeltaTime;

            // Целевой прогресс до 0.9 (Unity так считает загрузку)
            float targetProgress = Mathf.Clamp01(op.progress / 0.9f);

            // Плавное движение прогресс-бара
            displayedProgress = Mathf.MoveTowards(displayedProgress, targetProgress, Time.unscaledDeltaTime * 1.5f);

            if (progressBar != null)
                progressBar.value = displayedProgress;

            // Когда загрузка почти завершена и прошло минимальное время
            if (displayedProgress >= 0.99f && timer >= minDisplayTime)
            {
                if (progressBar != null)
                    progressBar.value = 1f;

                yield return new WaitForSeconds(0.1f);
                op.allowSceneActivation = true;
            }

            yield return null;
        }
    }


    void UpdateLevelButtons()
    {
        for (int i = 0; i < levelButtons.Length; i++)
        {
            int level = i + 1;
            bool unlocked = IsLevelUnlocked(level);
            bool completed = IsLevelCompleted(level);

            levelButtons[i].interactable = unlocked;

            if (levelCheckmarks != null && levelCheckmarks.Length > i)
                levelCheckmarks[i].gameObject.SetActive(completed);
        }
    }

    private bool IsLevelUnlocked(int level)
    {
        return level == 1 || PlayerPrefs.GetInt($"Level{level}_Unlocked", 0) == 1;
    }

    private bool IsLevelCompleted(int level)
    {
        return PlayerPrefs.GetInt($"Level{level}_Completed", 0) == 1;
    }

    public void BackToMainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }
}
