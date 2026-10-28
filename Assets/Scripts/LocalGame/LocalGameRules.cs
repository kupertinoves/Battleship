using System;
using System.Collections.Generic;
using UnityEngine;

public enum LocalShotResult
{
    Invalid,
    AlreadyShot,
    Miss,
    Hit,
    Sunk
}

public sealed class LocalShip
{
    readonly HashSet<Vector2Int> cells;
    readonly HashSet<Vector2Int> hits = new HashSet<Vector2Int>();

    public IEnumerable<Vector2Int> Cells => cells;
    public int Length => cells.Count;
    public bool IsSunk => hits.Count == cells.Count;

    public LocalShip(IEnumerable<Vector2Int> shipCells)
    {
        cells = new HashSet<Vector2Int>(shipCells);
    }

    public bool Contains(Vector2Int cell) => cells.Contains(cell);
    public void RegisterHit(Vector2Int cell) => hits.Add(cell);
}

public sealed class LocalBoard
{
    public const int Size = 10;
    static readonly int[] Fleet = { 4, 3, 3, 2, 2, 2, 1, 1, 1, 1 };

    readonly bool[,] shots = new bool[Size, Size];
    readonly LocalShip[,] occupancy = new LocalShip[Size, Size];
    readonly List<LocalShip> ships = new List<LocalShip>();

    public IReadOnlyList<LocalShip> Ships => ships;
    public bool AllShipsSunk => ships.Count == Fleet.Length && ships.TrueForAll(ship => ship.IsSunk);

    public bool WasShot(Vector2Int cell) => InBounds(cell) && shots[cell.x, cell.y];
    public LocalShip ShipAt(Vector2Int cell) => InBounds(cell) ? occupancy[cell.x, cell.y] : null;

    public static bool InBounds(Vector2Int cell) => cell.x >= 0 && cell.x < Size && cell.y >= 0 && cell.y < Size;

    public bool TryPlaceFleet(IEnumerable<Vector2Int[]> placement, out string error)
    {
        error = null;
        var candidate = new List<Vector2Int[]>();
        foreach (Vector2Int[] cells in placement) candidate.Add(cells);
        if (!HasCorrectFleet(candidate))
        {
            error = "Флот должен состоять из 1×4, 2×3, 3×2 и 4×1 палуб.";
            return false;
        }

        var occupied = new bool[Size, Size];
        foreach (Vector2Int[] shipCells in candidate)
        {
            if (!IsStraightAndContiguous(shipCells))
            {
                error = "Каждый корабль должен располагаться одной прямой линией.";
                return false;
            }

            foreach (Vector2Int cell in shipCells)
            {
                if (!InBounds(cell) || occupied[cell.x, cell.y])
                {
                    error = "Корабль выходит за поле или пересекает другой корабль.";
                    return false;
                }
                occupied[cell.x, cell.y] = true;
            }
        }

        for (int x = 0; x < Size; x++)
        for (int y = 0; y < Size; y++)
        {
            if (!occupied[x, y]) continue;
            for (int dx = -1; dx <= 1; dx++)
            for (int dy = -1; dy <= 1; dy++)
            {
                int nx = x + dx, ny = y + dy;
                if (nx >= 0 && nx < Size && ny >= 0 && ny < Size && occupied[nx, ny] && (dx != 0 || dy != 0))
                {
                    // Cells belonging to this same ship may touch orthogonally only.
                    if (Math.Abs(dx) + Math.Abs(dy) == 1 && SameShip(candidate, new Vector2Int(x, y), new Vector2Int(nx, ny)))
                        continue;
                    error = "Корабли не должны соприкасаться, даже по диагонали.";
                    return false;
                }
            }
        }

        Clear();
        foreach (Vector2Int[] shipCells in candidate)
        {
            var ship = new LocalShip(shipCells);
            ships.Add(ship);
            foreach (Vector2Int cell in shipCells) occupancy[cell.x, cell.y] = ship;
        }
        return true;
    }

    public LocalShotResult Shoot(Vector2Int cell)
    {
        if (!InBounds(cell)) return LocalShotResult.Invalid;
        if (shots[cell.x, cell.y]) return LocalShotResult.AlreadyShot;
        shots[cell.x, cell.y] = true;
        LocalShip ship = occupancy[cell.x, cell.y];
        if (ship == null) return LocalShotResult.Miss;
        ship.RegisterHit(cell);
        return ship.IsSunk ? LocalShotResult.Sunk : LocalShotResult.Hit;
    }

    public static bool TryBuildFleetFromMask(bool[,] mask, out Vector2Int[][] placement, out string error)
    {
        placement = Array.Empty<Vector2Int[]>();
        error = null;
        if (mask == null || mask.GetLength(0) != Size || mask.GetLength(1) != Size)
        {
            error = "Размер поля расстановки должен быть 10×10.";
            return false;
        }

        var visited = new bool[Size, Size];
        var ships = new List<Vector2Int[]>();
        for (int x = 0; x < Size; x++)
        for (int y = 0; y < Size; y++)
        {
            if (!mask[x, y] || visited[x, y]) continue;
            var cells = FloodShip(mask, visited, new Vector2Int(x, y));
            if (!IsStraightAndContiguous(cells))
            {
                error = "Корабли должны быть прямыми и непрерывными.";
                return false;
            }
            ships.Add(cells.ToArray());
        }

        if (!HasCorrectFleet(ships))
        {
            error = "Нужны корабли: один четырёхпалубный, два трёхпалубных, три двухпалубных и четыре однопалубных.";
            return false;
        }

        if (!ValidateNoTouch(mask))
        {
            error = "Корабли не должны соприкасаться, даже по диагонали.";
            return false;
        }

        placement = ships.ToArray();
        return true;
    }

    public static Vector2Int[][] GenerateRandomFleet(System.Random random = null)
    {
        random = random ?? new System.Random();
        var occupied = new bool[Size, Size];
        var result = new List<Vector2Int[]>();
        if (!PlaceNext(Fleet, 0, occupied, result, random))
            throw new InvalidOperationException("Не удалось сгенерировать расстановку флота.");
        return result.ToArray();
    }

    void Clear()
    {
        ships.Clear();
        Array.Clear(occupancy, 0, occupancy.Length);
        Array.Clear(shots, 0, shots.Length);
    }

    static bool HasCorrectFleet(IReadOnlyCollection<Vector2Int[]> candidate)
    {
        if (candidate.Count != Fleet.Length) return false;
        var counts = new Dictionary<int, int>();
        foreach (Vector2Int[] ship in candidate)
        {
            if (ship == null || ship.Length < 1 || ship.Length > 4) return false;
            counts.TryGetValue(ship.Length, out int count);
            counts[ship.Length] = count + 1;
        }
        foreach (int length in Fleet)
            if (!counts.TryGetValue(length, out int count) || count != RequiredCount(length)) return false;
        return true;
    }

    static int RequiredCount(int length)
    {
        int count = 0;
        foreach (int item in Fleet) if (item == length) count++;
        return count;
    }

    static bool IsStraightAndContiguous(IReadOnlyCollection<Vector2Int> cells)
    {
        if (cells.Count == 0) return false;
        int minX = int.MaxValue, maxX = int.MinValue, minY = int.MaxValue, maxY = int.MinValue;
        foreach (Vector2Int cell in cells)
        {
            if (!InBounds(cell)) return false;
            minX = Math.Min(minX, cell.x); maxX = Math.Max(maxX, cell.x);
            minY = Math.Min(minY, cell.y); maxY = Math.Max(maxY, cell.y);
        }
        return (minX == maxX && maxY - minY + 1 == cells.Count) ||
               (minY == maxY && maxX - minX + 1 == cells.Count);
    }

    static bool SameShip(IReadOnlyList<Vector2Int[]> ships, Vector2Int a, Vector2Int b)
    {
        foreach (Vector2Int[] ship in ships)
        {
            bool hasA = false, hasB = false;
            foreach (Vector2Int cell in ship) { if (cell == a) hasA = true; if (cell == b) hasB = true; }
            if (hasA && hasB) return true;
        }
        return false;
    }

    static bool ValidateNoTouch(bool[,] mask)
    {
        for (int x = 0; x < Size; x++)
        for (int y = 0; y < Size; y++)
        {
            if (!mask[x, y]) continue;
            for (int dx = -1; dx <= 1; dx++)
            for (int dy = -1; dy <= 1; dy++)
            {
                int nx = x + dx, ny = y + dy;
                if (nx < 0 || nx >= Size || ny < 0 || ny >= Size || !mask[nx, ny] || (dx == 0 && dy == 0)) continue;
                // Orthogonal neighbours of a cell are part of the same ship. Diagonals are never allowed.
                if (dx != 0 && dy != 0) return false;
                // Horizontal/vertical touching across separate ships is detected as an overlong component.
            }
        }
        return true;
    }

    static List<Vector2Int> FloodShip(bool[,] mask, bool[,] visited, Vector2Int start)
    {
        var cells = new List<Vector2Int>();
        var queue = new Queue<Vector2Int>();
        queue.Enqueue(start); visited[start.x, start.y] = true;
        Vector2Int[] directions = { Vector2Int.left, Vector2Int.right, Vector2Int.up, Vector2Int.down };
        while (queue.Count > 0)
        {
            Vector2Int cell = queue.Dequeue(); cells.Add(cell);
            foreach (Vector2Int direction in directions)
            {
                Vector2Int next = cell + direction;
                if (InBounds(next) && mask[next.x, next.y] && !visited[next.x, next.y])
                { visited[next.x, next.y] = true; queue.Enqueue(next); }
            }
        }
        return cells;
    }

    static bool PlaceNext(int[] fleet, int index, bool[,] occupied, List<Vector2Int[]> result, System.Random random)
    {
        if (index == fleet.Length) return true;
        int length = fleet[index];
        var candidates = new List<Vector2Int[]>();
        for (int x = 0; x < Size; x++)
        for (int y = 0; y < Size; y++)
        for (int axis = 0; axis < 2; axis++)
        {
            var cells = new Vector2Int[length];
            for (int i = 0; i < length; i++) cells[i] = new Vector2Int(x + (axis == 0 ? i : 0), y + (axis == 1 ? i : 0));
            if (cells[length - 1].x >= Size || cells[length - 1].y >= Size || !CanPlace(cells, occupied)) continue;
            candidates.Add(cells);
        }

        for (int i = candidates.Count - 1; i > 0; i--)
        {
            int swap = random.Next(i + 1);
            (candidates[i], candidates[swap]) = (candidates[swap], candidates[i]);
        }

        foreach (Vector2Int[] cells in candidates)
        {
            Mark(cells, occupied, true); result.Add(cells);
            if (PlaceNext(fleet, index + 1, occupied, result, random)) return true;
            result.RemoveAt(result.Count - 1); Mark(cells, occupied, false);
        }
        return false;
    }

    static bool CanPlace(Vector2Int[] cells, bool[,] occupied)
    {
        foreach (Vector2Int cell in cells)
        for (int dx = -1; dx <= 1; dx++)
        for (int dy = -1; dy <= 1; dy++)
        {
            int nx = cell.x + dx, ny = cell.y + dy;
            if (nx >= 0 && nx < Size && ny >= 0 && ny < Size && occupied[nx, ny]) return false;
        }
        return true;
    }

    static void Mark(Vector2Int[] cells, bool[,] occupied, bool value)
    {
        foreach (Vector2Int cell in cells) occupied[cell.x, cell.y] = value;
    }
}

public sealed class BattleshipBot
{
    static readonly Vector2Int[] Directions = { Vector2Int.left, Vector2Int.right, Vector2Int.up, Vector2Int.down };
    readonly HashSet<Vector2Int> tried = new HashSet<Vector2Int>();
    readonly HashSet<Vector2Int> targets = new HashSet<Vector2Int>();
    readonly HashSet<Vector2Int> forbidden = new HashSet<Vector2Int>();
    readonly System.Random random;

    public BattleshipBot(System.Random random = null) { this.random = random ?? new System.Random(); }

    public Vector2Int ChooseShot()
    {
        var candidates = new List<Vector2Int>();
        foreach (Vector2Int target in targets)
            if (CanShoot(target)) candidates.Add(target);
        if (candidates.Count == 0)
        {
            for (int x = 0; x < LocalBoard.Size; x++)
            for (int y = 0; y < LocalBoard.Size; y++)
            {
                Vector2Int cell = new Vector2Int(x, y);
                if (CanShoot(cell) && ((x + y) & 1) == 0) candidates.Add(cell);
            }
        }
        if (candidates.Count == 0)
        {
            for (int x = 0; x < LocalBoard.Size; x++)
            for (int y = 0; y < LocalBoard.Size; y++)
            {
                Vector2Int cell = new Vector2Int(x, y);
                if (CanShoot(cell)) candidates.Add(cell);
            }
        }
        if (candidates.Count == 0) throw new InvalidOperationException("У бота не осталось доступных клеток.");
        Vector2Int shot = candidates[random.Next(candidates.Count)];
        tried.Add(shot);
        targets.Remove(shot);
        return shot;
    }

    public void Observe(Vector2Int shot, LocalShotResult result, LocalShip sunkShip = null)
    {
        if (result == LocalShotResult.Hit || result == LocalShotResult.Sunk)
        {
            if (sunkShip != null)
            {
                foreach (Vector2Int cell in sunkShip.Cells)
                for (int dx = -1; dx <= 1; dx++)
                for (int dy = -1; dy <= 1; dy++)
                {
                    Vector2Int blocked = cell + new Vector2Int(dx, dy);
                    if (LocalBoard.InBounds(blocked)) { forbidden.Add(blocked); targets.Remove(blocked); }
                }
            }
            else
            {
                foreach (Vector2Int direction in Directions)
                {
                    Vector2Int next = shot + direction;
                    if (LocalBoard.InBounds(next) && !tried.Contains(next) && !forbidden.Contains(next)) targets.Add(next);
                }
            }
        }
    }

    bool CanShoot(Vector2Int cell) => LocalBoard.InBounds(cell) && !tried.Contains(cell) && !forbidden.Contains(cell);
}

public sealed class LocalGameManager
{
    public LocalBoard PlayerBoard { get; }
    public LocalBoard BotBoard { get; }
    public BattleshipBot Bot { get; }
    public bool PlayerTurn { get; private set; } = true;
    public bool Finished { get; private set; }
    public bool PlayerWon { get; private set; }

    public LocalGameManager(LocalBoard playerBoard, LocalBoard botBoard, BattleshipBot bot)
    {
        PlayerBoard = playerBoard; BotBoard = botBoard; Bot = bot;
    }

    public LocalShotResult PlayerShoot(Vector2Int cell)
    {
        if (Finished || !PlayerTurn) return LocalShotResult.Invalid;
        LocalShotResult result = BotBoard.Shoot(cell);
        if (result == LocalShotResult.AlreadyShot || result == LocalShotResult.Invalid) return result;
        if (BotBoard.AllShipsSunk) { Finished = true; PlayerWon = true; }
        else if (result == LocalShotResult.Miss) PlayerTurn = false;
        return result;
    }

    public LocalShotResult BotShoot()
    {
        if (Finished || PlayerTurn) return LocalShotResult.Invalid;
        return BotShoot(Bot.ChooseShot());
    }

    public LocalShotResult BotShoot(Vector2Int cell)
    {
        if (Finished || PlayerTurn) return LocalShotResult.Invalid;
        LocalShotResult result = PlayerBoard.Shoot(cell);
        LocalShip hitShip = PlayerBoard.ShipAt(cell);
        Bot.Observe(cell, result, result == LocalShotResult.Sunk ? hitShip : null);
        if (PlayerBoard.AllShipsSunk) { Finished = true; PlayerWon = false; }
        else if (result == LocalShotResult.Miss) PlayerTurn = true;
        return result;
    }
}
