using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Wires the editor-authored connection panel to the existing client API.</summary>
public sealed class ConnectionMenuUI : MonoBehaviour
{
    [SerializeField] GameNgoClient connection;
    [SerializeField] GameObject overlay;
    [SerializeField] TMP_InputField addressInput;
    [SerializeField] TMP_InputField portInput;
    [SerializeField] TMP_Text statusText;
    [SerializeField] Button hostButton;
    [SerializeField] Button joinButton;
    [SerializeField] Button backButton;

    void OnEnable()
    {
        if (connection == null) return;
        hostButton.onClick.AddListener(Host);
        joinButton.onClick.AddListener(Join);
        backButton.onClick.AddListener(Back);
        connection.StatusChanged += Refresh;
        connection.ConnectionChanged += OnConnectionChanged;
        Refresh(connection.Status);
    }

    void OnDisable()
    {
        if (hostButton != null) hostButton.onClick.RemoveListener(Host);
        if (joinButton != null) joinButton.onClick.RemoveListener(Join);
        if (backButton != null) backButton.onClick.RemoveListener(Back);
        if (connection == null) return;
        connection.StatusChanged -= Refresh;
        connection.ConnectionChanged -= OnConnectionChanged;
    }

    public void Bind(GameNgoClient client)
    {
        if (connection == client) { Refresh(client.Status); return; }
        if (isActiveAndEnabled) OnDisable();
        connection = client;
        if (isActiveAndEnabled) OnEnable();
    }

    void Host() => connection.Host(portInput.text);

    void Join()
    {
        string address = addressInput.text.Trim();
        PlayerPrefs.SetString("Battleship.HostAddress", address);
        connection.Join(address, portInput.text);
    }

    void Back()
    {
        if (connection.Busy) connection.Cancel();
        else SceneManager.LoadScene("MainMenu");
        Refresh(connection.Status);
    }

    void OnConnectionChanged(bool connected) => Refresh(connection.Status);

    void Refresh(string status)
    {
        if (statusText != null) statusText.text = status;
        bool available = connection != null && !connection.Busy;
        if (hostButton != null) hostButton.interactable = available;
        if (joinButton != null) joinButton.interactable = available;
        if (addressInput != null) addressInput.interactable = available;
        if (portInput != null) portInput.interactable = available;
        if (overlay != null) overlay.SetActive(connection == null || !connection.Connected);
    }
}
