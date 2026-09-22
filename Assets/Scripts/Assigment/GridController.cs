using UnityEngine;

public enum CellState
{
    Empty,
    Ship
}

public class GridController : MonoBehaviour
{
    public GameObject cellPrefab;
    public Transform gridRoot;

    public CellState[,] grid = new CellState[10, 10];

    void Start()
    {
        GenerateGrid();
    }

    void GenerateGrid()
    {
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
        grid[x, y] = hasShip ? CellState.Ship : CellState.Empty;
        Debug.Log($"Cell {x},{y} = {grid[x,y]}");
    }
}
