// SceneA.
// SceneA is given the sceneName which will
// load SceneB from the Build Settings

using UnityEngine;
using UnityEngine.SceneManagement;

public class Change_scene : MonoBehaviour
{
    void Start()
    {
        Debug.Log("LoadMainScene");
    }

    public void LoadSceneByName(string Scene)
    {
        Debug.Log("sceneName to load: " + Scene);
        SceneManager.LoadScene(Scene);
    }
}