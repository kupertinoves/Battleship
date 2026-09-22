using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;
using System.Collections;
using TMPro;


public class ChangePassword : MonoBehaviour
{

    [Header("API")]
    public string AuthUrl = "http://localhost:8000";

    [Header("OldPassword")]
    public TMP_InputField OldPassword;

    [Header("NewPassword")]
    public TMP_InputField NewPassword;

    [Header("Error")]
    public TMP_Text Error;

    public void OnSubmit()
    {
        StartCoroutine(ChangePass());
    }

    IEnumerator ChangePass()

    {
        Error.text="";
        string token = TokenStorage.Load();
        if (string.IsNullOrEmpty(token))
        {
            SceneManager.LoadScene("Auth");
            yield break;
        }

        var data = new ChangePasswordSerial
        {
            old_password = OldPassword.text,
            new_password = NewPassword.text,
        };
        string json = JsonUtility.ToJson(data);
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
        var request = new UnityWebRequest(AuthUrl, "POST");
        // ✅ Проверяем токен
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader(
            "Authorization",
            "Bearer " + token
        );

        yield return request.SendWebRequest();
        if (request.responseCode==409){
            Debug.Log("Incorrect pass");
            var err = JsonUtility.FromJson<ChangePasswordErrorDetail>(request.downloadHandler.text);

            Error.text = err.detail;
            yield break;
        }
        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Token valid");
            Debug.Log(request.downloadHandler.text);
            SceneManager.LoadScene("Account");
        }
        else
        {
            Debug.Log("Token invalid: " + request.responseCode);

            // ❌ Токен протух / невалиден
            TokenStorage.Clear();
            SceneManager.LoadScene("Auth");
        }
    }
}
[System.Serializable]
public class ChangePasswordSerial
{
    public string old_password;
    public string new_password;
}
[System.Serializable]
public class ChangePasswordErrorDetail
{
    public string detail;
}