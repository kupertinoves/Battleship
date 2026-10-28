using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Runtime scene hook for non-visual client state. Screen layout and styling are
/// authored in scenes and prefabs; this class does not create or style UI.
/// </summary>
public static partial class UIRuntimePolish
{
    static bool initialized;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetState()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        initialized = false;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Initialize()
    {
        if (initialized) return;
        initialized = true;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "WIn") LocalStatistics.RecordWin();
        else if (scene.name == "Defeat") LocalStatistics.RecordLoss();
    }
}
