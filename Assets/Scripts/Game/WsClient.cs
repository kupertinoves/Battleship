using UnityEngine;
using NativeWebSocket;
using System.Text;
using Newtonsoft.Json;
using System.Collections.Generic;
using TMPro;
using UnityEngine.SceneManagement;
public class GameWsClient : MonoBehaviour
{
    [Header("WebSocket")]
    private WebSocket ws;
    public TextMeshProUGUI statusText;


    // События для GameController
    //public System.Action OnSendRequested;            // сервер ждёт корабли
    //                 // игра началась
    public System.Action<TurnMessage> OPPONENT_LAST_HIT; // ход игрока
    public System.Action<HitResultMessage> OnHitResult; // результат выстрела

    bool finished, destroyed;
    const int MaxAttempts = 5;

    async void Start()
    {
        for (int attempt = 1; attempt <= MaxAttempts && !finished && !destroyed; attempt++)
        {
            // токен перечитываем на каждой попытке
            string token = TokenStorage.Load();
            ws = new WebSocket(ServerConfig.GameWsUrl + "?token=" + token);

            ws.OnOpen += () => Debug.Log("WS connected");
            ws.OnError += (e) => Debug.LogWarning("WS Error: " + e);
            ws.OnClose += (e) => Debug.Log("WS Closed: " + e);
            ws.OnMessage += (bytes) =>
            {
                string msg = Encoding.UTF8.GetString(bytes);
                Debug.Log("WS IN: " + msg);
                HandleMessage(msg);
            };

            try { await ws.Connect(); }   // возвращается, когда соединение закрыто
            catch (System.Exception e) { Debug.LogWarning("WS connect failed: " + e.Message); }

            if (finished || destroyed) return;
            if (attempt == MaxAttempts) break;
            if (statusText != null) statusText.text = "Соединение потеряно. Повторное подключение...";
            await System.Threading.Tasks.Task.Delay(3000);
        }
        if (!finished && !destroyed && statusText != null)
            statusText.text = "Нет соединения с сервером";
    }

    void Update()
    {
    #if !UNITY_WEBGL || UNITY_EDITOR
        ws?.DispatchMessageQueue();
    #endif
    }

    System.Collections.IEnumerator LoadAfterDelay(string scene)
    {
        yield return new WaitForSeconds(3f);
        SceneManager.LoadScene(scene);
    }

    async void OnDestroy()
    {
        destroyed = true;
        if (ws != null && ws.State == WebSocketState.Open) await ws.Close();
    }

    private void HandleMessage(string msg)
    {
        //var data = TextToDict.ConvertToDictionary(msg);
        //Debug.Log(data);
        // сервер иногда присылает текст-обёртку ("Game over: {...}") или JSON с одинарными кавычками
        int brace = msg.IndexOf('{');
        if (brace < 0) { Debug.LogWarning("WS: сообщение без JSON: " + msg); return; }
        msg = msg.Substring(brace);

        TurnMessage turn;
        try { turn = JsonConvert.DeserializeObject<TurnMessage>(msg); }
        catch (System.Exception e) { Debug.LogWarning("WS: не удалось разобрать сообщение: " + e.Message); return; }
        if (turn == null || string.IsNullOrEmpty(turn.TURN)) return;

        if (turn.TURN == "SEND")
        {
            SendMyShips();
            return;
        }

        if (turn.TURN == "YOUR_TURN")
        {
            statusText.text = "Твой ход";
            OPPONENT_LAST_HIT?.Invoke(turn);
            return;
        }
        if (turn.TURN == "OPPONENT_TURN")
        {
            statusText.text = "Ход противника";
            var turne = JsonConvert.DeserializeObject<HitResultMessage>(msg);
            OnHitResult?.Invoke(turne);
            return;
        }
        if (turn.TURN=="WAIT"){
            statusText.text = "Ждем противника...";
            return;
        }
        if (turn.TURN=="WIN" ||  turn.TURN=="LOST"){
            finished = true;
            if (turn.TURN=="WIN"){
                statusText.text="Ты победил!";
                ws.Close();
                StartCoroutine(LoadAfterDelay("Win"));
                return;
            }else{
                statusText.text="Ты проиграл";
                ws.Close();
                StartCoroutine(LoadAfterDelay("Defeat"));
                return;
            }
            //ws.Close();
           //System.Threading.Tasks.Task.Delay(5000);
            //SceneManager.LoadScene("MainMenu");
            //return;
        }
        if (turn.TURN=="DISCONNECT"){
            finished = true;
            ws.Close();
            statusText.text="Технические проблемы...";
            StartCoroutine(LoadAfterDelay("MainMenu"));
            return;
        }

    }

    // Отправляем свои координаты кораблей
    void SendMyShips()
    {
        // Пример формата: "[[0,0],[0,1],[0,2]]"
        string shipArray = "[";
        for (int i = 0; i < PlacementData.ShipCells.Count; i++)
        {
            var c = PlacementData.ShipCells[i];
            shipArray += $"[{c.x},{c.y}]";
            if (i != PlacementData.ShipCells.Count - 1) shipArray += ",";
        }
        shipArray += "]";

        ws.SendText(shipArray);
    }

    // Отправляем выстрел
    public void SendShot(int x, int y)
    {
        var req = new ShotRequest { x = x, y = y };
        string json = JsonConvert.SerializeObject(req);
        ws.SendText(json);
        Debug.Log("Shot sent: " + json);
    }
    
}

// DTO
[System.Serializable]
public class TurnMessage
{
    public string TURN; // "YOUR_TURN" | "OPPONENT_TURN"
    public string OPPONENT_LAST_HIT; // [x,y] или null
}

[System.Serializable]
public class HitResultMessage
{
    public string TURN; // "YOUR_TURN" | "OPPONENT_TURN"
    public string YOUR_LAST_HIT; // [x,y] или null // 1 или 0
}

[System.Serializable]
public class ShotRequest
{
    public int x;
    public int y;
}
