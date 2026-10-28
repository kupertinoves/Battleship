using UnityEngine;
using System;

public enum CellState
{
    Empty,
    Ship
}

public class GridController : MonoBehaviour
{
    public const int MaximumShipCells = 20;
    public GameObject cellPrefab;
    public Transform gridRoot;

    public CellState[,] grid = new CellState[10, 10];
    public Action<int> OnPlacementUpdated;
    public Action<string> OnPlacementRejected;
    public int ShipCellCount { get; private set; }

    void Start()
    {
        GenerateGrid();
    }

    void GenerateGrid()
    {
        ShipCellCount = 0;
        for (int y = 0; y < 10; y++)
        {
            for (int x = 0; x < 10; x++)
            {
                GameObject cellObj = Instantiate(cellPrefab, gridRoot);
                var cellView = cellObj.GetComponent<CellView>();

                cellView.Init(x, y);
                cellView.OnCellToggled += OnCellToggled;

                // привязываем кнопку
                cellObj.GetComponent<UnityEngine.UI.Button>()
                    .onClick.AddListener(cellView.Toggle);

                grid[x, y] = CellState.Empty;
            }
        }
    }

    void OnCellToggled(int x, int y, bool hasShip)
    {
        if (hasShip)
        {
            if (ShipCellCount >= MaximumShipCells || TouchesShipDiagonally(x, y))
            {
                var cell = gridRoot.GetChild(y * 10 + x).GetComponent<CellView>();
                cell.SetShipWithoutNotify(false);
                OnPlacementRejected?.Invoke(ShipCellCount >= MaximumShipCells
                    ? "Во флоте может быть не больше 20 клеток."
                    : "Корабли не должны соприкасаться по диагонали.");
                return;
            }

            ShipCellCount++;
        }
        else
        {
            ShipCellCount = Mathf.Max(0, ShipCellCount - 1);
        }

        grid[x, y] = hasShip ? CellState.Ship : CellState.Empty;
        OnPlacementUpdated?.Invoke(ShipCellCount);
    }

    bool TouchesShipDiagonally(int x, int y)
    {
        for (int dx = -1; dx <= 1; dx++)
        for (int dy = -1; dy <= 1; dy++)
        {
            if (Mathf.Abs(dx) != 1 || Mathf.Abs(dy) != 1) continue;
            int nx = x + dx;
            int ny = y + dy;
            if (nx >= 0 && nx < 10 && ny >= 0 && ny < 10 && grid[nx, ny] == CellState.Ship)
                return true;
        }

        return false;
    }
}
