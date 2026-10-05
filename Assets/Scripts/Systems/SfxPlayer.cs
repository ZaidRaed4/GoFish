using UnityEngine;

public enum SfxId { Deal, Draw, Transfer, Click, Yes, No, Book, YourTurn, Win, Lose }

public class SfxPlayer : MonoBehaviour
{
    [System.Serializable]
    public struct Entry
    {
        public SfxId id;
        public AudioClip clip;
        [Tooltip("Optional extra takes; each play picks one of all the takes at random")]
        public AudioClip[] moreTakes;
        [Range(0f, 1f)] public float volume;
        [Tooltip("Random pitch change +/- this amount, so repeated sounds don't feel robotic.")]
        public float pitchJitter;
    }

    [SerializeField] Entry[] sounds;
    [SerializeField, Range(1, 12)] int voices = 6;

    const string MutedKey = "sound.muted";
    static SfxPlayer _instance;
    static int _muted = -1; // -1 = not read yet

    AudioSource[] _sources;
    int _next;

    public static bool Muted
    {
        get
        {
            if (_muted < 0) _muted = PlayerPrefs.GetInt(MutedKey, 0);
            return _muted == 1;
        }
        set
        {
            _muted = value ? 1 : 0;
            PlayerPrefs.SetInt(MutedKey, _muted);
        }
    }

    void Awake()
    {
        _instance = this;

        _sources = new AudioSource[voices];
        for (int i = 0; i < voices; i++)
        {
            var src = gameObject.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.spatialBlend = 0f;
            _sources[i] = src;
        }
    }

    void OnDestroy()
    {
        if (_instance == this) _instance = null;
    }

    public static void Play(SfxId id)
    {
        if (_instance != null && !Muted) _instance.PlayNow(id);
    }

    void PlayNow(SfxId id)
    {
        for (int i = 0; i < sounds.Length; i++)
        {
            if (sounds[i].id != id || sounds[i].clip == null) continue;

            var src = _sources[_next];
            _next = (_next + 1) % _sources.Length;

            float j = sounds[i].pitchJitter;
            src.pitch = 1f + (j > 0f ? Random.Range(-j, j) : 0f);
            src.PlayOneShot(PickTake(sounds[i]), sounds[i].volume);
            return;
        }
    }

    static AudioClip PickTake(Entry entry)
    {
        int extra = entry.moreTakes != null ? entry.moreTakes.Length : 0;
        int pick = Random.Range(0, extra + 1);
        if (pick == 0) return entry.clip;

        var take = entry.moreTakes[pick - 1];
        return take != null ? take : entry.clip;
    }
}

public static class Sfx
{
    public static void Play(SfxId id) => SfxPlayer.Play(id);
}
