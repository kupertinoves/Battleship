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
    public string wsUrl = "ws://localhost:8000/ws/game"; // URL WebSocket
    private WebSocket ws;
    public TextMeshProUGUI statusText;


    // События для GameController
    //public System.Action OnSendRequested;            // сервер ждёт корабли
    //                 // игра началась
    public System.Action<TurnMessage> OPPONENT_LAST_HIT; // ход игрока
    public System.Action<HitResultMessage> OnHitResult; // результат выстрела

    async void Start()
    {
        // если токен не задан, загрузим из файла
        string token = TokenStorage.Load();

        ws = new WebSocket(wsUrl + "?token=" + token);

        ws.OnOpen += () => Debug.Log("WS connected");
        ws.OnError += (e) => Debug.LogError("WS Error: " + e);
        ws.OnClose += (e) => Debug.Log("WS Closed: " + e);

        ws.OnMessage += (bytes) =>
        {
            string msg = Encoding.UTF8.GetString(bytes);
            Debug.Log("WS IN: " + msg);
            HandleMessage(msg);
        };

        await ws.Connect();
    }

    void Update()
    {
    #if !UNITY_WEBGL || UNITY_EDITOR
        ws?.DispatchMessageQueue();
    #endif
    }

    private void HandleMessage(string msg)
    {
        //var data = TextToDict.ConvertToDictionary(msg);
        //Debug.Log(data);
        var turn = JsonConvert.DeserializeObject<TurnMessage>(msg);

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
            if (turn.TURN=="WIN"){
                statusText.text="Ты победил!";
                ws.Close();
                System.Threading.Tasks.Task.Delay(5000);
                SceneManager.LoadScene("Win");
                return;
            }else{
                statusText.text="Ты проиграл";
                ws.Close();
                System.Threading.Tasks.Task.Delay(5000);
                SceneManager.LoadScene("Defeat");
                return;
            }
            //ws.Close();
           //System.Threading.Tasks.Task.Delay(5000);
            //SceneManager.LoadScene("MainMenu");
            //return;
        }
        if (turn.TURN=="DISCONNECT"){
            ws.Close();
            statusText.text="Технические проблемы...";
            System.Threading.Tasks.Task.Delay(5000);
            SceneManager.LoadScene("MainMenu");
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
