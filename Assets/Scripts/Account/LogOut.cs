using UnityEngine;
using UnityEngine.SceneManagement;

public class LogOut :MonoBehaviour{
    public void OnSubmit(){
        TokenStorage.Clear();
        SceneManager.LoadScene("MainMenu");
    }
}