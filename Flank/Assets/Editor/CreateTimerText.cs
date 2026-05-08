using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class CreateTimerText
{
    [MenuItem("Game/Setup Timer Text")]
    private static void Setup()
    {
        GameObject panel = GameObject.Find("GameStatePanel");
        if (panel == null)
        {
            Debug.LogError("[TimerSetup] GameStatePanel not found in scene. Open GameScene first.");
            return;
        }

        // Remove any old single-clock TimerText from a previous setup.
        Transform old = panel.transform.Find("TimerText");
        if (old != null)
        {
            Object.DestroyImmediate(old.gameObject);
            Debug.Log("[TimerSetup] Removed old single TimerText object.");
        }

        string fontPath = AssetDatabase.GUIDToAssetPath("8f586378b4e144a9851e7b34d9b748ee");
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(fontPath);

        TextMeshProUGUI attTmp = GetOrCreateClock(panel, "AttackerTimerText", -237.73f, font);
        TextMeshProUGUI defTmp = GetOrCreateClock(panel, "DefenderTimerText", -307.73f, font);

        AssignToControllers(attTmp, defTmp);

        EditorSceneManager.MarkSceneDirty(panel.scene);
        Debug.Log("[TimerSetup] Attacker and Defender timer clocks created and wired.");
    }

    private static TextMeshProUGUI GetOrCreateClock(GameObject panel, string name, float y, TMP_FontAsset font)
    {
        Transform existing = panel.transform.Find(name);
        if (existing != null)
        {
            Debug.Log($"[TimerSetup] {name} already exists — reusing.");
            return existing.GetComponent<TextMeshProUGUI>();
        }

        GameObject go = new GameObject(name);
        go.layer = panel.gameObject.layer;
        go.transform.SetParent(panel.transform, false);

        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(0f, y);
        rt.sizeDelta = new Vector2(403.95f, 70f);

        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        if (font != null) tmp.font = font;
        tmp.text = "0:00";
        tmp.fontSize = 56;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Left;
        tmp.richText = true;

        go.SetActive(false);
        return tmp;
    }

    private static void AssignToControllers(TextMeshProUGUI attTmp, TextMeshProUGUI defTmp)
    {
        GameHudController hud = Object.FindFirstObjectByType<GameHudController>(FindObjectsInactive.Include);
        if (hud != null)
        {
            SerializedObject so = new SerializedObject(hud);
            so.FindProperty("_attackerTimerText").objectReferenceValue = attTmp;
            so.FindProperty("_defenderTimerText").objectReferenceValue = defTmp;
            so.ApplyModifiedProperties();
            Debug.Log("[TimerSetup] Wired to GameHudController.");
        }
        else
        {
            Debug.LogWarning("[TimerSetup] GameHudController not found in scene.");
        }

        LanGameHudController lanHud = Object.FindFirstObjectByType<LanGameHudController>(FindObjectsInactive.Include);
        if (lanHud != null)
        {
            SerializedObject so = new SerializedObject(lanHud);
            so.FindProperty("attackerTimerText").objectReferenceValue = attTmp;
            so.FindProperty("defenderTimerText").objectReferenceValue = defTmp;
            so.ApplyModifiedProperties();
            Debug.Log("[TimerSetup] Wired to LanGameHudController.");
        }
        else
        {
            Debug.LogWarning("[TimerSetup] LanGameHudController not found in scene.");
        }
    }
}
