using System.IO;
using UnityEngine;

[System.Serializable]
public class AuthData
{
    public string access_token;
}

public static class TokenStorage
{
    private static string FilePath =>
        Path.Combine(Application.persistentDataPath, "auth.json");

    /// <summary>
    /// Сохранить токен в файл
    /// </summary>
    public static void Save(string token)
    {

        Debug.Log(Application.persistentDataPath);

        File.WriteAllText(FilePath, token);

        Debug.Log("Token saved to: " + FilePath);
    }

    /// <summary>
    /// Загрузить токен из файла
    /// </summary>
    public static string Load()
    {
        if (!File.Exists(FilePath))
            return null;

        try
        {
            string json = File.ReadAllText(FilePath);
            var data = JsonUtility.FromJson<AuthData>(json);
            return data != null ? data.access_token : null;
        }
        catch (System.Exception)
        {
            // повреждённый файл токена — считаем, что токена нет
            return null;
        }
    }

    /// <summary>
    /// Удалить токен (logout)
    /// </summary>
    public static void Clear()
    {
        if (File.Exists(FilePath))
            File.Delete(FilePath);
    }
}
