using UnityEngine.SceneManagement;
using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class PlacementScreen : MonoBehaviour
{
    public GridController gridController;
    public TMP_Text feedbackText;

    void Start()
    {
        if (gridController != null)
        {
            gridController.OnPlacementUpdated += UpdateFeedback;
            gridController.OnPlacementRejected += SetFeedback;
            UpdateFeedback(gridController.ShipCellCount);
        }
    }

    public void OnReadyClicked()
    {
        if (gridController == null)
        {
            SetFeedback("Не удалось загрузить поле расстановки.");
            return;
        }

        if (PlacementValidator.TryValidate(
            gridController.grid,
            out List<Vector2Int> cells,
            out string error
        ))
        {
            SetFeedback("Флот готов к бою.");

            PlacementData.Clear();
            PlacementData.ShipCells.AddRange(cells);

            // The existing WebSocket client connects when the Game scene starts.
            SceneManager.LoadScene("Game");
        }
        else
        {
            SetFeedback(error);
        }
    }

    void UpdateFeedback(int shipCells)
    {
        int remaining = GridController.MaximumShipCells - shipCells;
        SetFeedback(remaining == 0
            ? "20 из 20 клеток. Проверьте форму и нажмите «В бой»."
            : $"Размещено: {shipCells}/20 клеток. Осталось: {remaining}.");
    }

    void SetFeedback(string message)
    {
        if (feedbackText != null) feedbackText.text = message;
    }
}
