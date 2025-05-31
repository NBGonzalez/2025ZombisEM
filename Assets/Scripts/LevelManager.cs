using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using Unity.Netcode.Components;

public enum GameMode
{
    Tiempo,
    Monedas
}

public class LevelManager : NetworkBehaviour
{
    #region Properties

    [Header("Prefabs")]
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private GameObject zombiePrefab;

    [Header("Team Settings")]
    [Tooltip("Número de jugadores humanos")]
    [SerializeField] private int numberOfHumans = 2;

    [Tooltip("Número de zombis")]
    [SerializeField] private int numberOfZombies = 2;

    [Header("Game Mode Settings")]
    [Tooltip("Selecciona el modo de juego")]
    [SerializeField] private GameMode gameMode;

    [Tooltip("Tiempo de partida en minutos para el modo tiempo")]
    [SerializeField] private int minutes = 5;

    public List<Vector3> humanSpawnPoints = new List<Vector3>();
    public List<Vector3> zombieSpawnPoints = new List<Vector3>();

    // Referencias a los elementos de texto en el canvas
    private TextMeshProUGUI humansText;
    private TextMeshProUGUI zombiesText;
    private TextMeshProUGUI gameModeText;

    public TextMeshProUGUI victoriaText;
    public TextMeshProUGUI victoriaParcialText;
    public TextMeshProUGUI derrotaText;

    private int CoinsGenerated = 0;

    public string PlayerPrefabName => playerPrefab.name;
    public string ZombiePrefabName => zombiePrefab.name;

    private UniqueIdGenerator uniqueIdGenerator;
    private LevelBuilder levelBuilder;

    private PlayerController playerController;
    public int totalPlayers;

    private float remainingSeconds;
    private bool isGameOver = false;

    public GameObject gameOverPanel; // Asigna el panel desde el inspector

    public GameManager gameManager;

    int collected;
    int total;

    #endregion

    #region Unity game loop methods

    private void Awake()
    {
        Debug.Log("Despertando el nivel");

        // Obtener la referencia al UniqueIDGenerator
        uniqueIdGenerator = GetComponent<UniqueIdGenerator>();

        // Obtener la referencia al LevelBuilder
        levelBuilder = GetComponent<LevelBuilder>();

        Time.timeScale = 1f; // Asegurarse de que el tiempo no esté detenido

        gameManager = GameManager.Instance; // Obtener la instancia del GameManager
    }

    private void Start()
    {
        collected = GameManager.Instance.TotalCoinsCollected.Value;
        total = GameManager.Instance.CoinsGenerated.Value;
        // Obtener el modo de juego desde GameManager
        if (gameManager.CurrentGameMode == "CoinGame")
        {
            gameMode = GameMode.Monedas;
        }
        else if (gameManager.CurrentGameMode == "TimeGame")
        {
            gameMode = GameMode.Tiempo;
        }

        minutes = gameManager.GetTime(); // Asignar el tiempo de partida desde GameManager

        // El GameManager tiene el numero de jugadores, saca cuentas para saber cuantos zombies y humanos spawnear despues.
        totalPlayers = gameManager.GetNumberOfPlayers();
        if (totalPlayers % 2 == 0)
        {
            numberOfHumans = totalPlayers / 2;
            numberOfZombies = totalPlayers / 2;
        }
        else
        {
            numberOfHumans = totalPlayers / 2; // Humanos restantes
            numberOfZombies = totalPlayers / 2 + 1; // Zombis restantes + 1 por ser impar
        }

        //Debug.Log("Iniciando el nivel");
        // Buscar el objeto "CanvasPlayer" en la escena
        GameObject canvas = GameObject.Find("CanvasPlayer");
        if (canvas != null)
        {
            //Debug.Log("Canvas encontrado");

            // Buscar el Panel dentro del CanvasHud
            Transform panel = canvas.transform.Find("PanelHud");
            if (panel != null)
            {
                // Buscar los TextMeshProUGUI llamados "HumansValue" y "ZombiesValue" dentro del Panel
                Transform humansTextTransform = panel.Find("HumansValue");
                Transform zombiesTextTransform = panel.Find("ZombiesValue");
                Transform gameModeTextTransform = panel.Find("GameModeConditionValue");

                if (humansTextTransform != null)
                {
                    humansText = humansTextTransform.GetComponent<TextMeshProUGUI>();
                }

                if (zombiesTextTransform != null)
                {
                    zombiesText = zombiesTextTransform.GetComponent<TextMeshProUGUI>();
                }

                if (gameModeTextTransform != null)
                {
                    gameModeText = gameModeTextTransform.GetComponent<TextMeshProUGUI>();
                }

                if (victoriaText == null)
                {
                    victoriaText = panel.Find("VictoriaText").GetComponent<TextMeshProUGUI>();
                    victoriaText.enabled = false; // Inicialmente oculto
                }

                if (victoriaParcialText == null)
                {
                    victoriaParcialText = panel.Find("VictoriaParcialText").GetComponent<TextMeshProUGUI>();
                    victoriaParcialText.enabled = false; // Inicialmente oculto
                }

                if (derrotaText == null)
                {
                    derrotaText = panel.Find("DerrotaText").GetComponent<TextMeshProUGUI>();
                    derrotaText.enabled = false; // Inicialmente oculto
                }
                
                gameModeText.text = $"{collected}/{total}";

            }
        }

        remainingSeconds = minutes * 60;

        // Obtener los puntos de aparición y el número de monedas generadas desde LevelBuilder
        if (levelBuilder != null)
        {
            levelBuilder.Build();
            humanSpawnPoints = levelBuilder.GetHumanSpawnPoints();
            zombieSpawnPoints = levelBuilder.GetZombieSpawnPoints();
            CoinsGenerated = levelBuilder.GetCoinsGenerated();
        }

        SpawnTeams();

        UpdateTeamUI();
    }

    private void Update()
    {
        if (gameMode == GameMode.Tiempo)
        {
            // Lógica para el modo de juego basado en tiempo
            HandleTimeLimitedGameMode();
        }
        else if (gameMode == GameMode.Monedas)
        {
            // Lógica para el modo de juego basado en monedas
            HandleCoinBasedGameMode();
        }

        if (Input.GetKeyDown(KeyCode.Z)) // Presiona "Z" para convertirte en Zombie
        {
            // Comprobar si el jugador actual está usando el prefab de humano
            GameObject currentPlayer = GameObject.FindGameObjectWithTag("Player");
            if (currentPlayer != null && currentPlayer.name.Contains(playerPrefab.name))
            {
                ChangeToZombie();
            }
            else
            {
                Debug.Log("El jugador actual no es un humano.");
            }
        }
        else if (Input.GetKeyDown(KeyCode.H)) // Presiona "H" para convertirte en Humano
        {
            // Comprobar si el jugador actual está usando el prefab de zombie
            GameObject currentPlayer = GameObject.FindGameObjectWithTag("Player");
            if (currentPlayer != null && currentPlayer.name.Contains(zombiePrefab.name))
            {
                ChangeToHuman();
            }
            else
            {
                Debug.Log("El jugador actual no es un zombie.");
            }
        }
        UpdateTeamUI();

        if (isGameOver)
        {
            ShowGameOverPanel();
        }
    }

    #endregion

    #region Team management methods

    private void ChangeToZombie()
    {
        GameObject currentPlayer = GameObject.FindGameObjectWithTag("Player");
        ChangeToZombie(currentPlayer, true);
    }

    public void ChangeToZombie(GameObject human, bool enabled)
    {
        Debug.Log("Cambiando a Zombie");

        if (human != null)
        {
            // Guardar la posición, rotación y uniqueID del humano actual
            Vector3 playerPosition = human.transform.position;
            Quaternion playerRotation = human.transform.rotation;
            string uniqueID = human.GetComponent<PlayerController>().uniqueID;

            ulong Id = human.GetComponent<NetworkObject>().OwnerClientId; // Obtener el ID del cliente propietario
            string playerName = GameManager.Instance.GetPlayerName(Id); // Obtener el nombre del jugador desde GameManager

            // Destruir el humano actual
            human.GetComponent<NetworkObject>().Despawn(); // Despawn para redirigir a los clientes
            //Destroy(human);

            // Instanciar el prefab del zombie en la misma posición y rotación
            GameObject zombie = Instantiate(zombiePrefab, playerPosition, playerRotation);

            zombie.GetComponent<NetworkObject>().SpawnAsPlayerObject(Id); // Asignar la propiedad al cliente propietario

            zombie.GetComponent<PlayerController>().OnNetworkSpawn(); // Asegurarse de que el PlayerController se inicialice correctamente
            zombie.GetComponent<PlayerController>().name = playerName; // Asignar el nombre del jugador


            if (enabled) { zombie.tag = "Player"; }

            // Obtener el componente PlayerController del zombie instanciado
            PlayerController playerController = zombie.GetComponent<PlayerController>();
            if (playerController != null)
            {
                playerController.enabled = enabled;
                playerController.isZombie = true; // Cambiar el estado a zombie
                playerController.uniqueID = uniqueID; // Mantener el identificador único
                playerController.convertido.Value = true; // Marcar como convertido a mitad de partida
                numberOfHumans--; // Reducir el número de humanos
                numberOfZombies++; // Aumentar el número de zombis

                if (numberOfHumans == 0)
                {
                    VictoriaZombiesRequestRpc(Id);
                }

                UpdateHumansZombiesClientRpc(numberOfHumans, numberOfZombies);
                UpdateTeamUI();

                if (enabled)
                {
                    // Obtener la referencia a la cámara principal
                    Camera mainCamera = Camera.main;

                    if (mainCamera != null)
                    {
                        // Obtener el script CameraController de la cámara principal
                        CameraController cameraController = mainCamera.GetComponent<CameraController>();

                        if (cameraController != null)
                        {
                            // Asignar el zombie al script CameraController
                            cameraController.player = zombie.transform;
                        }

                        // Asignar el transform de la cámara al PlayerController
                        playerController.cameraTransform = mainCamera.transform;
                    }
                    else
                    {
                        Debug.LogError("No se encontró la cámara principal.");
                    }
                }
            }
            else
            {
                Debug.LogError("PlayerController no encontrado en el zombie instanciado.");
            }
        }
        else
        {
            Debug.LogError("No se encontró el humano actual.");
        }
    }

    private void ChangeToHuman()
    {
        Debug.Log("Cambiando a Humano");

        // Obtener la referencia al jugador actual
        GameObject currentPlayer = GameObject.FindGameObjectWithTag("Player");

        if (currentPlayer != null)
        {
            // Guardar la posición y rotación del jugador actual
            Vector3 playerPosition = currentPlayer.transform.position;
            Quaternion playerRotation = currentPlayer.transform.rotation;

            // Destruir el jugador actual
            Destroy(currentPlayer);

            // Instanciar el prefab del humano en la misma posición y rotación
            GameObject human = Instantiate(playerPrefab, playerPosition, playerRotation);
            human.tag = "Player";

            // Obtener la referencia a la cámara principal
            Camera mainCamera = Camera.main;

            if (mainCamera != null)
            {
                // Obtener el script CameraController de la cámara principal
                CameraController cameraController = mainCamera.GetComponent<CameraController>();

                if (cameraController != null)
                {
                    // Asignar el humano al script CameraController
                    cameraController.player = human.transform;
                }

                // Obtener el componente PlayerController del humano instanciado
                playerController = human.GetComponent<PlayerController>();
                // Asignar el transform de la cámara al PlayerController
                if (playerController != null)
                {
                    playerController.enabled = true;
                    playerController.cameraTransform = mainCamera.transform;
                    playerController.isZombie = false; // Cambiar el estado a humano
                    numberOfHumans++; // Aumentar el número de humanos
                    numberOfZombies--; // Reducir el número de zombis
                    UpdateHumansZombiesClientRpc(numberOfHumans, numberOfZombies);
                    UpdateTeamUI();
                }
                else
                {
                    Debug.LogError("PlayerController no encontrado en el humano instanciado.");
                }
            }
            else
            {
                Debug.LogError("No se encontró la cámara principal.");
            }
        }
        else
        {
            Debug.LogError("No se encontró el jugador actual.");
        }
    }

    private void SpawnPlayer(Vector3 spawnPosition, GameObject prefab, ulong clientId)
    {

        //////////////////////////////// INTENTO DE SPAWN DE JUGADORES /////////////////////////


        //Debug.Log($"Instanciando jugador en {spawnPosition}");
        if (prefab != null)
        {
            Debug.Log($"Instanciando jugador en {spawnPosition}");
            // Crear una instancia del prefab en el punto especificado


            // Verifica si el jugador ya existe en el diccionario
            if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
            {
                // Si ya tiene un objeto de jugador, no hace nada
                if (client.PlayerObject != null)
                    return;
            }
            Debug.Log("Antes del Spawn: " + NetworkManager.Singleton.SpawnManager.SpawnedObjectsList.Count);
            // Instancia el nuevo jugador y lo asigna al cliente
            GameObject player = Instantiate(prefab, spawnPosition, Quaternion.identity);
            NetworkObject playerNetworkObject = player.GetComponent<NetworkObject>();
            playerNetworkObject.SpawnWithOwnership(clientId); // Asigna la propiedad al cliente

            player.GetComponent<PlayerController>().OnNetworkSpawn();
            //player.GetComponent<PlayerController>().networkName.Value = playerName; // Asigna el nombre del jugador
            Debug.Log("Despues del Spawn: " + NetworkManager.Singleton.SpawnManager.SpawnedObjectsList.Count);

            player.tag = "Player";

            // Obtener la referencia a la cámara principal
            Camera mainCamera = player.transform.GetChild(3).gameObject.GetComponent<Camera>();

            if (mainCamera != null)
            {
                // Obtener el script CameraController de la cámara principal
                CameraController cameraController = mainCamera.GetComponent<CameraController>();

                if (cameraController != null)
                {
                    //Debug.Log($"CameraController encontrado en la cámara principal.");
                    // Asignar el jugador al script CameraController
                    cameraController.player = player.transform;
                }

                //Debug.Log($"Cámara principal encontrada en {mainCamera}");
                // Obtener el componente PlayerController del jugador instanciado
                playerController = player.GetComponent<PlayerController>();
                // Asignar el transform de la cámara al PlayerController
                if (playerController != null)
                {
                    //Debug.Log($"PlayerController encontrado en el jugador instanciado.");
                    playerController.enabled = true;
                    playerController.cameraTransform = mainCamera.transform;
                    playerController.uniqueID = uniqueIdGenerator.GenerateUniqueID(); // Generar un identificador único

                }
                else
                {
                    Debug.LogError("PlayerController no encontrado en el jugador instanciado.");
                }
            }
            else
            {
                Debug.LogError("No se encontró la cámara principal.");
            }
        }
        else
        {
            Debug.LogError("Faltan referencias al prefab o al punto de aparición.");
        }

    }

    private void SpawnTeams()
    {
        //Debug.Log("Instanciando equipos");
        if (humanSpawnPoints.Count <= 0) { return; }

        int nJugadores = NetworkManager.Singleton.ConnectedClientsIds.Count;
        List<ulong> jugadores = NetworkManager.Singleton.ConnectedClientsIds.ToList();

        int totalZombis = nJugadores / 2;
        int totalHumanos = nJugadores / 2;

        if (nJugadores % 2 != 0) totalZombis++; // Si impar, un zombi extra

        System.Random rnd = new System.Random();
        jugadores = jugadores.OrderBy(x => rnd.Next()).ToList(); // Mezcla aleatoriamente la lista

        int zombisSpawneados = 0;
        int humanosSpawneados = 0;

        if (NetworkManager.Singleton.IsHost)
        {
            for (int i = 0; i < jugadores.Count; i++)
            {
                ulong clientId = jugadores[i];

                if (zombisSpawneados < totalZombis)
                {
                    SpawnPlayer(humanSpawnPoints[i], zombiePrefab, clientId);
                    zombisSpawneados++;
                }
                else if (humanosSpawneados < totalHumanos)
                {
                    SpawnPlayer(humanSpawnPoints[i], playerPrefab, clientId);
                    humanosSpawneados++;
                }
            }
        }



        //Debug.Log($"Personaje jugable instanciado en {humanSpawnPoints[0]}");

        for (int i = 1; i < numberOfHumans; i++)
        {
            if (i < humanSpawnPoints.Count)
            {
                //SpawnNonPlayableCharacter(playerPrefab, humanSpawnPoints[i]);
            }
        }

        for (int i = 0; i < numberOfZombies; i++)
        {
            if (i < zombieSpawnPoints.Count)
            {
                //SpawnNonPlayableCharacter(zombiePrefab, zombieSpawnPoints[i]);
            }
        }
    }

    private void SpawnNonPlayableCharacter(GameObject prefab, Vector3 spawnPosition)
    {
        if (prefab != null)
        {
            GameObject npc = Instantiate(prefab, spawnPosition, Quaternion.identity);
            // Desactivar el controlador del jugador en los NPCs
            var playerController = npc.GetComponent<PlayerController>();
            if (playerController != null)
            {
                playerController.enabled = false; // Desactivar el controlador del jugador
                playerController.uniqueID = uniqueIdGenerator.GenerateUniqueID(); // Asignar un identificador único
            }
            Debug.Log($"Personaje no jugable instanciado en {spawnPosition}");
        }
    }

    [ClientRpc]
    void UpdateHumansZombiesClientRpc(int humans, int zombies)
    {
        this.numberOfHumans = humans;
        this.numberOfZombies = zombies;
    }
    private void UpdateTeamUI()
    {
        if (humansText != null)
        {
            humansText.text = $"{numberOfHumans}";
        }

        if (zombiesText != null)
        {
            zombiesText.text = $"{numberOfZombies}";
        }
    }

    #endregion

    #region Modo de juego

    private void HandleTimeLimitedGameMode()
    {
        // Implementar la lógica para el modo de juego basado en tiempo
        if (isGameOver) return;

        // Decrementar remainingSeconds basado en Time.deltaTime
        remainingSeconds -= Time.deltaTime;

        // Comprobar si el tiempo ha llegado a cero
        if (remainingSeconds <= 0)
        {
            //isGameOver = true;
            var allPlayers = GameObject.FindGameObjectsWithTag("Player");
            
            if (allPlayers.Count() == GameManager.Instance.maxPlayers)
            {
                remainingSeconds = 0;
                VictoriaHumanosRequestRpc();
            }
        }

        // Convertir remainingSeconds a minutos y segundos
        int minutesRemaining = Mathf.FloorToInt(remainingSeconds / 60);
        int secondsRemaining = Mathf.FloorToInt(remainingSeconds % 60);

        // Actualizar el texto de la interfaz de usuario
        if (gameModeText != null)
        {
            gameModeText.text = $"{minutesRemaining:D2}:{secondsRemaining:D2}";
        }

    }

    private void HandleCoinBasedGameMode()
    {
        if (isGameOver) return;

        if (gameModeText != null)
        {
            collected = GameManager.Instance.TotalCoinsCollected.Value;
            total = GameManager.Instance.CoinsGenerated.Value;
            gameModeText.text = $"{collected}/{total}";

            if (collected >= total)
            {
                var allPlayers = GameObject.FindGameObjectsWithTag("Player");
                if(allPlayers.Count() == GameManager.Instance.maxPlayers)
                {
                    VictoriaHumanosRequestRpc();
                }
                //isGameOver = true;
                
            }
        }
    }

    private void ShowGameOverPanel()
    {
        if (gameOverPanel != null)
        {
            Time.timeScale = 0f;
            gameOverPanel.SetActive(true); // Muestra el panel de pausa

            // Gestión del cursor
            Cursor.lockState = CursorLockMode.None; // Desbloquea el cursor
            Cursor.visible = true; // Hace visible el cursor
        }
    }

    public void ReturnToMainMenu()
    {
        // Gestión del cursor
        //Cursor.lockState = CursorLockMode.Locked; // Bloquea el cursor
        //Cursor.visible = false; // Oculta el cursor
        GameManager.Instance.ResetGameRequestRpc(); // Resetea el GameManager
        if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer)
        {
            var allPlayers = GameObject.FindGameObjectsWithTag("Player");
            foreach (var player in allPlayers)
            {
                player.GetComponent<NetworkObject>().Despawn();
            }
            NetworkManager.Singleton.SceneManager.LoadScene("MenuScene", UnityEngine.SceneManagement.LoadSceneMode.Single);
        }

    }

    #endregion

    [Rpc(SendTo.ClientsAndHost)]
    public void VictoriaZombiesRequestRpc(ulong ID)
    {
        Debug.Log("Victoria de los zombis");
        var allPlayers = GameObject.FindGameObjectsWithTag("Player");
        foreach (var player in allPlayers)
        {
            if (player.GetComponent<NetworkObject>().IsOwner)
            {
                bool haSidoConvertido = player.GetComponent<PlayerController>().convertido.Value;

                if (player.GetComponent<NetworkObject>().OwnerClientId == ID)
                {
                    derrotaText.enabled = true; // Mostrar mensaje de derrota
                }
                else if (haSidoConvertido)
                {
                    victoriaParcialText.enabled = true; // Mostrar mensaje de victoria parcial
                }
                else
                {
                    victoriaText.enabled = true; // Mostrar mensaje de victoria
                }
                ShowGameOverPanel();
            }

        }
    }

    [Rpc(SendTo.ClientsAndHost)]
    public void VictoriaHumanosRequestRpc()
    {
        Debug.Log("Victoria de los humanos");
        var allPlayers = GameObject.FindGameObjectsWithTag("Player");
        foreach (var player in allPlayers)
        {
            if (player.GetComponent<NetworkObject>().IsOwner)
            {
                if (player.GetComponent<PlayerController>().isZombie)
                {
                    derrotaText.enabled = true; // Mostrar mensaje de derrota
                }
                else
                {
                    victoriaText.enabled = true; // Mostrar mensaje de victoria

                }
                ShowGameOverPanel();
            }

        }
    }

}
