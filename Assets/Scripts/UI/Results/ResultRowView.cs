using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ResultRowView : MonoBehaviour
{
    [SerializeField] Image portrait;
    [SerializeField] TextMeshProUGUI nameText;
    [SerializeField] TextMeshProUGUI booksText;
    [SerializeField] GameObject winnerMark;

    public void Set(Sprite face, string playerName, int books, bool winner)
    {
        if (portrait != null) portrait.sprite = face;
        if (nameText != null) nameText.text = playerName;
        if (booksText != null) booksText.text = books.ToString();
        if (winnerMark != null) winnerMark.SetActive(winner);
    }
}
