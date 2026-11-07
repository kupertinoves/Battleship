using UnityEngine;
using UnityEngine.UI;

public class CellView : MonoBehaviour
{
    public int x;
    public int y;

    [Header("Cell colors")]
    // Значения по умолчанию: в Cell.prefab цвета не заданы, и без них клетки были прозрачными.
    [SerializeField] Color emptyColor = new Color(0.10f, 0.20f, 0.27f, 0.85f);
    [SerializeField] Color shipColor = new Color(0.20f, 0.72f, 0.82f, 1f);

    private bool hasShip = false;
    private Image image;

    public System.Action<int, int, bool> OnCellToggled;

    void Awake()
    {
        image = GetComponent<Image>();
        // если в префабе/сцене сохранены нулевые (прозрачные) цвета — используем значения по умолчанию
        if (emptyColor.a <= 0f) emptyColor = new Color(0.10f, 0.20f, 0.27f, 0.85f);
        if (shipColor.a <= 0f) shipColor = new Color(0.20f, 0.72f, 0.82f, 1f);
        UpdateVisual();
    }

    public void Init(int x, int y)
    {
        this.x = x;
        this.y = y;
    }

    public void Toggle()
    {
        hasShip = !hasShip;
        UpdateVisual();
        OnCellToggled?.Invoke(x, y, hasShip);
    }

    public void SetShip(bool value)
    {
        hasShip = value;
        UpdateVisual();
        OnCellToggled?.Invoke(x, y, hasShip);
    }

    public void SetShipWithoutNotify(bool value)
    {
        hasShip = value;
        UpdateVisual();
    }

    void UpdateVisual()
    {
        if (image == null) return;
        image.color = hasShip ? shipColor : emptyColor;
    }
}
