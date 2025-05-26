using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using UnityEngine.UI;
using TMPro;
using System.Collections.Concurrent;

public class MenuManager : MonoBehaviour
{
    public TMP_InputField inputName;
    public string name;

    //Paneles:
    public GameObject namePanel;
    public GameObject finalPanel;



    public void Awake()
    {
        Time.timeScale = 1f; // Asegúrate de que el tiempo está restaurado al cargar la escena
    }


    public void StartGame()
    {
        NetworkManager.Singleton.SceneManager.LoadScene("GameScene", UnityEngine.SceneManagement.LoadSceneMode.Single);
        //SceneManager.LoadScene("GameScene"); // Cambia "MainScene" por el nombre de tu escena principal

    }

    public void NamePanel()
    {
        namePanel.SetActive(false);
        name = inputName.text;

        var allPlayers = GameObject.FindGameObjectsWithTag("Player");
        foreach ( var player in allPlayers )
        {
            if ( player.GetComponent<NetworkObject>().IsOwner)
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

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false; // Salir en el editor
#else
            Application.Quit(); // Salir en una build
#endif
    }
}
