using UnityEngine;

/// <summary>
/// ЕДИНОЕ место настройки адресов бэкенда.
/// Чтобы сменить сервер, достаточно изменить AuthHost / GameHost (или порты) ниже.
/// Все сцены и скрипты берут адреса отсюда; в Inspector ничего прописывать не нужно.
/// </summary>
public static class ServerConfig
{
    // ====== ЕДИНСТВЕННОЕ МЕСТО, ГДЕ ПРОПИСЫВАЮТСЯ АДРЕСА ======
    public const string AuthHost = "193.188.20.123";   // сервис аутентификации
    public const int    AuthPort = 8000;
    public const string GameHost = "193.188.20.123";   // игровой менеджер
    public const int    GamePort = 8008;
    public const string StatHost = "193.188.20.123";   // сервис статистики
    public const int    StatPort = 8009;
    public const bool   UseTls   = false;              // true -> https/wss
    // ===========================================================

    static string Http => UseTls ? "https" : "http";
    static string Ws   => UseTls ? "wss"   : "ws";

    public static string AuthBaseUrl => $"{Http}://{AuthHost}:{AuthPort}";
    public static string GameBaseUrl => $"{Http}://{GameHost}:{GamePort}";

    // Аутентификация
    public static string RegisterUrl       => AuthBaseUrl + "/auth/register";
    public static string LoginUrl          => AuthBaseUrl + "/auth/login";
    public static string CheckTokenUrl     => AuthBaseUrl + "/auth/check";
    public static string UserInfoUrl       => AuthBaseUrl + "/auth/user";
    public static string ChangePasswordUrl => AuthBaseUrl + "/auth/change/pass";

    // Игровой менеджер
    public static string GameWsUrl         => $"{Ws}://{GameHost}:{GamePort}/ws";

    // Статистика (GET - список игроков, POST add - прибавить результат матча текущему игроку)
    public static string StatBaseUrl       => $"{Http}://{StatHost}:{StatPort}";
    public static string RatingUrl         => StatBaseUrl + "/statistic";
    public static string StatisticAddUrl   => StatBaseUrl + "/statistic/add";
}