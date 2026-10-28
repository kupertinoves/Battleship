using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class GameUIController : MonoBehaviour
{
    public GameGridView myField;
    public GameGridView enemyField;
    [FormerlySerializedAs("ws")] public GameNgoClient network;
    int lastShotSequence;

    void Start()
    {
        myField.Init();
        enemyField.Init();
        ResetBoards();
        for (int x = 0; x < 10; x++)
        for (int y = 0; y < 10; y++)
        {
            GameCellView cell = enemyField.cells[x, y];
            cell.GetComponent<Button>().onClick.AddListener(() =>
            {
                if (!BattlePauseController.IsPaused && network != null && network.CanShoot)
                    network.Shoot(cell.Coord.x, cell.Coord.y);
            });
        }
        network.StateChanged += OnState;
        network.ConnectionChanged += OnConnection;
    }

    void ResetBoards()
    {
        lastShotSequence = 0;
        for (int x = 0; x < 10; x++)
        for (int y = 0; y < 10; y++)
        {
            myField.SetCell(new Vector2Int(x, y), GameCellState.Empty);
            enemyField.SetCell(new Vector2Int(x, y), GameCellState.Empty);
        }
        foreach (Vector2Int cell in PlacementData.ShipCells) myField.SetCell(cell, GameCellState.Ship);
    }

    void OnConnection(bool connected)
    {
        ResetBoards();
        BattlePauseController pause = FindObjectOfType<BattlePauseController>();
        if (pause != null) pause.Resume();
    }

    void OnState(FleetSnapshot state)
    {
        if (state.ShotSequence <= lastShotSequence) return;
        lastShotSequence = state.ShotSequence;
        GameGridView target = state.Shooter == network.LocalId ? enemyField : myField;
        target.SetCell(new Vector2Int(state.X, state.Y), state.Hit ? GameCellState.Hit : GameCellState.Miss);
    }

    void OnDestroy()
    {
        if (network == null) return;
        network.StateChanged -= OnState;
        network.ConnectionChanged -= OnConnection;
    }
}
