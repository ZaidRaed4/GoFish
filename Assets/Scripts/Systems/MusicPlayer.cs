using UnityEngine;

public class MusicPlayer : MonoBehaviour
{
    [SerializeField] AudioClip track;
    [SerializeField, Range(0f, 1f)] float masterVolume = 0.8f;
    [Tooltip("Seconds to move between scene volumes, and to mute or unmute")]
    [SerializeField] float fadeDuration = 1f;

    static MusicPlayer _instance;

    AudioSource _source;
    float _sceneVolume = 1f;
    float _volume;

    void Awake()
    {
        if (_instance != null)
        {
            enabled = false;
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);

        _source = gameObject.AddComponent<AudioSource>();
        _source.clip = track;
        _source.loop = true;
        _source.playOnAwake = false;
        _source.priority = 0;
        _source.spatialBlend = 0f;
        _source.volume = 0f;
        if (track != null) _source.Play();
    }

    void OnDestroy()
    {
        if (_instance == this) _instance = null;
    }

    public static void SetSceneVolume(float volume)
    {
        if (_instance != null) _instance._sceneVolume = Mathf.Clamp01(volume);
    }

    void Update()
    {
        float target = SfxPlayer.Muted ? 0f : _sceneVolume;
        _volume = Mathf.MoveTowards(_volume, target, Time.unscaledDeltaTime / Mathf.Max(0.01f, fadeDuration));
        _source.volume = masterVolume * _volume;
    }
}
