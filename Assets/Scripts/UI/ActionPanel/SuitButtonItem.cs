using TMPro;
using UnityEngine;
using UnityEngine.UI;
using GoFish.Core;

public class SuitButtonItem : MonoBehaviour
{
    [SerializeField] Button button;
    [SerializeField] TextMeshProUGUI label;
    [SerializeField] GameObject selectedGlow;

    public Suit SuitValue { get; private set; }

    PlayerActionPanel _panel;

    void Awake()
    {
        if (button == null) button = GetComponent<Button>();
        if (label == null) label = GetComponentInChildren<TextMeshProUGUI>(true);
    }

    public void Bind(PlayerActionPanel panel, Suit suit, string text)
    {
        _panel = panel;
        SuitValue = suit;

        if (label != null)
        {
            label.text = text;
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
                if (_panel != null) _panel.ToggleSuit(SuitValue);
            });
        }

        SetSelected(false);
    }

    public void SetSelected(bool selected)
    {
        if (selectedGlow != null) selectedGlow.SetActive(selected);
    }

    public void SetInteractable(bool canClick)
    {
        if (button != null) button.interactable = canClick;
    }
}
