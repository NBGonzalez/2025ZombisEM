using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class GameManager : NetworkBehaviour
{
    NetworkManager _networkManager;
    GameObject _playerPrefab;

    public int maxPlayers;
    int playerId;
    public static GameManager Instance { get; private set; }

    // Variable de red para sincronizar el modo de juego entre el servidor y los clientes
    NetworkVariable<FixedString64Bytes> networkGameMode = new(writePerm: NetworkVariableWritePermission.Server, readPerm: NetworkVariableReadPermission.Everyone);
    NetworkVariable<int> networkTime = new(writePerm: NetworkVariableWritePermission.Server, readPerm: NetworkVariableReadPermission.Everyone);
    public NetworkVariable<int> numberOfPlayers = new(writePerm: NetworkVariableWritePermission.Server, readPerm: NetworkVariableReadPermission.Everyone);
    public NetworkVariable<int> networkSeed = new(writePerm: NetworkVariableWritePermission.Server, readPerm: NetworkVariableReadPermission.Everyone);
    public NetworkVariable<int> CoinsGenerated = new(writePerm: NetworkVariableWritePermission.Server, readPerm: NetworkVariableReadPermission.Everyone);
    public NetworkVariable<int> TotalCoinsCollected = new(writePerm: NetworkVariableWritePermission.Server, readPerm: NetworkVariableReadPermission.Everyone);

    [SerializeField] public Dictionary<ulong, string> networkPlayerNames = new Dictionary<ulong, string>();


    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(this);
        }
        else
        {
            Destroy(gameObject); // Esto evita múltiples instancias
        }

        networkGameMode.OnValueChanged += (oldValue, newValue) => { Debug.Log($"Sincronizado en cliente: {newValue}"); };
        networkTime.OnValueChanged += (oldValue, newValue) => { Debug.Log($"Tiempo sincronizado en cliente: {newValue}"); };

    }

    // Start is called before the first frame update
    void Start()
    {

        if (IsServer)
        {
            GetComponent<NetworkObject>().Spawn();
            networkSeed.Value = UnityEngine.Random.Range(0, 10000); // Generar un seed aleatorio para el servidor
        }


        maxPlayers = UIManager.maxConnections;

        _networkManager = NetworkManager.Singleton;
        _playerPrefab = _networkManager.NetworkConfig.Prefabs.Prefabs[0].Prefab; // Aqui cogemos el humano, para el orco es Prefabs[1]

        _networkManager.OnServerStarted += OnServerStarted;
        _networkManager.OnClientConnectedCallback += OnClientConnected;

        // Spawnear jugadores al cambiar de escena

    }

    private void OnClientConnected(ulong obj) // Solo se ejecuta en el jugador
    {
        if (IsServer)
        {
            var player = Instantiate(_playerPrefab);
            player.GetComponent<NetworkObject>().SpawnAsPlayerObject(obj);
            numberOfPlayers.Value++;
        }

    }

    private void OnServerStarted()
    {
        print("El servidor está listo");
    }

    // Update is called once per frame


    #region GAMEMODE
    public string CurrentGameMode
    {
        get => networkGameMode.Value.ToString();
        set => networkGameMode.Value = value;
    }
    public void SetGameMode(string mode)
    {
        networkGameMode.Value = mode;
    }
    public string GetGameMode()
    {
        return networkGameMode.Value.ToString();
    }
    #endregion

    #region TIME
    public void SetTime(int time)
    {
        networkTime.Value = time;
    }
    public int GetTime()
    {
        return networkTime.Value;
    }
    #endregion

    public int GetNumberOfPlayers()
    {
        return numberOfPlayers.Value;
    }
    public int GetNetworkSeed()
    {
        return networkSeed.Value;
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetPlayerNameServerRpc(ulong clientId, string name)
    {
        // Aseguramos que el nombre se añade al diccionario en el cliente
        if (!this.networkPlayerNames.ContainsKey(clientId))
        {
            this.networkPlayerNames.Add(clientId, name);
        }
        else
        {
            this.networkPlayerNames[clientId] = name; // Actualizamos el nombre si ya existe
        }
        //Debug.Log($"Nombre del jugador {name} establecido en cliente: {clientId}");
        SetPlayerNameClientRpc(clientId, name);
    }

    [ClientRpc(RequireOwnership = false)]
    void SetPlayerNameClientRpc(ulong clientId, string playerName)
    {
        if (!this.networkPlayerNames.ContainsKey(clientId))
        {
            this.networkPlayerNames.Add(clientId, playerName);
        }
        else
        {
            this.networkPlayerNames[clientId] = playerName; // Actualizamos el nombre si ya existe
        }

        //Debug.Log($"Nombre sincronizado en cliente {NetworkManager.Singleton.LocalClientId}: {clientId} -> {playerName}");
    }

    public void SetPlayerName(string name, ulong clientId)
    {

        networkPlayerNames.TryAdd(clientId, name); // Aseguramos que el nombre se añade al diccionario
        //Debug.Log($"Nombre del jugador {name} establecido: {clientId}");

    }
    public string GetPlayerName(ulong clientId)
    {
        if (networkPlayerNames.TryGetValue(clientId, out string name))
        {
            return name;
        }
        else
        {
            return "Jugador Desconocido"; // Valor por defecto si no se encuentra el nombre
        }
    }

    [Rpc(SendTo.ClientsAndHost)]
    public void ResetGameRequestRpc()
    {
        networkGameMode.Value = new FixedString64Bytes("Default");

        networkSeed.Value = UnityEngine.Random.Range(0, 10000);
        CoinsGenerated.Value = 0;
        TotalCoinsCollected.Value = 0;
        networkPlayerNames.Clear();
        Debug.Log("Juego reiniciado y variables de red restablecidas.");
    }
}