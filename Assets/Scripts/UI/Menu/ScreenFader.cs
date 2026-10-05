using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ScreenFader : MonoBehaviour
{
    [Tooltip("Saved switched off so it doesn't hide the scene while editing")]
    [SerializeField] Image cover;
    [SerializeField] float fadeInDuration = 0.3f;
    [SerializeField] float fadeOutDuration = 0.25f;

    static ScreenFader _current;
    bool _leaving;

    void Awake()
    {
        _current = this;
        Show(1f);
    }

    void OnDestroy()
    {
        if (_current == this) _current = null;
    }

    IEnumerator Start()
    {
        yield return null;
        yield return Tween.Run(fadeInDuration, k => Show(1f - k), unscaledTime: true);
    }

    public static void LoadScene(string sceneName)
    {
        if (_current == null)
        {
            SceneManager.LoadScene(sceneName);
            return;
        }

        if (_current._leaving) return;
        _current._leaving = true;
        _current.StopAllCoroutines();
        _current.StartCoroutine(_current.FadeOutAndLoad(sceneName));
    }

    IEnumerator FadeOutAndLoad(string sceneName)
    {
        float from = cover.color.a;
        yield return Tween.Run(fadeOutDuration, k => Show(Mathf.Lerp(from, 1f, k)), unscaledTime: true);
        SceneManager.LoadScene(sceneName);
    }

    void Show(float alpha)
    {
        var color = cover.color;
        color.a = alpha;
        cover.color = color;
        cover.enabled = alpha > 0f;
    }
}
