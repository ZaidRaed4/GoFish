using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TurnOverlayView : MonoBehaviour
{
    public enum ValueState { Hidden, Neutral, Correct, Wrong }

    [Header("Root")]
    [SerializeField] CanvasGroup rootGroup;
    [SerializeField] GameObject dimmer;
    [SerializeField] RectTransform panelRect;

    [Header("Header Moving Groups")]
    [SerializeField] RectTransform askerGroup;
    [SerializeField] RectTransform targetGroup;
    [SerializeField] RectTransform vsGroup;

    [Header("Header Text")]
    [SerializeField] TextMeshProUGUI askerNameText;
    [SerializeField] TextMeshProUGUI targetNameText;

    [Header("Header Portraits")]
    [SerializeField] Image askerAvatar;
    [SerializeField] Image targetAvatar;
    [SerializeField] TableSeats seats;

    [Header("Anchors (positions)")]
    [SerializeField] RectTransform askerIntroAnchor;
    [SerializeField] RectTransform vsIntroAnchor;
    [SerializeField] RectTransform targetIntroAnchor;

    [SerializeField] RectTransform askerTopAnchor;
    [SerializeField] RectTransform vsTopAnchor;
    [SerializeField] RectTransform targetTopAnchor;

    [Header("Body")]
    [SerializeField] TextMeshProUGUI leftLabelText;
    [SerializeField] GameObject valueBoxRoot;
    [SerializeField] Image valueBg;
    [SerializeField] TextMeshProUGUI valueText;

    [Header("Answer")]
    [Tooltip("YES / NO - GO FISH / RIGHT / WRONG, next to the value box")]
    [SerializeField] TextMeshProUGUI answerText;

    [Header("Section Roots")]
    [Tooltip("Label and value box")]
    [SerializeField] GameObject bodyRoot;
    [Tooltip("Rank, count and suit rows")]
    [SerializeField] GameObject optionsRoot;

    [Header("Value Colors")]
    [SerializeField] Color neutralColor = new Color(1f, 1f, 1f, 1f);
    [SerializeField] Color correctColor = new Color(0.15f, 0.65f, 0.20f, 1f);
    [SerializeField] Color wrongColor = new Color(0.85f, 0.10f, 0.10f, 1f);

    public bool HeaderIsTop { get; private set; }

    public void Init(TurnOverlayAnimator animator)
    {
        if (rootGroup != null)
        {
            rootGroup.gameObject.SetActive(true);

            var tap = rootGroup.GetComponent<OverlayTapToSkip>();
            if (tap == null) tap = rootGroup.gameObject.AddComponent<OverlayTapToSkip>();
            tap.animator = animator;
        }

        SetVisibleInstant(false, false);
    }

    public void SetVisibleInstant(bool visible, bool blockInput)
    {
        if (rootGroup == null) return;

        rootGroup.alpha = visible ? 1f : 0f;

        rootGroup.interactable = visible && blockInput;
        rootGroup.blocksRaycasts = visible && blockInput;

        if (dimmer != null) dimmer.SetActive(visible);
    }

    public void Hide()
    {
        SetVisibleInstant(false, false);
        HeaderIsTop = false;
    }

    public void SetHeaderNames(string asker, string target)
    {
        if (askerNameText) askerNameText.text = (asker ?? "").ToUpperInvariant();
        if (targetNameText) targetNameText.text = (target ?? "").ToUpperInvariant();
    }

    public void SetHeaderPortraits(int askerId, int targetId)
    {
        if (askerAvatar) askerAvatar.sprite = Portrait(askerId);
        if (targetAvatar) targetAvatar.sprite = Portrait(targetId);
    }

    Sprite Portrait(int playerId) => seats != null ? seats.Portrait(playerId) : null;

    public void SetLeftLabel(string text)
    {
        if (leftLabelText) leftLabelText.text = (text ?? "").ToUpperInvariant();
    }

    public void SetValue(string text, ValueState state)
    {
        bool show = state != ValueState.Hidden;

        if (state == ValueState.Hidden || state == ValueState.Neutral) SetAnswer(null, state);

        if (valueBoxRoot) valueBoxRoot.SetActive(show);

        if (!show) return;

        if (valueText) valueText.text = (text ?? "").ToUpperInvariant();

        if (valueBg != null)
            valueBg.color = state == ValueState.Correct ? correctColor : state == ValueState.Wrong ? wrongColor : neutralColor;
    }

    public void SetAnswer(string text, ValueState state)
    {
        if (answerText == null) return;

        bool show = !string.IsNullOrEmpty(text) && (state == ValueState.Correct || state == ValueState.Wrong);
        answerText.gameObject.SetActive(show);
        if (!show) return;

        answerText.text = text;
        answerText.color = state == ValueState.Correct ? correctColor : wrongColor;
    }

    public void SnapHeaderToTop()
    {
        if (askerGroup && askerTopAnchor) askerGroup.anchoredPosition = askerTopAnchor.anchoredPosition;
        if (vsGroup && vsTopAnchor) vsGroup.anchoredPosition = vsTopAnchor.anchoredPosition;
        if (targetGroup && targetTopAnchor) targetGroup.anchoredPosition = targetTopAnchor.anchoredPosition;
        HeaderIsTop = true;
    }

    public void SetHeaderOnly(bool headerOnly)
    {
        if (bodyRoot) bodyRoot.SetActive(!headerOnly);
        if (optionsRoot) optionsRoot.SetActive(!headerOnly);
    }

    public void ShowAtTop(bool blockInput)
    {
        SetVisibleInstant(true, blockInput);
        if (HeaderIsTop) SnapHeaderToTop();
        if (vsGroup != null) vsGroup.localScale = Vector3.one;
    }

    public void PrepareIntro(bool blockInput)
    {
        if (rootGroup == null) return;

        rootGroup.alpha = 0f;
        SetHeaderOnly(true);
        SetLeftLabel("");
        SetValue("", ValueState.Hidden);

        rootGroup.interactable = blockInput;
        rootGroup.blocksRaycasts = blockInput;
        if (dimmer != null) dimmer.SetActive(true);
        HeaderIsTop = false;

        float width = panelRect != null ? panelRect.rect.width : 960f;
        float introY = askerIntroAnchor != null ? askerIntroAnchor.anchoredPosition.y : 0f;

        if (askerGroup != null) askerGroup.anchoredPosition = new Vector2(-width * 0.6f, introY);
        if (targetGroup != null) targetGroup.anchoredPosition = new Vector2(width * 0.6f, introY);

        if (vsGroup != null && vsIntroAnchor != null)
        {
            vsGroup.anchoredPosition = vsIntroAnchor.anchoredPosition;
            vsGroup.localScale = Vector3.zero;
        }
    }

    public IEnumerator PlayIntro(float fadeIn, float slideIn, float vsPop, float hold, float lift)
    {
        SetHeaderOnly(true);
        SetLeftLabel("");
        SetValue("", ValueState.Hidden);

        yield return Fade(0f, 1f, fadeIn);
        yield return MoveHeaderTo(askerIntroAnchor, vsIntroAnchor, targetIntroAnchor, slideIn);

        if (vsGroup != null)
            yield return PopScale(vsGroup, 0f, 1f, vsPop);

        yield return new WaitForSeconds(hold);
        yield return MoveHeaderTo(askerTopAnchor, vsTopAnchor, targetTopAnchor, lift);

        HeaderIsTop = true;
        SetHeaderOnly(false);
        SetValue("", ValueState.Neutral);
    }

    public IEnumerator Fade(float from, float to, float duration)
    {
        if (rootGroup == null) yield break;

        rootGroup.alpha = from;
        if (dimmer != null) dimmer.SetActive(to > 0.01f || from > 0.01f);

        yield return Tween.Run(duration, k => rootGroup.alpha = Mathf.Lerp(from, to, k));
    }

    public IEnumerator FadeOutAndHide(float duration)
    {
        if (vsGroup != null) vsGroup.localScale = Vector3.one;

        yield return Fade(1f, 0f, duration);
        Hide();
    }

    public IEnumerator PopValue(float fromScale, float duration)
    {
        if (valueBoxRoot != null)
            yield return PopScale((RectTransform)valueBoxRoot.transform, fromScale, 1f, duration);
    }

    static IEnumerator PopScale(RectTransform rt, float from, float to, float duration)
    {
        if (rt == null) yield break;

        yield return Tween.Run(duration, k =>
        {
            float scale = Mathf.Lerp(from, to, Tween.EaseOutCubic(k));
            rt.localScale = new Vector3(scale, scale, 1f);
        });
    }

    IEnumerator MoveHeaderTo(RectTransform askerTo, RectTransform vsTo, RectTransform targetTo, float duration)
    {
        if (askerTo == null || vsTo == null || targetTo == null) yield break;

        Vector2 askerFrom = askerGroup.anchoredPosition;
        Vector2 vsFrom = vsGroup.anchoredPosition;
        Vector2 targetFrom = targetGroup.anchoredPosition;

        yield return Tween.Run(duration, t =>
        {
            float k = Tween.EaseOutCubic(t);
            askerGroup.anchoredPosition = Vector2.LerpUnclamped(askerFrom, askerTo.anchoredPosition, k);
            vsGroup.anchoredPosition = Vector2.LerpUnclamped(vsFrom, vsTo.anchoredPosition, k);
            targetGroup.anchoredPosition = Vector2.LerpUnclamped(targetFrom, targetTo.anchoredPosition, k);
        });
    }
}
