using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class NetworkEvents : MonoBehaviour
{
    //public static NetworkEvents Instance { get; private set; }
    public TextMeshProUGUI disconectedText;
    private void Awake()
    {

    }
    private void Start()
    {
        disconectedText = GameObject.Find("DisconectedText").GetComponent<TextMeshProUGUI>();
        disconectedText.enabled = false;
    }
    void OnEnable()
    {
        NetworkManager.Singleton.OnClientDisconnectCallback += HandleClientDisconnect;
    }

    void OnDisable()
    {
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientDisconnectCallback -= HandleClientDisconnect;
    }

    private void HandleClientDisconnect(ulong clientId)
    {
        Debug.Log($"Jugador con ClientId {clientId} se ha desconectado.");

        if (disconectedText == null)
            disconectedText = GameObject.Find("DisconectedText").GetComponent<TextMeshProUGUI>();

        NetworkManager.Singleton.StartCoroutine(ShowDisconectedClient(clientId));
    }
    IEnumerator ShowDisconectedClient(ulong clientId)
    {
        disconectedText.enabled = true;
        disconectedText.text = $"El jugador: {GameManager.Instance.GetPlayerName(clientId)} se ha desconectado.";
        yield return new WaitForSeconds(3f);
        disconectedText.enabled = false;
    }
}
