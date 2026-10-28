using UnityEngine;

/// <summary>Connects the scene-authored connection panel to the existing client.</summary>
public static partial class UIRuntimePolish
{
    public static void ShowConnectionMenu(GameNgoClient connection)
    {
        ConnectionMenuUI menu = Object.FindAnyObjectByType<ConnectionMenuUI>();
        if (menu != null) menu.Bind(connection);
        else Debug.LogError("Game scene is missing its editor-authored ConnectionMenuUI.");
    }
}
