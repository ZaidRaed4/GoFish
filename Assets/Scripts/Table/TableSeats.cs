using System;
using UnityEngine;

public class TableSeats : MonoBehaviour
{
    [Serializable]
    public struct Seat
    {
        [Tooltip("Where flying cards leave from, and land when they have no card slot")]
        public RectTransform cardPoint;
        public LayAreaView books;
        [Tooltip("The bot's face-down card row; empty for you")]
        public OpponentHandView hand;
        [Tooltip("Shown on the turn board and the results board")]
        public Sprite portrait;
    }

    [SerializeField] Seat[] seats = new Seat[4];

    public RectTransform CardPoint(int playerId) => SeatOf(playerId).cardPoint;
    public LayAreaView Books(int playerId) => SeatOf(playerId).books;
    public OpponentHandView Hand(int playerId) => SeatOf(playerId).hand;
    public Sprite Portrait(int playerId) => SeatOf(playerId).portrait;

    Seat SeatOf(int playerId) => seats != null && playerId >= 0 && playerId < seats.Length ? seats[playerId] : default;
}
