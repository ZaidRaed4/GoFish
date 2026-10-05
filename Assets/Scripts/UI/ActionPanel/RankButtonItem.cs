using TMPro;
using UnityEngine;
using UnityEngine.UI;
using GoFish.Core;

public class RankButtonItem : MonoBehaviour
{
    [SerializeField] Button button;
    [SerializeField] TextMeshProUGUI label;

    Rank _rank;
    PlayerActionPanel _panel;

    void Awake()
    {
        if (button == null) button = GetComponent<Button>();
        if (label == null) label = GetComponentInChildren<TextMeshProUGUI>(true);
    }

    public void Bind(PlayerActionPanel panel, Rank rank)
    {
        _panel = panel;
        _rank = rank;

        if (label != null)
        {
            label.text = Card.RankShort(rank);
            label.enabled = true;
            var color = label.color;
            color.a = 1f;
            label.color = color;
        }

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() =>
            {
                if (_panel != null)
                    _panel.OnRankButtonPressed(_rank);
            });
        }
    }
}
