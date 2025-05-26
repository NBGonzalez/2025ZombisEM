using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class GameManager : NetworkBehaviour
{
    NetworkManager _networkManager;
    GameObject _playerPrefab;
    
    int maxPlayers;
    int playerId;
    public static GameManager Instance { get; private set; }

    private void Awake()
    {
        DontDestroyOnLoad(this);
    }

    // Start is called before the first frame update
    void Start()
    {
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
}
