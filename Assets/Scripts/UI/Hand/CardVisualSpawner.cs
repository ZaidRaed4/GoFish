using UnityEngine;

[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(CardDragSnap))]
public class CardVisualSpawner : MonoBehaviour
{
    [Header("Visual Prefab")]
    [SerializeField] CardVisualFollower visualPrefab;

    [Header("Parent (optional)")]
    [SerializeField] RectTransform visualParent;
    [SerializeField] string visualLayerTag = "VisualLayer";

    [Header("Behavior")]
    [SerializeField] bool matchBaseSize = true;

    CardVisualFollower _visualInstance;
    RectTransform _baseRt;
    CardDragSnap _baseCard;

    public CardVisualFollower VisualInstance => _visualInstance;


    void Awake()
    {
        _baseRt = GetComponent<RectTransform>();
        _baseCard = GetComponent<CardDragSnap>();
    }

    void OnEnable()
    {
        EnsureSpawned();
    }

    void OnDisable()
    {
        DespawnVisual();
    }

    void EnsureSpawned()
    {
        if (_visualInstance != null) return;

        if (visualPrefab == null)
        {
            Debug.LogWarning("CardVisualSpawner: No visualPrefab assigned.");
            return;
        }

        if (visualParent == null)
            visualParent = FindVisualParent();

        if (visualParent == null)
        {
            Debug.LogWarning("CardVisualSpawner: Could not find a visual parent.");
            return;
        }

        _visualInstance = Instantiate(visualPrefab, visualParent);
        var visRt = (RectTransform)_visualInstance.transform;

        visRt.position = _baseRt.position;
        visRt.rotation = _baseRt.rotation;

        if (matchBaseSize)
        {
            visRt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, _baseRt.rect.width);
            visRt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, _baseRt.rect.height);
        }

        _visualInstance.Bind(_baseCard);
    }

    public void DespawnVisual()
    {
        if (_visualInstance == null) return;

        if (Application.isPlaying) Destroy(_visualInstance.gameObject);
        else DestroyImmediate(_visualInstance.gameObject);

        _visualInstance = null;
    }

    RectTransform FindVisualParent()
    {
        if (!string.IsNullOrEmpty(visualLayerTag))
        {
            var go = GameObject.FindWithTag(visualLayerTag);
            if (go != null)
                return go.GetComponent<RectTransform>();
        }

        var c = GetComponentInParent<Canvas>();
        return c != null ? (RectTransform)c.transform : null;
    }

    void OnDestroy()
    {
        DespawnVisual();
    }
}
