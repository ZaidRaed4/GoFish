using UnityEngine;
using GoFish.Core;

[CreateAssetMenu(menuName = "GoFish/Card Sprite Library")]
public class CardSpriteLibrary : ScriptableObject
{
    public Sprite cardBack;
    [Tooltip("Index = card.ToId0To51()  (Suit*13 + (Rank-2))")]
    public Sprite[] faces = new Sprite[52];

    public Sprite GetFace(Card card) => faces != null && faces.Length == 52 ? faces[card.ToId0To51()] : null;

    public void EnsureSize()
    {
        if (faces == null || faces.Length != 52)
            faces = new Sprite[52];
    }
}
