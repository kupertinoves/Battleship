using UnityEngine;
using UnityEngine.UI;

public class CellView : MonoBehaviour
{
    public int x;
    public int y;

    [Header("Cell colors")]
    [SerializeField] Color emptyColor;
    [SerializeField] Color shipColor;

    private bool hasShip = false;
    private Image image;

    public System.Action<int, int, bool> OnCellToggled;

    void Awake()
    {
        image = GetComponent<Image>();
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
