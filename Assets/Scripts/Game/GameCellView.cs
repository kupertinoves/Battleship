using UnityEngine;
using UnityEngine.UI;
using TMPro;

public enum GameCellState
{
    Empty,
    Ship,
    Miss,
    Hit
}

[RequireComponent(typeof(RectTransform))]
public class GameCellView : MonoBehaviour
{
    public Vector2Int Coord;

    [Header("Prefab references")]
    [SerializeField] Image image;
    [SerializeField] TMP_Text marker;

    [Header("Cell colors")]
    [SerializeField] Color emptyColor;
    [SerializeField] Color shipColor;
    [SerializeField] Color missColor;
    [SerializeField] Color hitColor;

    private GameCellState state;

    private void Awake()
    {
        if (image == null) image = GetComponent<Image>();
        if (marker == null) marker = GetComponentInChildren<TMP_Text>(true);
        if (marker != null) marker.text = string.Empty;
        if (image != null) image.color = emptyColor;
    }

    public void SetState(GameCellState newState)
    {
        state = newState;
        if (marker != null) marker.text = state == GameCellState.Hit ? "×" : state == GameCellState.Miss ? "•" : "";

        if (image == null) return; // защита на всякий случай

        switch (state)
        {
            case GameCellState.Empty: image.color = emptyColor; break;
            case GameCellState.Ship:  image.color = shipColor;  break;
            case GameCellState.Miss:  image.color = missColor;  break;
            case GameCellState.Hit:   image.color = hitColor;   break;
        }
    }

    public GameCellState GetState()
    {
        return state;
    }
}
