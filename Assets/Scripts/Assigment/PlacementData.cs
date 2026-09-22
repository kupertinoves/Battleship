using System.Collections.Generic;
using UnityEngine;

public static class PlacementData
{
    // Список всех клеток с кораблями
    public static List<Vector2Int> ShipCells = new List<Vector2Int>();

    public static void Clear()
    {
        ShipCells.Clear();
    }
}
