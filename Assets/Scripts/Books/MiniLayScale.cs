using UnityEngine;

public class MiniLayScale : MonoBehaviour
{
    [Range(0.4f, 1f)]
    [SerializeField] float scale = 0.5f;

    void Awake() => Apply();

#if UNITY_EDITOR
    void OnValidate() => Apply();
#endif

    void Apply() => transform.localScale = new Vector3(scale, scale, 1f);
}
