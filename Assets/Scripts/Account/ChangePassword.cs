using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;
using System.Collections;
using TMPro;


public class ChangePassword : MonoBehaviour
{

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
        var request = new UnityWebRequest(ServerConfig.ChangePasswordUrl, "POST");
        // ✅ Проверяем токен
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader(
            "Authorization",
            "Bearer " + token
        );

        request.timeout = 15;
        yield return request.SendWebRequest();
        if (request.result == UnityWebRequest.Result.ConnectionError)
        {
            Error.text = "Нет соединения с сервером";
            yield break;
        }
        if (request.responseCode==409){
            Debug.Log("Incorrect pass");
            var err = JsonUtility.FromJson<ChangePasswordErrorDetail>(request.downloadHandler.text);

            Error.text = err.detail;
            yield break;
        }
        if (request.result == UnityWebRequest.Result.Success)
        {
            SceneManager.LoadScene("Account");
        }
        else if (request.responseCode == 401)
        {
            // токен протух / невалиден
            TokenStorage.Clear();
            SceneManager.LoadScene("Auth");
        }
        else
        {
            Error.text = "Ошибка сервера (" + request.responseCode + ")";
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