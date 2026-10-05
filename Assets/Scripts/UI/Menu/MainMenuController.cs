using UnityEngine;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    [SerializeField] string gameSceneName = "GameTable_cleaning";

    [Header("Buttons")]
    [SerializeField] Button playButton;
    [SerializeField] Button easyButton;
    [SerializeField] Button hardButton;

    [Header("Selection rings")]
    [SerializeField] GameObject easySelected;
    [SerializeField] GameObject hardSelected;

    [Header("How to play")]
    [SerializeField] Button howToPlayButton;
    [SerializeField] HowToPlayView howToPlay;

    const string DifficultyKey = "bots.difficulty";

    void Awake()
    {
        Time.timeScale = 1f;
        MatchSettings.Difficulty = (BotDifficulty)PlayerPrefs.GetInt(DifficultyKey, (int)BotDifficulty.Easy);

        if (playButton != null) playButton.onClick.AddListener(Play);
        if (easyButton != null) easyButton.onClick.AddListener(() => SetDifficulty(BotDifficulty.Easy));
        if (hardButton != null) hardButton.onClick.AddListener(() => SetDifficulty(BotDifficulty.Hard));
        if (howToPlayButton != null && howToPlay != null) howToPlayButton.onClick.AddListener(howToPlay.Open);

        RefreshSelection();
    }

    void SetDifficulty(BotDifficulty difficulty)
    {
        MatchSettings.Difficulty = difficulty;
        PlayerPrefs.SetInt(DifficultyKey, (int)difficulty);
        RefreshSelection();
        Sfx.Play(SfxId.Click);
    }

    void RefreshSelection()
    {
        if (easySelected != null) easySelected.SetActive(MatchSettings.Difficulty == BotDifficulty.Easy);
        if (hardSelected != null) hardSelected.SetActive(MatchSettings.Difficulty == BotDifficulty.Hard);
    }

    public void Quit() => Application.Quit();

    void Play()
    {
        Sfx.Play(SfxId.Click);
        MatchSettings.Configured = true;
        ScreenFader.LoadScene(gameSceneName);
    }
}
