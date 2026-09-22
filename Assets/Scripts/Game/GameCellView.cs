using UnityEngine;
using UnityEngine.UI;

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

    [Header("Colors")]
    public Color emptyColor = Color.white;  // делаем сразу белым
    public Color shipColor = Color.gray;
    public Color missColor = Color.blue;
    public Color hitColor = Color.red;

    private GameCellState state;

    private Image image;

    private void Awake()
    {
        // Создаем компонент Image, если его нет
        image = GetComponent<Image>();
        if (image == null)
        {
            image = gameObject.AddComponent<Image>();
        }

        // Цвет по умолчанию
        image.color = emptyColor;

        // Если нужен Button для клика по enemyField, можно добавить
        if (GetComponent<Button>() == null)
        {
            gameObject.AddComponent<Button>();
        }
    }

    public void SetState(GameCellState newState)
    {
        state = newState;

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
