using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DeckView : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] RectTransform stackRoot;
    [SerializeField] Image cardBackPrefab;
    [SerializeField] TextMeshProUGUI countText;

    [Header("Visuals")]
    [SerializeField] int maxVisible = 6;
    [SerializeField] Vector2 offsetPerCard = new Vector2(3f, 3f);
    [SerializeField] float rotationZ = 0f;
    [SerializeField] float visualScale = 1f;

    readonly List<Image> _pool = new();

    public void Render(int deckCount)
    {
        if (countText) countText.text = deckCount.ToString();

        if (stackRoot == null || cardBackPrefab == null) return;

        stackRoot.localScale = Vector3.one * visualScale;

        int visible = Mathf.Min(deckCount, maxVisible);
        EnsurePoolSize(visible);

        for (int i = 0; i < visible; i++)
        {
            var img = _pool[i];
            if (!img.gameObject.activeSelf) img.gameObject.SetActive(true);

            var rt = (RectTransform)img.transform;
            rt.SetParent(stackRoot, false);
            rt.anchoredPosition = offsetPerCard * i;
            rt.localRotation = Quaternion.Euler(0f, 0f, rotationZ);
            rt.SetSiblingIndex(i);
        }

        for (int i = visible; i < _pool.Count; i++)
            if (_pool[i].gameObject.activeSelf) _pool[i].gameObject.SetActive(false);
    }

    void EnsurePoolSize(int needed)
    {
        while (_pool.Count < needed)
        {
            var img = Instantiate(cardBackPrefab, stackRoot);
            img.raycastTarget = false;

            var cg = img.GetComponent<CanvasGroup>();
            if (cg == null) cg = img.gameObject.AddComponent<CanvasGroup>();
            cg.blocksRaycasts = false;

            _pool.Add(img);
        }
    }
}
