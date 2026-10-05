using TMPro;
using UnityEngine;

public class PlayerIdCardView : MonoBehaviour
{
    [SerializeField] GameBootstrap bootstrap;
    [SerializeField] TableAnimations animations;
    [Tooltip("0 = you, 1..3 = the bots")]
    [SerializeField] int playerId;

    [Header("UI")]
    [SerializeField] TextMeshProUGUI nameText;
    [SerializeField] TextMeshProUGUI cardsText;
    [SerializeField] TextMeshProUGUI booksText;
    [SerializeField] GameObject turnHighlight;

    int _shownCards = -1;
    int _shownBooks = -1;
    bool _nameSet;

    void Awake()
    {
        if (bootstrap == null) bootstrap = FindFirstObjectByType<GameBootstrap>();
        if (animations == null) animations = FindFirstObjectByType<TableAnimations>();
        if (turnHighlight != null) turnHighlight.SetActive(false);
    }

    void Update()
    {
        var state = bootstrap != null ? bootstrap.State : null;
        if (state == null || playerId < 0 || playerId >= state.PlayerCount) return;

        var player = state.GetPlayer(playerId);

        if (!_nameSet && nameText != null)
        {
            nameText.text = Viewer.DisplayName(player);
            _nameSet = true;
        }

        if (player.Hand.Count != _shownCards)
        {
            _shownCards = player.Hand.Count;
            if (cardsText != null) cardsText.text = _shownCards.ToString();
        }

        if (player.BooksCount != _shownBooks)
        {
            _shownBooks = player.BooksCount;
            if (booksText != null) booksText.text = _shownBooks.ToString();
        }

        if (turnHighlight != null)
        {
            int shownTurn = animations != null ? animations.ShownTurnPlayerId(state) : state.CurrentTurnPlayerId;
            bool myTurn = bootstrap.Engine != null && shownTurn == playerId;
            if (turnHighlight.activeSelf != myTurn) turnHighlight.SetActive(myTurn);
        }
    }
}
