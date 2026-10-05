using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using GoFish.Core;

public class OpponentHandView : MonoBehaviour
{
    [Header("Stack Visuals")]
    [SerializeField] RectTransform stackRoot;
    [SerializeField] Image cardBackPrefab;
    [SerializeField] CardSpriteLibrary spriteLibrary;

    [Tooltip("Most card backs drawn; the ID card shows the real count")]
    [SerializeField] int maxVisible = 8;

    [Tooltip("Local offset per card (x,y). Example top: (14,0). Left/right: (0,14).")]
    [SerializeField] Vector2 offsetPerCard = new Vector2(14f, 0f);

    [Tooltip("Optional rotation for each back (degrees). Example: Left/Right = 90.")]
    [SerializeField] float cardRotationZ = 0f;

    [Tooltip("Optional scale for the whole stack visuals.")]
    [SerializeField] float visualScale = 1f;

    readonly List<Image> _pool = new();

    public void Render(PlayerState player)
    {
        if (player == null || stackRoot == null || cardBackPrefab == null) return;

        stackRoot.localScale = Vector3.one * visualScale;

        int visible = Mathf.Min(player.Hand.Count, maxVisible);
        EnsurePoolSize(visible);

        for (int i = 0; i < visible; i++)
        {
            var img = _pool[i];
            if (!img.gameObject.activeSelf) img.gameObject.SetActive(true);

            var rt = (RectTransform)img.transform;
            rt.SetParent(stackRoot, false);
            rt.anchoredPosition = offsetPerCard * i;
            rt.localRotation = Quaternion.Euler(0f, 0f, cardRotationZ);
            rt.SetAsLastSibling();
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

            if (spriteLibrary != null && spriteLibrary.cardBack != null)
                img.sprite = spriteLibrary.cardBack;

            _pool.Add(img);
        }
    }
}
