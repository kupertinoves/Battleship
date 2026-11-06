using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;

public class GameUIController : MonoBehaviour
{
    public GameGridView myField;
    public GameGridView enemyField;
    public GameWsClient ws;
    private bool canShoot = false;
    private Vector2Int lastShotCoord;
    private int k=0;
    void Start()
    {
        myField.Init();
        enemyField.Init();

        DrawMyShips();
        SetupEnemyClicks();
        ws.OPPONENT_LAST_HIT += OPPONENT_LAST_HIT_VALIDATE;
        ws.OnHitResult+=Hit_validate;


    }

    void DrawMyShips()
    {
        foreach (Vector2Int coord in PlacementData.ShipCells)
        {
            myField.SetCell(coord, GameCellState.Ship);
        }
    }

    void SetupEnemyClicks()
    {
        int size = enemyField.cells.GetLength(0);
        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                GameCellView cell = enemyField.cells[x, y];
                cell.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(() =>
                {
                    OnEnemyCellClicked(cell);
                });
            }
        }
    }
    

    void OnEnemyCellClicked(GameCellView cell)
    {
        //Debug.Log($"Clicked on enemy cell: {cell.Coord}");
        //cell.SetState(GameCellState.Miss);}
        if (!canShoot)
        {
            Debug.Log("Not your turn!");
            return;
        }

        // Запоминаем, куда стреляли
        lastShotCoord = cell.Coord;

        // Запрещаем стрелять повторно до следующего YOUR_TURN
        canShoot = false;

        // Отправляем координаты на сервер
        ws.SendShot(cell.Coord.x, cell.Coord.y);

        Debug.Log($"Shot sent to: {cell.Coord.x},{cell.Coord.y}");
    }

    private void OPPONENT_LAST_HIT_VALIDATE(TurnMessage msg)
    {
        k+=1;
        canShoot=true;
        Debug.Log(canShoot);
        if (string.IsNullOrEmpty(msg.OPPONENT_LAST_HIT) || msg.OPPONENT_LAST_HIT=="[]") return;
        string coordStr=msg.OPPONENT_LAST_HIT;
        coordStr = coordStr.Trim('[', ']');
        var parts = coordStr.Split(',');

        if (parts.Length != 2) return;

        if (!int.TryParse(parts[0].Trim(), out int cx) || !int.TryParse(parts[1].Trim(), out int cy)) return;
        Vector2Int coord = new Vector2Int(cx, cy);

        if (PlacementData.ShipCells.Contains(coord))
            myField.SetCell(coord, GameCellState.Hit);
        else
            myField.SetCell(coord, GameCellState.Miss);
    }

    private void Hit_validate(HitResultMessage msg){
        if (k==0){
            k+=1;
            return;
        }
        if (msg.YOUR_LAST_HIT=="0"){
            enemyField.SetCell(lastShotCoord,GameCellState.Miss);
        }else{
            enemyField.SetCell(lastShotCoord,GameCellState.Hit);
        }

    }

    
}


