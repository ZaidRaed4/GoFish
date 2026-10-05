using UnityEngine;
using UnityEngine.UI;

public class PauseMenu : MonoBehaviour
{
    [Tooltip("Saved inactive, switched on by Pause()")]
    [SerializeField] GameObject root;

    [Header("Buttons")]
    [SerializeField] Button pauseButton;
    [SerializeField] Button resumeButton;
    [SerializeField] Button howToPlayButton;
    [SerializeField] Button menuButton;

    [Header("Refs")]
    [SerializeField] HowToPlayView howToPlay;
    [Tooltip("The results board; back there goes to the menu instead")]
    [SerializeField] GameObject resultsRoot;

    [SerializeField] string menuSceneName = "MainMenu";

    public static bool IsPaused { get; private set; }

    void Awake()
    {
        SetPaused(false);
        if (root != null) root.SetActive(false);

        if (pauseButton != null) pauseButton.onClick.AddListener(Pause);
        if (resumeButton != null) resumeButton.onClick.AddListener(ResumeWithClick);
        if (howToPlayButton != null) howToPlayButton.onClick.AddListener(OpenHowToPlay);
        if (menuButton != null) menuButton.onClick.AddListener(GoToMenu);
    }

    // Nothing to pause once the results are up
    void Update()
    {
        if (pauseButton == null || resultsRoot == null) return;
        bool show = !resultsRoot.activeInHierarchy;
        if (pauseButton.gameObject.activeSelf != show) pauseButton.gameObject.SetActive(show);
    }

    // Never leave the next scene frozen
    void OnDestroy()
    {
        BackButton.Remove(Resume);
        SetPaused(false);
    }

    // Back button with no screen open
    public void OnBack()
    {
        if (resultsRoot != null && resultsRoot.activeInHierarchy) GoToMenu();
        else Pause();
    }

    public void Pause()
    {
        if (IsPaused || root == null) return;
        if (resultsRoot != null && resultsRoot.activeInHierarchy) return;

        Sfx.Play(SfxId.Click);
        SetPaused(true);
        root.SetActive(true);
        BackButton.Push(Resume);
    }

    public void Resume()
    {
        if (!IsPaused) return;

        if (howToPlay != null && howToPlay.IsOpen) howToPlay.Close();
        if (root != null) root.SetActive(false);
        BackButton.Remove(Resume);
        SetPaused(false);
    }

    void ResumeWithClick()
    {
        Sfx.Play(SfxId.Click);
        Resume();
    }

    void OpenHowToPlay()
    {
        if (howToPlay != null) howToPlay.Open();
    }

    void GoToMenu()
    {
        Sfx.Play(SfxId.Click);
        SetPaused(false);
        ScreenFader.LoadScene(menuSceneName);
    }

    static void SetPaused(bool paused)
    {
        IsPaused = paused;
        Time.timeScale = paused ? 0f : 1f;
    }
}
