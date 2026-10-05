using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class SoundToggle : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI label;

    void Awake() => GetComponent<Button>().onClick.AddListener(Toggle);

    void OnEnable() => Refresh();

    void Toggle()
    {
        SfxPlayer.Muted = !SfxPlayer.Muted;
        Refresh();
        Sfx.Play(SfxId.Click);
    }

    void Refresh()
    {
        if (label != null) label.text = SfxPlayer.Muted ? "SOUND OFF" : "SOUND ON";
    }
}
