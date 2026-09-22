using UnityEngine;

public class GameGridView : MonoBehaviour
{
    public int size = 10;                 // Размер поля
    public GameCellView gameCellPrefab;   // Префаб клетки
    public GameCellView[,] cells;         // Массив клеток

    // Инициализация поля
    public void Init()
    {
        cells = new GameCellView[size, size];

        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                var cell = Instantiate(gameCellPrefab, transform);
                cell.Coord = new Vector2Int(x, y);
                cell.SetState(GameCellState.Empty);
                cells[x, y] = cell;
            }
        }
    }

    // Установить состояние клетки по координатам
    public void SetCell(Vector2Int coord, GameCellState state)
    {
        if (coord.x < 0 || coord.x >= size || coord.y < 0 || coord.y >= size) return;
        cells[coord.x, coord.y].SetState(state);
    }

    // Получить клетку по координатам
    public GameCellView GetCell(Vector2Int coord)
    {
        if (coord.x < 0 || coord.x >= size || coord.y < 0 || coord.y >= size) return null;
        return cells[coord.x, coord.y];
    }
}
