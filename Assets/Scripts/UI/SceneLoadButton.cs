using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Routes an editor-authored button to a scene in Build Settings.</summary>
public sealed class SceneLoadButton : MonoBehaviour
{
    [SerializeField] Button button;
    [SerializeField] string sceneName;

    void OnEnable()
    {
        if (button == null) button = GetComponent<Button>();
        if (button != null) button.onClick.AddListener(LoadScene);
    }

    void OnDisable()
    {
        if (button != null) button.onClick.RemoveListener(LoadScene);
    }

    void LoadScene()
    {
        if (!string.IsNullOrWhiteSpace(sceneName)) SceneManager.LoadScene(sceneName);
    }
}
