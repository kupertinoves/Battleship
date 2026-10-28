using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// Local overlay only: opening it does not pause the remote match or send messages.
public class BattlePauseController : MonoBehaviour
{
    public static bool IsPaused { get; private set; }
    public GameObject overlay;
    public Button resumeButton;
    public CanvasGroup[] fields;
    bool[] previousInteractable;
    bool[] previousRaycasts;
    GameObject previousSelection;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetState() { IsPaused = false; }

    void Update()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) Toggle();
#elif ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(KeyCode.Escape)) Toggle();
#endif
    }

    public void Toggle()
    {
        if (IsPaused) Resume();
        else Pause();
    }

    public void Pause()
    {
        if (IsPaused || overlay == null) return;
        IsPaused = true;
        previousSelection = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
        previousInteractable = new bool[fields.Length];
        previousRaycasts = new bool[fields.Length];
        for (int i = 0; i < fields.Length; i++)
        {
            if (fields[i] == null) continue;
            previousInteractable[i] = fields[i].interactable;
            previousRaycasts[i] = fields[i].blocksRaycasts;
            fields[i].interactable = false;
            fields[i].blocksRaycasts = false;
        }
        overlay.transform.SetAsLastSibling();
        overlay.SetActive(true);
        if (EventSystem.current != null && resumeButton != null)
            EventSystem.current.SetSelectedGameObject(resumeButton.gameObject);
    }

    public void Resume()
    {
        if (!IsPaused) return;
        IsPaused = false;
        if (overlay != null) overlay.SetActive(false);
        for (int i = 0; i < fields.Length; i++)
        {
            if (fields[i] == null) continue;
            fields[i].interactable = previousInteractable[i];
            fields[i].blocksRaycasts = previousRaycasts[i];
        }
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(previousSelection);
    }

    void OnDisable() { Resume(); }
}
