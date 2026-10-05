using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using GoFish.Core;

public class GameOverController : MonoBehaviour
{
    [SerializeField] GameBootstrap bootstrap;
    [SerializeField] TableAnimations animations;

    [Header("UI")]
    [Tooltip("Saved inactive, switched on at game over")]
    [SerializeField] GameObject root;
    [SerializeField] TextMeshProUGUI titleText;
    [Tooltip("Filled best first")]
    [SerializeField] ResultRowView[] rows;
    [SerializeField] Button playAgainButton;
    [SerializeField] Button menuButton;
    [SerializeField] TableSeats seats;

    [Header("Flow")]
    [SerializeField] string menuSceneName = "MainMenu";
    [SerializeField] float delayAfterLastMove = 0.8f;
    [SerializeField] float fadeInDuration = 0.3f;
    [SerializeField] float maxWaitForAnimations = 5f;

    bool _shown;
    float _gameOverAt = -1f;
    float _idleSince = -1f;

    void Awake()
    {
        if (bootstrap == null) bootstrap = FindFirstObjectByType<GameBootstrap>();
        if (animations == null) animations = FindFirstObjectByType<TableAnimations>();
        if (seats == null) seats = FindFirstObjectByType<TableSeats>();

        if (root != null) root.SetActive(false);
        if (playAgainButton != null) playAgainButton.onClick.AddListener(PlayAgain);
        if (menuButton != null) menuButton.onClick.AddListener(GoToMenu);
    }

    void Update()
    {
        if (_shown || bootstrap == null || bootstrap.Engine == null) return;
        if (bootstrap.Engine.Phase != TurnPhase.GameOver) return;

        float now = Time.time;
        if (_gameOverAt < 0f) _gameOverAt = now;

        bool busy = animations != null && animations.IsBusy;
        if (busy && now - _gameOverAt < maxWaitForAnimations)
        {
            _idleSince = -1f;
            return;
        }

        if (_idleSince < 0f) _idleSince = now;
        if (now - _idleSince < delayAfterLastMove) return;

        Show(bootstrap.State);
    }

    void Show(GameState state)
    {
        _shown = true;

        var order = new List<PlayerState>(state.Players);
        order.Sort((a, b) => b.BooksCount != a.BooksCount ? b.BooksCount.CompareTo(a.BooksCount) : a.PlayerId.CompareTo(b.PlayerId));

        int best = order[0].BooksCount;
        var winners = order.FindAll(p => p.BooksCount == best);

        if (titleText != null)
        {
            if (winners.Count > 1) titleText.text = winners.Exists(p => Viewer.Is(p.PlayerId)) ? "A shared win!" : "It's a tie!";
            else titleText.text = Viewer.Is(winners[0].PlayerId) ? "You win!" : $"{SpelledOut.Name(winners[0].Name)} wins!";
        }

        for (int i = 0; i < rows.Length; i++)
        {
            if (rows[i] == null) continue;

            bool used = i < order.Count;
            rows[i].gameObject.SetActive(used);
            if (!used) continue;

            var p = order[i];
            string displayName = Viewer.DisplayName(p);
            rows[i].Set(seats != null ? seats.Portrait(p.PlayerId) : null, displayName, p.BooksCount, p.BooksCount == best);
        }

        if (root != null)
        {
            var group = root.GetComponent<CanvasGroup>();
            if (group == null) group = root.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            root.SetActive(true);
            StartCoroutine(Tween.Run(fadeInDuration, k => group.alpha = k, unscaledTime: true));
        }
        Sfx.Play(winners.Exists(p => Viewer.Is(p.PlayerId)) ? SfxId.Win : SfxId.Lose);
    }

    void PlayAgain()
    {
        Sfx.Play(SfxId.Click);
        ScreenFader.LoadScene(SceneManager.GetActiveScene().name);
    }

    void GoToMenu()
    {
        Sfx.Play(SfxId.Click);
        if (Application.CanStreamedLevelBeLoaded(menuSceneName)) ScreenFader.LoadScene(menuSceneName);
        else PlayAgain();
    }
}
