using UnityEngine;
using UnityEngine.UI;
using GoFish.Core;

public class BookFanView : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] Image card0;
    [SerializeField] Image card1;
    [SerializeField] Image card2;
    [SerializeField] Image card3;

    [Header("Fan Tuning")]
    [SerializeField] Vector2 offset0 = new Vector2(-18, -2);
    [SerializeField] Vector2 offset1 = new Vector2(-6,  2);
    [SerializeField] Vector2 offset2 = new Vector2( 6,  2);
    [SerializeField] Vector2 offset3 = new Vector2(18, -2);

    [SerializeField] float rot0 = -10f;
    [SerializeField] float rot1 = -3f;
    [SerializeField] float rot2 = 3f;
    [SerializeField] float rot3 = 10f;

    public void SetBook(CardSpriteLibrary lib, Rank rank)
    {
        SetImage(card0, lib.GetFace(new Card(rank, Suit.Clubs)),    offset0, rot0);
        SetImage(card1, lib.GetFace(new Card(rank, Suit.Diamonds)), offset1, rot1);
        SetImage(card2, lib.GetFace(new Card(rank, Suit.Hearts)),   offset2, rot2);
        SetImage(card3, lib.GetFace(new Card(rank, Suit.Spades)),   offset3, rot3);
    }

    static void SetImage(Image img, Sprite sprite, Vector2 offset, float rotZ)
    {
        if (img == null) return;
        img.sprite = sprite;
        var rt = img.rectTransform;
        rt.anchoredPosition = offset;
        rt.localRotation = Quaternion.Euler(0f, 0f, rotZ);
    }
}
