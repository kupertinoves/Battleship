using UnityEngine;
using UnityEngine.UI;

public class ToggleButtonColor : MonoBehaviour
{
    [SerializeField] Button button;
    [SerializeField] Color selectedColor;
    [SerializeField] Color unselectedColor;
    private bool isGreen = false;

    void OnButtonClick()
    {
        isGreen = !isGreen;
        UpdateColor();
    }

    void UpdateColor()
    {
        if (button == null) return;

        ColorBlock colors = button.colors;
        colors.normalColor = isGreen ? selectedColor : unselectedColor;
        button.colors = colors;
    }
}
