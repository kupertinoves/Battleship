using UnityEngine.SceneManagement;
using UnityEngine;
using System.Collections.Generic;

public class PlacementScreen : MonoBehaviour
{
    public GridController gridController;

    public void OnReadyClicked()
    {
        if (PlacementValidator.TryValidate(
            gridController.grid,
            out List<Vector2Int> cells,
            out string error
        ))
        {
            Debug.Log("Placement OK!");

            PlacementData.Clear();
            PlacementData.ShipCells.AddRange(cells);

// теперь можно переходить в сцену Game
            SceneManager.LoadScene("Game");

            // TODO: отправка по WebSocket
        }
        else
        {
            Debug.LogError("Placement error: " + error);
            // TODO: показать ошибку на UI (текстом на экране)
        }
    }
}
