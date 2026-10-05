using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using GoFish.Core;

public class LayAreaView : MonoBehaviour
{
    [Header("Wiring")]
    public RectTransform content;
    [SerializeField] BookFanView bookPrefab;
    [SerializeField] CardSpriteLibrary spriteLibrary;

    [Header("Book Size (optional override)")]
    [SerializeField] bool overrideSize = false;
    [SerializeField] Vector2 preferredSize = new Vector2(220f, 140f);

    [Tooltip("Overlap books more when the row is full, so they never spill out of the slot.")]
    [SerializeField] bool squeezeToFit = false;

    readonly Dictionary<Rank, BookFanView> _books = new Dictionary<Rank, BookFanView>(13);
    float _baseSpacing = float.NaN;

    public bool HasBook(Rank rank) => _books.ContainsKey(rank);

    public void EnsureBook(Rank rank, bool animatePop)
    {
        if (_books.ContainsKey(rank)) return;
        if (content == null || bookPrefab == null || spriteLibrary == null) return;

        var view = Instantiate(bookPrefab, content);
        ApplySize(view);
        view.SetBook(spriteLibrary, rank);
        _books[rank] = view;

        if (squeezeToFit) Squeeze();

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(content);
        Canvas.ForceUpdateCanvases();

        if (animatePop)
            StartCoroutine(PopIn(view.gameObject));
    }

    public void SyncFromPlayer(PlayerState player, bool animateNew)
    {
        foreach (var rank in player.BooksLaid)
            EnsureBook(rank, animateNew);
    }

    void ApplySize(BookFanView view)
    {
        if (!overrideSize) return;

        var rt = (RectTransform)view.transform;
        rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, preferredSize.x);
        rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, preferredSize.y);

        var le = view.GetComponent<LayoutElement>();
        if (le == null) le = view.gameObject.AddComponent<LayoutElement>();

        le.ignoreLayout = false;
        le.preferredWidth = le.minWidth = preferredSize.x;
        le.preferredHeight = le.minHeight = preferredSize.y;
        le.flexibleWidth = 0f;
        le.flexibleHeight = 0f;
    }

    void Squeeze()
    {
        var row = content.GetComponent<HorizontalLayoutGroup>();
        if (row == null) return;

        if (float.IsNaN(_baseSpacing)) _baseSpacing = row.spacing;

        float item = preferredSize.x;
        int n = _books.Count;
        float step = item + _baseSpacing;
        if (n > 1) step = Mathf.Min(step, (content.rect.width - item) / (n - 1));

        row.spacing = step - item;
    }

    const float PopDuration = 1f / 9f;

    IEnumerator PopIn(GameObject go)
    {
        var cg = go.GetComponent<CanvasGroup>();
        if (cg == null) cg = go.AddComponent<CanvasGroup>();

        // Relative to the prefab's own scale (the bots' fans are scaled down)
        Vector3 baseScale = go.transform.localScale;

        cg.alpha = 0f;
        go.transform.localScale = baseScale * 0.92f;

        yield return Tween.Run(PopDuration, k =>
        {
            cg.alpha = k;
            go.transform.localScale = Vector3.Lerp(baseScale * 0.92f, baseScale, k);
        });
    }
}
