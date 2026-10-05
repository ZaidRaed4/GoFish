#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using GoFish.Core;

// Inspector buttons that fill a card library from a folder of face sprites
[CustomEditor(typeof(CardSpriteLibrary))]
public class CardSpriteLibraryAutoFill : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var lib = (CardSpriteLibrary)target;

        GUILayout.Space(8);

        if (GUILayout.Button("Ensure faces[52]"))
        {
            lib.EnsureSize();
            EditorUtility.SetDirty(lib);
        }

        if (GUILayout.Button("Auto-Fill faces[] from selected folder (card_2C or C_2 names)"))
        {
            AutoFill(lib);
            EditorUtility.SetDirty(lib);
        }
    }

    static void AutoFill(CardSpriteLibrary lib)
    {
        lib.EnsureSize();

        var path = AssetDatabase.GetAssetPath(Selection.activeObject);
        if (string.IsNullOrEmpty(path) || !AssetDatabase.IsValidFolder(path))
        {
            Debug.LogError("Select the folder that holds the 52 face sprites in the Project window, then click Auto-Fill.");
            return;
        }

        int filled = 0;
        foreach (var guid in AssetDatabase.FindAssets("t:Sprite", new[] { path }))
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath(guid));
            if (sprite == null || !TryParseName(sprite.name, out Suit suit, out Rank rank)) continue;

            lib.faces[new Card(rank, suit).ToId0To51()] = sprite;
            filled++;
        }

        Debug.Log($"Auto-Fill done. Assigned {filled} sprites. Make sure none are missing.");
    }

    static bool TryParseName(string name, out Suit suit, out Rank rank)
    {
        suit = default;
        rank = default;

        string upper = name.Trim().ToUpperInvariant();
        string suitText, rankText;

        if (upper.StartsWith("CARD_") && upper.Length >= 7)
        {
            rankText = upper.Substring(5, upper.Length - 6);
            suitText = upper.Substring(upper.Length - 1);
        }
        else
        {
            var parts = upper.Split('_');
            if (parts.Length != 2) return false;
            suitText = parts[0];
            rankText = parts[1];
        }

        return TryParseSuit(suitText, out suit) && TryParseRank(rankText, out rank);
    }

    static bool TryParseSuit(string text, out Suit suit)
    {
        int index = "CDHS".IndexOf(text);
        suit = (Suit)Mathf.Max(0, index);
        return text.Length == 1 && index >= 0;
    }

    static bool TryParseRank(string text, out Rank rank)
    {
        int value = text switch
        {
            "J" => 11,
            "Q" => 12,
            "K" => 13,
            "A" => 14,
            _ => int.TryParse(text, out int n) ? n : 0
        };

        rank = (Rank)value;
        return value >= 2 && value <= 14;
    }
}
#endif
