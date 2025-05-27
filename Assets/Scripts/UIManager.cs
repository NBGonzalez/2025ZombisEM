using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using UnityEngine.UI;
using TMPro;
using System.Collections.Concurrent;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Relay;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;

public class UIManager : MonoBehaviour
{
    public TMP_InputField inputName;
    public TMP_InputField inputJoinCode;
    public string name;

    public const int maxConnections = 10;
    public string joinCode = "Enter room code...";
    public string gameModeSelected;

    public GameManager gameManager;

    //Paneles:
    public GameObject relayPanel;
    public GameObject gamemodePanel;
    public GameObject timeSelectionPanel;
    public GameObject namePanel;
    public GameObject finalPanel;
    public GameObject STATUSPANEL;
    public GameObject titlePanel;



    public void Awake()
    {
        Time.timeScale = 1f; // Asegúrate de que el tiempo está restaurado al cargar la escena
        relayPanel.SetActive(true);
        titlePanel.SetActive(true);

    }

    
    // Primero salen los paneles de Relay.
    public async void StartHost()
    {
        await UnityServices.InitializeAsync();
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
        var allocation = await RelayService.Instance.CreateAllocationAsync(maxConnections);
        NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(new RelayServerData(allocation, "dtls"));
        joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

        NetworkManager.Singleton.StartHost();

        // Una vez que el host se ha iniciado, ocultamos el panel de Relay y mostramos el panel de nombre
        relayPanel.SetActive(false);
        gamemodePanel.SetActive(true);
        StatusLabels();
    }
    public async void StartClient()
    {
        await UnityServices.InitializeAsync();
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }

        Debug.Log("Attempting to join with code: " + joinCode);
        //joinCode = inputJoinCode.text;
        var allocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
        NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(new RelayServerData(allocation, "dtls"));
        NetworkManager.Singleton.StartClient();

        // Una vez que el host se ha iniciado, ocultamos el panel de Relay y mostramos el panel de nombre
        relayPanel.SetActive(false);

        namePanel.SetActive(true);

        StatusLabels();
    }

    public void GetJoinCode(string code)
    {
        joinCode = code;
        Debug.Log("Join code set to: " + joinCode);

    }

    public void StatusLabels()
    {
        STATUSPANEL.SetActive(true);
        var mode = NetworkManager.Singleton.IsHost ?
            "Host" : NetworkManager.Singleton.IsServer ? "Server" : "Client";

        //STATUSPANEL.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = "Transport: " +
            //NetworkManager.Singleton.NetworkConfig.NetworkTransport.GetType().Name;
        STATUSPANEL.transform.GetChild(1).GetComponent<TextMeshProUGUI>().text = "Mode: " + mode;
        STATUSPANEL.transform.GetChild(2).GetComponent<TextMeshProUGUI>().text = "Room: " + joinCode;
    }

    
    // Si eres Host, muestra el panel de Gamemode.
    public void CoinGameModeSelected()
    {
        gameModeSelected = "CoinGame";
        //gameManager.SetGameMode(gameModeSelected);
        gameManager.CurrentGameMode = gameModeSelected;
        gamemodePanel.SetActive(false);
        namePanel.SetActive(true);
    }
    public void TimeGameModeSelected()
    {
        gameModeSelected = "TimeGame";
        //gameManager.SetGameMode(gameModeSelected);
        gameManager.CurrentGameMode = gameModeSelected;
        gamemodePanel.SetActive(false);
        timeSelectionPanel.SetActive(true);
    }

    // Si eres Host, muestra el panel para poner el tiempo de juego.
    public void TimeSelected(int time)
    {
        gameManager.SetTime(time);
        Debug.Log("Tiempo seleccionado: " + time + " minutos.");
        timeSelectionPanel.SetActive(false);
        namePanel.SetActive(true);
    }


    // A todos, muestra el panel de nombre
    public void NamePanel()
    {
        namePanel.SetActive(false);
        name = inputName.text;

        var allPlayers = GameObject.FindGameObjectsWithTag("Player");
        foreach (var player in allPlayers)
        {
            if (player.GetComponent<NetworkObject>().IsOwner)
            {
                player.GetComponent<PlayerController>().networkName.Value = name;
            }
            else
            {
                player.gameObject.transform.GetChild(4).GetChild(0).GetComponent<TextMeshProUGUI>().text = player.GetComponent<PlayerController>().networkName.Value.ToString();
            }

        }
        Debug.Log(name);
        finalPanel.SetActive(true);
    }

    // Cuando todos los jugadores han puesto su nombre, se inicia el juego.
    public void StartGame()
    {
        NetworkManager.Singleton.SceneManager.LoadScene("GameScene", UnityEngine.SceneManagement.LoadSceneMode.Single);
        //SceneManager.LoadScene("GameScene"); // Cambia "MainScene" por el nombre de tu escena principal

    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false; // Salir en el editor
#else
            Application.Quit(); // Salir en una build
#endif
    }
}
