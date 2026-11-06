using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Автоматически локализует все TextMeshPro-тексты в любой сцене (без ручной привязки)
/// и показывает на всех сценах кнопку переключения языка RU/EN.
/// Создаётся сам при старте приложения и живёт между сценами.
/// </summary>
public sealed class LocalizationManager : MonoBehaviour
{
    sealed class Entry { public string original; public string applied; }

    readonly Dictionary<TMP_Text, Entry> entries = new Dictionary<TMP_Text, Entry>();
    readonly List<TMP_Text> dead = new List<TMP_Text>();
    TMP_Text buttonLabel;
    float nextScan;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (FindFirstObjectByType<LocalizationManager>() != null) return;
        var go = new GameObject("LocalizationManager");
        DontDestroyOnLoad(go);
        go.AddComponent<LocalizationManager>();
    }

    void Awake() { BuildLanguageButton(); Loc.LanguageChanged += OnLanguageChanged; }
    void OnDestroy() { Loc.LanguageChanged -= OnLanguageChanged; }

    void Update()
    {
        if (Time.unscaledTime < nextScan) return;
        nextScan = Time.unscaledTime + 0.2f;
        Scan();
    }

    void Scan()
    {
        foreach (var t in FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (t == null || t == buttonLabel) continue;
            string cur = t.text;
            if (string.IsNullOrEmpty(cur)) continue;

            if (!entries.TryGetValue(t, out var e))
            {
                if (!HasCyrillic(cur)) continue;
                e = new Entry { original = cur };
                entries[t] = e;
            }
            else if (cur != e.applied && cur != e.original)
            {
                // Текст изменил игровой код — запоминаем новое исходное значение.
                if (!HasCyrillic(cur)) { entries.Remove(t); continue; }
                e.original = cur;
            }
            Apply(t, e);
        }
        Cleanup();
    }

    void Apply(TMP_Text t, Entry e)
    {
        string target = Loc.IsEnglish ? Loc.ToEnglish(e.original) : e.original;
        e.applied = target;
        if (t.text != target) t.text = target;
    }

    void OnLanguageChanged()
    {
        foreach (var kv in entries) if (kv.Key != null) Apply(kv.Key, kv.Value);
        if (buttonLabel != null) buttonLabel.text = Loc.IsEnglish ? "EN" : "RU";
    }

    void Cleanup()
    {
        dead.Clear();
        foreach (var kv in entries) if (kv.Key == null) dead.Add(kv.Key);
        foreach (var k in dead) entries.Remove(k);
    }

    static bool HasCyrillic(string s)
    {
        foreach (char c in s) if (c >= 'Ѐ' && c <= 'ӿ') return true;
        return false;
    }

    void BuildLanguageButton()
    {
        var canvasGo = new GameObject("LanguageCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        var btnGo = new GameObject("LanguageButton", typeof(RectTransform), typeof(Image), typeof(Button));
        btnGo.transform.SetParent(canvasGo.transform, false);
        var rt = (RectTransform)btnGo.transform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 0f); // нижний левый угол
        rt.anchoredPosition = new Vector2(24, 24);
        rt.sizeDelta = new Vector2(110, 52);
        btnGo.GetComponent<Image>().color = new Color(0.08f, 0.2f, 0.35f, 0.85f);
        btnGo.GetComponent<Button>().onClick.AddListener(
            () => Loc.Language = Loc.IsEnglish ? Loc.Russian : Loc.English);

        var txtGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        txtGo.transform.SetParent(btnGo.transform, false);
        var trt = (RectTransform)txtGo.transform;
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one; trt.offsetMin = trt.offsetMax = Vector2.zero;
        buttonLabel = txtGo.GetComponent<TextMeshProUGUI>();
        buttonLabel.alignment = TextAlignmentOptions.Center;
        buttonLabel.fontSize = 30;
        buttonLabel.color = Color.white;
        buttonLabel.raycastTarget = false;
        buttonLabel.text = Loc.IsEnglish ? "EN" : "RU";

        if (FindFirstObjectByType<EventSystem>() == null)
            Debug.LogWarning("LocalizationManager: в сцене нет EventSystem — кнопка языка не будет кликабельна.");
    }
}
