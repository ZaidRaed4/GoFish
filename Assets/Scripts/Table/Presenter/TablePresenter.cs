using UnityEngine;
using GoFish.Core;

public class TablePresenter : MonoBehaviour
{
    [SerializeField] TableSeats seats;
    [SerializeField] DeckView deckView;

    GameState _state;

    void Awake()
    {
        if (seats == null) seats = FindFirstObjectByType<TableSeats>();
    }

    public void Bind(GameState state) => _state = state;

    public void Render()
    {
        if (_state == null) return;

        if (deckView != null) deckView.Render(_state.DeckCount);

        for (int id = 1; id < _state.PlayerCount; id++)
        {
            var hand = seats != null ? seats.Hand(id) : null;
            if (hand != null) hand.Render(_state.GetPlayer(id));
        }
    }
}
