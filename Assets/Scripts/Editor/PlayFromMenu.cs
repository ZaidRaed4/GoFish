#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;

[InitializeOnLoad]
public static class PlayFromMenu
{
    const string PrefKey = "GoFish.PlayFromMainMenu";
    const string MenuPath = "GoFish/Play From Main Menu";

    static PlayFromMenu()
    {
        EditorApplication.delayCall += Apply;
    }

    static bool Enabled
    {
        get => EditorPrefs.GetBool(PrefKey, true);
        set => EditorPrefs.SetBool(PrefKey, value);
    }

    [MenuItem(MenuPath)]
    static void Toggle()
    {
        Enabled = !Enabled;
        Apply();
    }

    [MenuItem(MenuPath, true)]
    static bool ToggleValidate()
    {
        Menu.SetChecked(MenuPath, Enabled);
        return true;
    }

    static void Apply()
    {
        SceneAsset start = null;

        if (Enabled)
        {
            foreach (var scene in EditorBuildSettings.scenes)
            {
                if (!scene.enabled) continue;
                start = AssetDatabase.LoadAssetAtPath<SceneAsset>(scene.path);
                break;
            }
        }

        EditorSceneManager.playModeStartScene = start;
    }
}
#endif
