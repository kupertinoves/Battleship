using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Connects editor-authored board UI to the offline game rules.</summary>
public sealed class BotGameUIController : MonoBehaviour
{
    [Header("Editor-authored board roots")]
    [SerializeField] GameGridView playerField;
    [SerializeField] GameGridView botField;

    [Header("Editor-authored controls")]
    [SerializeField] TMP_Text statusText;
    [SerializeField] Button readyButton;
    [SerializeField] Button rematchButton;
    [SerializeField] Button clearButton;
    [SerializeField] Button backButton;
    [SerializeField] string mainMenuScene = "MainMenu";
    [SerializeField, Min(0f)] float botShotDelay = 0.45f;

    LocalGameManager match;
    Vector2Int[][] playerPlacement;
    bool placementLocked;
    bool resultRecorded;
    Coroutine botTurn;

    void Start()
    {
        if (playerField == null || botField == null || playerField.gameCellPrefab == null || botField.gameCellPrefab == null)
        {
            SetStatus("В сцене не настроены поля или префаб клетки.");
            return;
        }

        playerField.Init();
        botField.Init();
        for (int x = 0; x < LocalBoard.Size; x++)
        for (int y = 0; y < LocalBoard.Size; y++)
        {
            GameCellView playerCell = playerField.cells[x, y];
            Button playerButton = playerCell.GetComponent<Button>();
            if (playerButton != null) playerButton.onClick.AddListener(() => TogglePlayerCell(playerCell));

            GameCellView botCell = botField.cells[x, y];
            Button botButton = botCell.GetComponent<Button>();
            if (botButton != null) botButton.onClick.AddListener(() => FireAtBot(botCell));
        }

        if (readyButton != null) readyButton.onClick.AddListener(TryStartMatch);
        if (rematchButton != null) { rematchButton.onClick.AddListener(Rematch); rematchButton.interactable = false; }
        if (clearButton != null) clearButton.onClick.AddListener(ClearPlayerFleet);
        if (backButton != null) backButton.onClick.AddListener(ReturnToMenu);
        SetStatus("Расставьте флот на левом поле, затем нажмите «Готово».");
        RefreshControls();
    }

    void TogglePlayerCell(GameCellView cell)
    {
        if (placementLocked || match != null || cell == null) return;
        cell.SetState(cell.GetState() == GameCellState.Ship ? GameCellState.Empty : GameCellState.Ship);
    }

    void ClearPlayerFleet()
    {
        if (placementLocked || match != null || playerField == null) return;
        ResetGrid(playerField);
        SetStatus("Флот очищен. Расставьте корабли и нажмите «Готово».");
    }

    void TryStartMatch()
    {
        if (match != null || playerField == null || botField == null) return;
        var mask = new bool[LocalBoard.Size, LocalBoard.Size];
        for (int x = 0; x < LocalBoard.Size; x++)
        for (int y = 0; y < LocalBoard.Size; y++)
            mask[x, y] = playerField.cells[x, y].GetState() == GameCellState.Ship;

        if (!LocalBoard.TryBuildFleetFromMask(mask, out playerPlacement, out string error))
        {
            SetStatus(error);
            return;
        }

        var player = new LocalBoard();
        var bot = new LocalBoard();
        if (!player.TryPlaceFleet(playerPlacement, out error) || !bot.TryPlaceFleet(LocalBoard.GenerateRandomFleet(), out error))
        {
            SetStatus(error ?? "Не удалось подготовить флот.");
            return;
        }

        match = new LocalGameManager(player, bot, new BattleshipBot());
        resultRecorded = false;
        placementLocked = true;
        RefreshControls();
        SetStatus("Ваш ход. Выберите клетку на поле противника.");
    }

    void FireAtBot(GameCellView cell)
    {
        if (match == null || match.Finished || !match.PlayerTurn || cell == null) return;
        LocalShotResult result = match.PlayerShoot(cell.Coord);
        if (result == LocalShotResult.AlreadyShot) { SetStatus("Вы уже стреляли в эту клетку."); return; }
        if (result == LocalShotResult.Invalid) return;
        botField.SetCell(cell.Coord, result == LocalShotResult.Miss ? GameCellState.Miss : GameCellState.Hit);
        if (match.Finished) { FinishMatch(); return; }
        if (!match.PlayerTurn)
        {
            SetStatus("Промах. Ход компьютера.");
            botTurn = StartCoroutine(PlayBotTurn());
        }
        else SetStatus(result == LocalShotResult.Sunk ? "Корабль уничтожен. Ваш ход продолжается." : "Попадание. Ваш ход продолжается.");
        RefreshControls();
    }

    IEnumerator PlayBotTurn()
    {
        while (match != null && !match.Finished && !match.PlayerTurn)
        {
            if (botShotDelay > 0f) yield return new WaitForSecondsRealtime(botShotDelay);
            Vector2Int shot = match.Bot.ChooseShot();
            LocalShotResult result = match.BotShoot(shot);
            playerField.SetCell(shot, result == LocalShotResult.Miss ? GameCellState.Miss : GameCellState.Hit);
            if (match.Finished) { FinishMatch(); yield break; }
            if (match.PlayerTurn) SetStatus("Компьютер промахнулся. Ваш ход.");
            else SetStatus(result == LocalShotResult.Sunk ? "Компьютер потопил корабль. Он продолжает ход." : "Компьютер попал и продолжает ход.");
            RefreshControls();
        }
        botTurn = null;
    }

    void Rematch()
    {
        if (playerPlacement == null || playerField == null || botField == null) return;
        ResetGrid(playerField);
        ResetGrid(botField);
        var player = new LocalBoard();
        var bot = new LocalBoard();
        if (!player.TryPlaceFleet(playerPlacement, out string error) || !bot.TryPlaceFleet(LocalBoard.GenerateRandomFleet(), out error))
        { SetStatus(error); return; }
        match = new LocalGameManager(player, bot, new BattleshipBot());
        resultRecorded = false;
        foreach (Vector2Int[] ship in playerPlacement)
            foreach (Vector2Int cell in ship) playerField.SetCell(cell, GameCellState.Ship);
        RefreshControls();
        SetStatus("Новый матч. Ваш ход.");
    }

    void FinishMatch()
    {
        SetStatus(match.PlayerWon ? "Победа! Вы потопили весь флот противника." : "Поражение. Компьютер потопил ваш флот.");
        if (!resultRecorded)
        {
            if (match.PlayerWon) LocalStatistics.RecordWin();
            else LocalStatistics.RecordLoss();
            resultRecorded = true;
        }
        RefreshControls();
    }

    void ReturnToMenu() => SceneManager.LoadScene(mainMenuScene);

    void ResetGrid(GameGridView grid)
    {
        for (int x = 0; x < LocalBoard.Size; x++)
        for (int y = 0; y < LocalBoard.Size; y++) grid.SetCell(new Vector2Int(x, y), GameCellState.Empty);
    }

    void RefreshControls()
    {
        bool placing = match == null && !placementLocked;
        bool canShoot = match != null && !match.Finished && match.PlayerTurn;
        if (readyButton != null) readyButton.interactable = placing;
        if (clearButton != null) clearButton.interactable = placing;
        if (rematchButton != null) rematchButton.interactable = match != null && match.Finished;
        for (int x = 0; x < LocalBoard.Size; x++)
        for (int y = 0; y < LocalBoard.Size; y++)
        {
            Button playerButton = playerField.cells[x, y].GetComponent<Button>();
            if (playerButton != null) playerButton.interactable = placing;
            GameCellView target = botField.cells[x, y];
            Button targetButton = target.GetComponent<Button>();
            if (targetButton != null)
                targetButton.interactable = canShoot && target.GetState() == GameCellState.Empty;
        }
    }

    void SetStatus(string message) { if (statusText != null) statusText.text = message; }

    void OnDestroy()
    {
        if (botTurn != null) StopCoroutine(botTurn);
    }
}
