using System.Collections.Generic;
using UnityEngine;

public static class PlacementValidator
{
    // Классические правила морского боя
    private static readonly Dictionary<int, int> Rules = new()
    {
        { 4, 1 }, // один 4-палубный
        { 3, 2 }, // два 3-палубных
        { 2, 3 }, // три 2-палубных
        { 1, 4 }  // четыре 1-палубных
    };

    public static bool TryValidate(
        CellState[,] grid,
        out List<Vector2Int> allCells,
        out string error
    )
    {
        allCells = new List<Vector2Int>();
        error = null;

        int size = grid.GetLength(0);
        bool[,] visited = new bool[size, size];

        Dictionary<int, int> found = new();

        for (int x = 0; x < size; x++)
        for (int y = 0; y < size; y++)
        {
            if (grid[x, y] == CellState.Ship && !visited[x, y])
            {
                List<Vector2Int> ship = FloodFill(grid, visited, x, y);

                // Проверка: прямая линия
                if (!IsStraight(ship))
                {
                    error = "Корабли должны быть только по прямой";
                    return false;
                }

                int len = ship.Count;

                if (!Rules.ContainsKey(len))
                {
                    error = $"Недопустимый размер корабля: {len}";
                    return false;
                }

                if (!found.ContainsKey(len))
                    found[len] = 0;

                found[len]++;

                allCells.AddRange(ship);
            }
        }

        if (allCells.Count != 20)
        {
            error = $"Нужно разместить ровно 20 клеток кораблей. Сейчас: {allCells.Count}";
            return false;
        }

        // Корабли не должны соприкасаться по диагонали.
        for (int x = 0; x < size; x++)
        for (int y = 0; y < size; y++)
        {
            if (grid[x, y] != CellState.Ship) continue;
            if (x + 1 < size && y + 1 < size && grid[x + 1, y + 1] == CellState.Ship ||
                x + 1 < size && y - 1 >= 0 && grid[x + 1, y - 1] == CellState.Ship)
            {
                error = "Корабли не должны соприкасаться по диагонали";
                return false;
            }
        }

        // Проверяем количество кораблей каждого размера
        foreach (var rule in Rules)
        {
            int sizeRule = rule.Key;
            int required = rule.Value;
            int actual = found.ContainsKey(sizeRule) ? found[sizeRule] : 0;

            if (actual != required)
            {
                error = $"Кораблей размера {sizeRule} должно быть {required}, а не {actual}";
                return false;
            }
        }

        return true;
    }

    private static List<Vector2Int> FloodFill(
        CellState[,] grid,
        bool[,] visited,
        int sx,
        int sy
    )
    {
        int size = grid.GetLength(0);
        var result = new List<Vector2Int>();
        var stack = new Stack<Vector2Int>();

        stack.Push(new Vector2Int(sx, sy));
        visited[sx, sy] = true;

        var dirs = new Vector2Int[]
        {
            new(1,0), new(-1,0), new(0,1), new(0,-1)
        };

        while (stack.Count > 0)
        {
            var p = stack.Pop();
            result.Add(p);

            foreach (var d in dirs)
            {
                int nx = p.x + d.x;
                int ny = p.y + d.y;

                if (nx >= 0 && nx < size && ny >= 0 && ny < size)
                {
                    if (!visited[nx, ny] && grid[nx, ny] == CellState.Ship)
                    {
                        visited[nx, ny] = true;
                        stack.Push(new Vector2Int(nx, ny));
                    }
                }
            }
        }

        return result;
    }

    private static bool IsStraight(List<Vector2Int> ship)
    {
        if (ship.Count <= 1)
            return true;

        bool sameX = true;
        bool sameY = true;

        int x0 = ship[0].x;
        int y0 = ship[0].y;

        foreach (var p in ship)
        {
            if (p.x != x0) sameX = false;
            if (p.y != y0) sameY = false;
        }

        return sameX || sameY;
    }
}
