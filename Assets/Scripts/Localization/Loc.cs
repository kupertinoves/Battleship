using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

/// <summary>
/// Простая система локализации (RU / EN). Язык хранится в PlayerPrefs["Language"]:
/// 0 = русский, 1 = английский (как в практической работе 2).
/// Исходный язык интерфейса — русский, поэтому русская строка служит ключом.
/// </summary>
public static class Loc
{
    public const string PrefKey = "Language";
    public const int Russian = 0, English = 1;

    public static event Action LanguageChanged;

    static int? cached;
    public static int Language
    {
        get
        {
            if (cached == null) cached = PlayerPrefs.GetInt(PrefKey, Russian);
            return cached.Value;
        }
        set
        {
            if (cached == value) return;
            cached = value;
            PlayerPrefs.SetInt(PrefKey, value);
            PlayerPrefs.Save();
            LanguageChanged?.Invoke();
        }
    }

    public static bool IsEnglish => Language == English;

    static string Norm(string s) => Regex.Replace(s.Trim(), @"\s+", " ");

    static readonly Dictionary<string, string> table = Build();
    static readonly List<(Regex rx, string repl)> patterns = new List<(Regex, string)>
    {
        (new Regex(@"^ШАГ (\d+) / (\d+)$"), "STEP $1 / $2"),
        (new Regex(@"^Размещено: (\d+)/20 клеток\. Осталось: (\d+)\.$"), "Placed: $1/20 cells. Remaining: $2."),
        (new Regex(@"^Размещено: (\d+)/20 клеток$"), "Placed: $1/20 cells"),
        (new Regex(@"^Ошибка сервера \((\d+)\)$"), "Server error ($1)"),
        (new Regex(@"^Недопустимый размер корабля: (\d+)$"), "Invalid ship size: $1"),
    };

    /// <summary>Перевести русскую строку на текущий язык (для динамических сообщений в коде).</summary>
    public static string T(string ru) => IsEnglish ? ToEnglish(ru) : ru;

    /// <summary>Вернуть английский вариант или исходную строку, если перевода нет.</summary>
    public static string ToEnglish(string ru)
    {
        if (string.IsNullOrEmpty(ru)) return ru;
        string key = Norm(ru);
        if (table.TryGetValue(key, out var en)) return en;
        foreach (var (rx, repl) in patterns)
            if (rx.IsMatch(key)) return rx.Replace(key, repl);
        return ru;
    }

    static Dictionary<string, string> Build()
    {
        var d = new Dictionary<string, string>();
        void A(string ru, string en) => d[Norm(ru)] = en;

        A("Соединение потеряно. Повторное подключение...", "Connection lost. Reconnecting..."); A("Ожидание соединения…", "Waiting for connection…"); A("Сессия истекла. Войдите заново", "Session expired. Please sign in again");
        A("Нет соединения с сервером", "No connection to the server"); A("Неверный логин или пароль", "Invalid login or password");
        // --- Общие / навигация ---
        A("В ГЛАВНОЕ МЕНЮ", "TO MAIN MENU"); A("В МЕНЮ", "TO MENU"); A("НА ГЛАВНУЮ", "HOME"); A("НАЗАД", "BACK");
        A("ДАЛЕЕ", "NEXT"); A("ВЫХОД", "EXIT");
        // --- Главное меню ---
        A("МОРСКОЙ БОЙ", "BATTLESHIP"); A("МОРСКОЕ СРАЖЕНИЕ", "NAVAL BATTLE");
        A("ОНЛАЙН-ИГРА", "ONLINE GAME"); A("ИГРА С БОТОМ", "PLAY VS BOT"); A("ОБУЧЕНИЕ", "TUTORIAL");
        A("СТАТИСТИКА", "STATISTICS"); A("НАСТРОЙКИ", "SETTINGS"); A("АККАУНТ", "ACCOUNT");
        A("Соберите флот. Выберите тактику. Вступайте в бой.", "Build your fleet. Pick your tactics. Join the battle.");
        // --- Авторизация / регистрация / аккаунт ---
        A("АВТОРИЗАЦИЯ", "SIGN IN"); A("РЕГИСТРАЦИЯ", "REGISTRATION"); A("ВОЙТИ", "LOG IN");
        A("Добро пожаловать на борт", "Welcome aboard"); A("Ещё нет аккаунта?", "No account yet?");
        A("ЛОГИН", "LOGIN"); A("ПАРОЛЬ", "PASSWORD"); A("Введите пароль", "Enter password"); A("Имя командира", "Commander name");
        A("СОЗДАТЬ АККАУНТ", "CREATE ACCOUNT"); A("Создайте аккаунт командира", "Create a commander account");
        A("Никнейм", "Nickname"); A("НИКНЕЙМ", "NICKNAME"); A("Почта", "Email"); A("ПОЧТА", "EMAIL"); A("Пароль", "Password");
        A("ЭЛЕКТРОННАЯ ПОЧТА", "EMAIL"); A("ПРОФИЛЬ ИГРОКА", "PLAYER PROFILE");
        A("СМЕНИТЬ ПАРОЛЬ", "CHANGE PASSWORD"); A("СМЕНА ПАРОЛЯ", "PASSWORD CHANGE");
        A("Старый пароль", "Old password"); A("Новый пароль", "New password");
        A("Введите старый пароль", "Enter old password"); A("Введите новый пароль", "Enter new password"); A("Поменять", "Change");
        // --- Настройки ---
        A("ГРОМКОСТЬ", "VOLUME"); A("ЗВУК", "SOUND"); A("ПОЛНЫЙ ЭКРАН", "FULLSCREEN"); A("ПО УМОЛЧАНИЮ", "DEFAULT");
        A("Игровой чат", "In-game chat"); A("Поменять фон", "Change background");
        // --- Расстановка ---
        A("РАССТАНОВКА ФЛОТА", "FLEET PLACEMENT"); A("СОСТАВ ФЛОТА", "FLEET COMPOSITION");
        A("ЛИНКОР", "BATTLESHIP"); A("КРЕЙСЕРЫ", "CRUISERS"); A("ЭСМИНЦЫ", "DESTROYERS"); A("КАТЕРА", "BOATS");
        A("1 корабль · 4 клетки", "1 ship · 4 cells"); A("2 корабля · по 3 клетки", "2 ships · 3 cells each");
        A("3 корабля · по 2 клетки", "3 ships · 2 cells each"); A("4 корабля · по 1 клетке", "4 ships · 1 cell each");
        A("В БОЙ", "TO BATTLE"); A("ГОТОВО", "READY"); A("ОЧИСТИТЬ", "CLEAR"); A("НОВАЯ ИГРА", "NEW GAME");
        A("ВАШ ФЛОТ", "YOUR FLEET"); A("ФЛОТ ПРОТИВНИКА", "ENEMY FLEET"); A("ПРОТИВНИК", "OPPONENT");
        A("Выберите клетки кораблей. Повторное нажатие освобождает клетку.", "Select ship cells. Pressing again frees the cell.");
        A("Расставьте флот вручную: 1×4, 2×3, 3×2, 4×1. Корабли не должны касаться даже по диагонали.",
          "Place your fleet manually: 1×4, 2×3, 3×2, 4×1. Ships must not touch, even diagonally.");
        A("■ Корабль × Попадание • Промах", "■ Ship × Hit • Miss");
        A("Ожидание соединения…", "Waiting for connection…");
        // --- Итоги ---
        A("Победа", "Victory"); A("Поражение", "Defeat");
        A("Флот противника уничтожен. Отличная работа!", "The enemy fleet is destroyed. Great job!");
        A("Ваш флот потерял все корабли. Попробуйте ещё раз.", "Your fleet has lost all its ships. Try again.");
        // --- Статистика ---
        A("МОЯ СТАТИСТИКА", "MY STATISTICS"); A("ПОБЕДЫ", "WINS"); A("ПОРАЖЕНИЯ", "LOSSES"); A("ДОЛЯ ПОБЕД", "WIN RATE");
        A("ОНЛАЙН-РЕЙТИНГ", "ONLINE RATING"); A("Мой флот", "My fleet");
        A("ЛОКАЛЬНЫЕ РЕЗУЛЬТАТЫ НА ЭТОМ УСТРОЙСТВЕ", "LOCAL RESULTS ON THIS DEVICE");
        A("Пока нет завершённых матчей. Самое время выйти в море.", "No completed matches yet. Time to set sail.");
        A("Онлайн-рейтинг сейчас недоступен.", "Online rating is currently unavailable.");
        A("Загрузка рейтинга игроков…", "Loading player rating…");
        A("В рейтинге пока нет игроков.", "No players in the rating yet.");
        // --- Онлайн-игра: статусы ---
        A("Твой ход", "Your turn"); A("Ход противника", "Opponent turn"); A("Ждем противника...", "Waiting for opponent...");
        A("Ты победил!", "You won!"); A("Ты проиграл", "You lost"); A("Технические проблемы...", "Technical problems...");
        // --- Игра с ботом: статусы ---
        A("Расставьте флот на левом поле, затем нажмите «Готово».", "Place your fleet on the left grid, then press Ready.");
        A("Флот очищен. Расставьте корабли и нажмите «Готово».", "Fleet cleared. Place your ships and press Ready.");
        A("Ваш ход. Выберите клетку на поле противника.", "Your turn. Pick a cell on the enemy grid.");
        A("Промах. Ход компьютера.", "Miss. Computer turn.");
        A("Компьютер промахнулся. Ваш ход.", "The computer missed. Your turn.");
        A("Новый матч. Ваш ход.", "New match. Your turn.");
        A("Победа! Вы потопили весь флот противника.", "Victory! You sank the entire enemy fleet.");
        // --- Расстановка: сообщения ---
        A("Флот готов к бою.", "Fleet is ready for battle.");
        A("Во флоте может быть не больше 20 клеток.", "The fleet cannot have more than 20 cells.");
        A("Корабли не должны соприкасаться по диагонали.", "Ships must not touch diagonally.");
        A("Корабли должны быть только по прямой", "Ships must be straight lines");
        A("Не удалось загрузить поле расстановки.", "Could not load the placement grid.");
        // --- Обучение ---
        A("ИГРАТЬ С БОТОМ", "PLAY VS BOT");
        A("ВАШЕ ПОЛЕ", "YOUR GRID"); A("ПРАВИЛА РАССТАНОВКИ", "PLACEMENT RULES"); A("ВЫСТРЕЛ", "SHOT");
        A("ПОПАДАНИЕ И ПРОМАХ", "HIT AND MISS"); A("УНИЧТОЖЕННЫЙ КОРАБЛЬ", "SUNK SHIP"); A("ПОБЕДА", "VICTORY");
        A("ВЫ ГОТОВЫ К ИГРЕ", "YOU ARE READY TO PLAY");
        return d;
    }
}
