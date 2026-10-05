using UnityEngine;

public class SceneMusic : MonoBehaviour
{
    [SerializeField, Range(0f, 1f)] float volume = 1f;

    void Start() => MusicPlayer.SetSceneVolume(volume);

    void OnValidate()
    {
        if (Application.isPlaying && isActiveAndEnabled) MusicPlayer.SetSceneVolume(volume);
    }
}
