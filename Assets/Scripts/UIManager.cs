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
using UnityEditor;
using System.Runtime.CompilerServices;
using System.Collections.Generic;

public class UIManager : NetworkBehaviour
{
    public TMP_InputField inputName;
    public TMP_InputField inputJoinCode;
    public string name;

    public const int maxConnections = 10;
    public string joinCode = "Enter room code...";
    public string gameModeSelected;

    public GameManager gameManager;

    private NetworkVariable<int> networkReadyPlayers = new NetworkVariable<int>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public Dictionary<ulong, string> allNetworkNames = new Dictionary<ulong, string>();

    // Paneles:
    public GameObject relayPanel;
    public GameObject gamemodePanel;
    public GameObject timeSelectionPanel;
    public GameObject namePanel;
    public GameObject readyPanel;
    public GameObject finalPanel;
    public GameObject STATUSPANEL;
    public GameObject titlePanel;

    public TextMeshProUGUI readyPlayersText;

    public GameObject prefab; // Prefab del jugador

    // Botones:
    public GameObject readyButton;
    public GameObject notReadyButton;

    public static bool hasNetworkConnection = false;

    public void Awake()
    {

        readyPlayersText = GameObject.Find("ReadyPlayersText").GetComponent<TextMeshProUGUI>();
        gameManager = GameObject.FindGameObjectWithTag("GameController").GetComponent<GameManager>();

        //Si ya tenemos conexión de red, saltamos directo a la selección de modo
        if (hasNetworkConnection && NetworkManager.Singleton != null &&
            (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsClient))
        {
            // Vamos directo al panel de selección de modo si somos host
            // o al panel de nombre si somos cliente
            relayPanel.SetActive(false);
            titlePanel.SetActive(false);

            if (NetworkManager.Singleton.IsHost)
            {
                gamemodePanel.SetActive(true);
            }
            else
            {
                namePanel.SetActive(true);
            }

            StatusLabels();
        }
        else
        {
            // Primera vez, mostramos los paneles de conexión
            relayPanel.SetActive(true);
            titlePanel.SetActive(true);
            hasNetworkConnection = false;
        }

    }
    private void Start()
    {

        Time.timeScale = 1f;
        if (NetworkManager.Singleton.IsHost)
        {
            // Si somos el host, spawneamos los jugadores al iniciar
            foreach (var clientId in NetworkManager.Singleton.ConnectedClientsIds)
            {
                GameObject player = Instantiate(prefab, new Vector3(0, 0, 0), Quaternion.identity);
                NetworkObject playerNetworkObject = player.GetComponent<NetworkObject>();
                playerNetworkObject.SpawnAsPlayerObject(clientId);
                //playerNetworkObject.ChangeOwnership(clientId); // Asigna la propiedad al cliente

                player.GetComponent<PlayerController>().OnNetworkSpawn();
            }
        }

    }

    private void OnEnable()
    {
        networkReadyPlayers.OnValueChanged += OnReadyPlayersChanged;
    }

    private void OnDisable()
    {
        networkReadyPlayers.OnValueChanged -= OnReadyPlayersChanged;
    }

    private void OnReadyPlayersChanged(int previousValue, int newValue)
    {
        readyPlayersText.text = "Jugadores listos: " + newValue + "/" + GameManager.Instance.GetNumberOfPlayers();

        if (NetworkManager.Singleton.IsHost && newValue >= GameManager.Instance.GetNumberOfPlayers())
        {
            StartGame();
        }
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
        hasNetworkConnection = true;
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
        hasNetworkConnection = true;
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
            player.GetComponent<PlayerController>().name = player.GetComponent<PlayerController>().networkName.Value.ToString();
            //player.gameObject.transform.GetChild(4).GetChild(0).GetComponent<TextMeshProUGUI>().text = name;
            if (player.GetComponent<NetworkObject>().IsOwner)
            {
                player.GetComponent<PlayerController>().networkName.Value = name;
                gameManager.SetPlayerNameServerRpc(player.GetComponent<NetworkObject>().OwnerClientId, name);

            }
        }

        Debug.Log(name);
        readyPanel.SetActive(true);
    }

    public void ReadyPanel()
    {
        //readyPanel.SetActive(true);
        readyButton.SetActive(true);
        notReadyButton.SetActive(false);
        NotPlayerReadyServerRpc();
        readyPlayersText.text = "Jugadores listos: " + networkReadyPlayers.Value + "/" + GameManager.Instance.GetNumberOfPlayers();

    }
    public void NotReadyPanel()
    {

        notReadyButton.SetActive(true);
        readyButton.SetActive(false);
        PlayerReadyServerRpc();
        readyPlayersText.text = "Jugadores listos: " + networkReadyPlayers.Value + "/" + GameManager.Instance.GetNumberOfPlayers();
    }

    [ServerRpc(RequireOwnership = false)]
    public void PlayerReadyServerRpc()
    {
        // Incrementar el contador de jugadores listos
        networkReadyPlayers.Value++;
        Debug.Log("Jugadores listos: " + networkReadyPlayers.Value);

        // Verificar si todos los jugadores están listos
        if (networkReadyPlayers.Value >= GameManager.Instance.GetNumberOfPlayers())
        {
            StartGame();
        }
    }
    [ServerRpc(RequireOwnership = false)]
    public void NotPlayerReadyServerRpc()
    {

        networkReadyPlayers.Value--;
        Debug.Log("NotPlayerReadyServerRpc called. Current ready players: " + networkReadyPlayers.Value);
    }

    // Cuando todos los jugadores han puesto su nombre, se inicia el juego.
    public void StartGame()
    {
        var allPlayers = GameObject.FindGameObjectsWithTag("Player");
        foreach (var player in allPlayers)
        {
            player.GetComponent<NetworkObject>().Despawn();
        }

        NetworkManager.Singleton.SceneManager.LoadScene("GameScene", UnityEngine.SceneManagement.LoadSceneMode.Single);
        //SceneManager.LoadScene("GameScene"); // Cambia "MainScene" por el nombre de tu escena principal

    }
    public void RestartGame()
    {
        // Resetear los paneles
        relayPanel.SetActive(false);
        titlePanel.SetActive(false);
        gamemodePanel.SetActive(false);
        timeSelectionPanel.SetActive(false);
        namePanel.SetActive(false);
        finalPanel.SetActive(false);

        // Mostrar el panel apropiado según el rol
        if (NetworkManager.Singleton.IsHost)
        {
            gamemodePanel.SetActive(true);
        }
        else
        {
            namePanel.SetActive(true);
        }

        StatusLabels();
    }
    public void QuitGame()
    {
        // Resetear la conexión al salir completamente
        hasNetworkConnection = false;

        // Desconectar de la red
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.Shutdown();
        }
    }
}