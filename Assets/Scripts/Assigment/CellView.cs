using UnityEngine;
using UnityEngine.UI;

public class CellView : MonoBehaviour
{
    public int x;
    public int y;

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

    void UpdateVisual()
    {
        image.color = hasShip ? Color.gray : Color.white;
    }
}
