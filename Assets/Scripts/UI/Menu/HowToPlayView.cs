using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HowToPlayView : MonoBehaviour
{
    [Serializable]
    public struct Page
    {
        public string heading;
        [TextArea(3, 8)] public string body;
    }

    [Tooltip("Saved inactive, switched on by Open()")]
    [SerializeField] GameObject root;

    [Header("Text")]
    [SerializeField] TextMeshProUGUI headingText;
    [SerializeField] TextMeshProUGUI bodyText;
    [SerializeField] TextMeshProUGUI pageText;
    [SerializeField] TextMeshProUGUI nextLabel;

    [Header("Buttons")]
    [SerializeField] Button backButton;
    [SerializeField] Button nextButton;
    [SerializeField] Button closeButton;

    public Page[] pages =
    {
        new Page
        {
            heading = "Ask",
            body = "On your turn, tap a bot, then pick a rank you hold.\n\n" +
                   "If they have none of that rank: go fish! You draw a card and your turn ends."
        },
        new Page
        {
            heading = "Count",
            body = "If they have it, guess how many cards of that rank they hold.\n\n" +
                   "Wrong guess: you draw a card and your turn ends."
        },
        new Page
        {
            heading = "Suits",
            body = "Right count? Now pick which suits they hold " +
                   "<sprite name=\"SUIT_C\" tint=1> <sprite name=\"SUIT_D\" tint=1> <sprite name=\"SUIT_H\" tint=1> <sprite name=\"SUIT_S\" tint=1>. " +
                   "Suits you hold in that rank can't be theirs, so they are left out. If only one choice is left, it is made for you.\n\n" +
                   "Right suits: take the cards and play again. Wrong: you draw a card and your turn ends."
        },
        new Page
        {
            heading = "Books",
            body = "Four cards of one rank make a book. Books are laid for you.\n\n" +
                   "The game ends when all 13 books are laid or nobody can catch the leader. Most books wins.\n\n" +
                   "Tip: drag cards to arrange your hand, tap a card to raise it."
        },
    };

    int _page;

    public bool IsOpen => root != null && root.activeSelf;

    void Awake()
    {
        if (root != null) root.SetActive(false);
        if (backButton != null) backButton.onClick.AddListener(PreviousPage);
        if (nextButton != null) nextButton.onClick.AddListener(NextPage);
        if (closeButton != null) closeButton.onClick.AddListener(CloseWithClick);
    }

    void OnDisable() => BackButton.Remove(Close);

    public void Open()
    {
        if (root == null) return;
        Sfx.Play(SfxId.Click);
        _page = 0;
        root.SetActive(true);
        Show();
        BackButton.Push(Close);
    }

    public void Close()
    {
        if (root != null) root.SetActive(false);
        BackButton.Remove(Close);
    }

    void CloseWithClick()
    {
        Sfx.Play(SfxId.Click);
        Close();
    }

    void NextPage()
    {
        Sfx.Play(SfxId.Click);
        if (_page < pages.Length - 1) { _page++; Show(); }
        else Close();
    }

    void PreviousPage()
    {
        Sfx.Play(SfxId.Click);
        if (_page > 0) { _page--; Show(); }
    }

    void Show()
    {
        if (pages == null || pages.Length == 0) return;
        _page = Mathf.Clamp(_page, 0, pages.Length - 1);

        if (headingText != null) headingText.text = pages[_page].heading;
        if (bodyText != null) bodyText.text = pages[_page].body;
        if (pageText != null) pageText.text = $"{_page + 1}/{pages.Length}";
        if (nextLabel != null) nextLabel.text = _page == pages.Length - 1 ? "DONE" : "NEXT";
        if (backButton != null) backButton.gameObject.SetActive(_page > 0);
    }
}
