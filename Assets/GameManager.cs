using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class GameManager : NetworkBehaviour
{
    NetworkManager _networkManager;
    GameObject _playerPrefab;
    
    int maxPlayers;
    int playerId;
    public static GameManager Instance { get; private set; }

    // Variable de red para sincronizar el modo de juego entre el servidor y los clientes
    NetworkVariable<FixedString64Bytes> networkGameMode = new(writePerm: NetworkVariableWritePermission.Server, readPerm: NetworkVariableReadPermission.Everyone);
    NetworkVariable<int> networkTime = new(writePerm: NetworkVariableWritePermission.Server, readPerm: NetworkVariableReadPermission.Everyone);

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
        var player = Instantiate(_playerPrefab);
        player.GetComponent<NetworkObject>().SpawnAsPlayerObject(obj);
    }

    private void OnServerStarted()
    {
        print("El servidor está listo");
    }

    // Update is called once per frame
    void Update()
    {
        
    }

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

}
