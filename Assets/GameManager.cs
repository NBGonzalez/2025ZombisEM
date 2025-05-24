using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    NetworkManager _networkManager;
    GameObject _playerPrefab;

    // Start is called before the first frame update
    void Start()
    {
        _networkManager = NetworkManager.Singleton;
        _playerPrefab = _networkManager.NetworkConfig.Prefabs.Prefabs[0].Prefab; // Aqui cogemos el humano, para el orco es Prefabs[1]

        _networkManager.OnServerStarted += OnServerStarted;
        _networkManager.OnClientConnectedCallback += OnClientConnected;
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
