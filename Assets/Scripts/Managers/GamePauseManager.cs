using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class GamePauseManager : MonoBehaviour
{
    public static GamePauseManager Instance { get; private set; }
    [Header("UI")]
    public string pausePanelTag = "PausePanel";
    private GameObject pausePanel;

    [Header("Input")]
    public InputActionReference pauseAction;

    private bool isPaused = false;
    public bool IsPaused => isPaused;
    private readonly List<PlayerInput> disabledPlayerInputs = new();
    private readonly List<AudioSource> pausedAudioSources = new();
    private bool cursorWasVisible;
    private CursorLockMode cursorWasLockState;

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;

        isPaused = false;
        Time.timeScale = 1f;

        if (pausePanel != null)
            SetPausePanel(false);

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        SceneManager.sceneLoaded += OnSceneLoaded;
    }


    private void OnEnable()
    {
        if (pauseAction?.action != null)
        {
            pauseAction.action.performed += OnPausePerformed;
            pauseAction.action.Enable();
        }
    }

    private void OnDisable()
    {
        if (pauseAction?.action != null)
            pauseAction.action.performed -= OnPausePerformed;
    }

    private void OnPausePerformed(InputAction.CallbackContext ctx)
    {
        if (isPaused) ResumeGame();
        else PauseGame();
    }

    public void PauseGame()
    {
        if (isPaused || pausePanel == null) return;

        cursorWasVisible = Cursor.visible;
        cursorWasLockState = Cursor.lockState;

        SetPausePanel(true);

        disabledPlayerInputs.Clear();
        foreach (var p in FindObjectsOfType<PlayerInput>(true))
        {
            if (!p.enabled) continue;
            p.enabled = false;
            disabledPlayerInputs.Add(p);
        }

        pausedAudioSources.Clear();
        foreach (var audio in FindObjectsOfType<AudioSource>(true))
        {
            if (!audio.isPlaying) continue;
            audio.Pause();
            pausedAudioSources.Add(audio);
        }

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        Time.timeScale = 0f;
        isPaused = true;
    }

    public void ResumeGame()
    {
        if (!isPaused || pausePanel == null) return;

        SetPausePanel(false);

        foreach (var p in disabledPlayerInputs)
            if (p != null) p.enabled = true;
        disabledPlayerInputs.Clear();

        foreach (var audio in pausedAudioSources)
            if (audio != null)
                audio.UnPause();
        pausedAudioSources.Clear();

        Cursor.visible = cursorWasVisible;
        Cursor.lockState = cursorWasLockState;

        Time.timeScale = 1f;
        isPaused = false;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name.StartsWith("Level"))
        {
            if (pausePanel == null)
                pausePanel = FindInactiveByTag(pausePanelTag);

            if (pausePanel != null)
                SetPausePanel(false);

            isPaused = false;
            Time.timeScale = 1f;
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
        else
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
    }

    private GameObject FindInactiveByTag(string tag)
    {
        foreach (var go in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            if (go.CompareTag(tag))
                return go;
        }
        return null;
    }


    private void SetPausePanel(bool show)
    {
        if (pausePanel == null) return;
        pausePanel.SetActive(show);

        if (!pausePanel.TryGetComponent(out CanvasGroup cg))
            cg = pausePanel.AddComponent<CanvasGroup>();

        cg.blocksRaycasts = show;
        cg.interactable = show;
    }

    public void ContinueGame() => ResumeGame();
    public void RestartLevel() => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    public void BackToMainMenu() => SceneManager.LoadScene("MainMenu");
}
