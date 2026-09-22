using UnityEngine;
using UnityEngine.UI;

public class ToggleButtonColor : MonoBehaviour
{
    public Button button;
    private bool isGreen = false;

    void OnButtonClick()
    {
        isGreen = !isGreen;
        UpdateColor();
    }

    void UpdateColor()
    {
        ColorBlock colors = button.colors;

        if (isGreen)
            colors.normalColor = Color.green;
        else
            colors.normalColor = Color.red;

        button.colors = colors;
    }
}