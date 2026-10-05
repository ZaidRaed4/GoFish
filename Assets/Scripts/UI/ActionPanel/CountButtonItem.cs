using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CountButtonItem : MonoBehaviour
{
    [SerializeField] Button button;
    [SerializeField] TextMeshProUGUI label;

    int _count;
    PlayerActionPanel _panel;

    public int Count => _count;

    void Awake()
    {
        if (button == null) button = GetComponent<Button>();
        if (label == null) label = GetComponentInChildren<TextMeshProUGUI>(true);
    }

    public void Bind(PlayerActionPanel panel, int count)
    {
        _panel = panel;
        _count = count;

        if (label != null) label.text = count.ToString();

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() =>
            {
                if (_panel != null) _panel.OnCountButtonPressed(_count);
            });
        }
    }
}
