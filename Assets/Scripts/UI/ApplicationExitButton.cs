using UnityEngine;
using UnityEngine.UI;

/// <summary>Connects an editor-authored exit button to the player application.</summary>
public sealed class ApplicationExitButton : MonoBehaviour
{
    [SerializeField] Button exitButton;

    void OnEnable()
    {
        if (exitButton != null) exitButton.onClick.AddListener(Exit);
    }

    void OnDisable()
    {
        if (exitButton != null) exitButton.onClick.RemoveListener(Exit);
    }

    static void Exit() => Application.Quit();
}
