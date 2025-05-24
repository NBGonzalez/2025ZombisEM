using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;

public class MenuManager : MonoBehaviour
{
    public void Awake()
    {
        Time.timeScale = 1f; // Aseg�rate de que el tiempo est� restaurado al cargar la escena
    }

    public void StartGame()
    {
        NetworkManager.Singleton.SceneManager.LoadScene("GameScene", UnityEngine.SceneManagement.LoadSceneMode.Single); 
        //NetworkManager.Singleton.SceneManager.LoadScene("NombreDeLaEscena", UnityEngine.SceneManagement.LoadSceneMode.Single);
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
