using TMPro;
using UnityEngine;
using Unity.Netcode;
using Unity.VisualScripting;
using System;
using Unity.Collections;
using System.Runtime.ConstrainedExecution;
using UnityEngine.UIElements;
using System.Collections.Generic;

public class PlayerController : NetworkBehaviour
{

    private TextMeshProUGUI coinText;

    [Header("Stats")]
    public int CoinsCollected = 0;

    [Header("Character settings")]
    public bool isZombie = false; // Añadir una propiedad para el estado del jugador
    public string uniqueID; // Añadir una propiedad para el identificador único

    [Header("Movement Settings")]
    public float moveSpeed = 5f;           // Velocidad de movimiento
    public float zombieSpeedModifier = 0.8f; // Modificador de velocidad para zombies
    public Animator animator;              // Referencia al Animator
    public Transform cameraTransform;      // Referencia a la cámara

    private float horizontalInput;         // Entrada horizontal (A/D o flechas)
    private float verticalInput;           // Entrada vertical (W/S o flechas)

    // Nombre del jugador
    public NetworkVariable<FixedString64Bytes> networkName = new(writePerm: NetworkVariableWritePermission.Owner,
                                                                 readPerm: NetworkVariableReadPermission.Everyone);
    int totalCollected;
    public string name;
    public GameObject textName;

    public NetworkVariable<bool> convertido = new(writePerm: NetworkVariableWritePermission.Server, readPerm: NetworkVariableReadPermission.Everyone);
    //public bool convertido = false; // Para saber si el jugador ha sido convertido en zombie a mitad de la partida

    public Dictionary<ulong, string> allNetworkNames = new Dictionary<ulong, string>();

    void Start()
    {
        
        // Buscar el objeto "CanvasPlayer" en la escena
        GameObject canvas = GameObject.Find("CanvasPlayer");

        totalCollected = GameManager.Instance.TotalCoinsCollected.Value;

        //Esto es para el nombre
        //textName.GetComponent<TextMeshPro>().text = networkName.Value.ToString();
        networkName.OnValueChanged += NameChange;


        if (!IsOwner)
        {
            this.GetComponent<PlayerController>().enabled = false;
        }

        if (canvas != null)
        {
            //Debug.Log("Canvas encontrado");

            // Buscar el Panel dentro del CanvasHud
            Transform panel = canvas.transform.Find("PanelHud");
            if (panel != null)
            {
                // Buscar el TextMeshProUGUI llamado "CoinsValue" dentro del Panel
                Transform coinTextTransform = panel.Find("CoinsValue");
                if (coinTextTransform != null)
                {
                    coinText = coinTextTransform.GetComponent<TextMeshProUGUI>();
                }
            }
        }

        UpdateCoinUI();
    }
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        //Debug.Log("Mi nombre es: ------------------ " + GameManager.Instance.networkPlayerNames[this.GetComponent<NetworkObject>().OwnerClientId]);
        //name = GameManager.Instance.networkPlayerNames[this.GetComponent<NetworkObject>().OwnerClientId];
        name = GameManager.Instance.GetPlayerName(this.GetComponent<NetworkObject>().OwnerClientId); // Obtener el nombre del jugador desde el GameManager
        networkName.Value = name; // Asignar el nombre al NetworkVariable
        this.transform.GetChild(4).GetChild(0).GetComponent<TextMeshProUGUI>().text = name;
        Debug.Log("Estoy en OnNetworkSpawn, mi nombre es: " + name + " y mi ID " + this.GetComponent<NetworkObject>().OwnerClientId);
        
    }




    public void NameChange(FixedString64Bytes previousValue, FixedString64Bytes newValue)
    {
        name = newValue.ToString();
        networkName.Value = name;
        this.gameObject.transform.GetChild(4).GetChild(0).GetComponent<TextMeshProUGUI>().text = newValue.ToString();
    }

    void Update()
    {
        if (!IsSpawned) return;

        // Leer entrada del teclado
        horizontalInput = Input.GetAxis("Horizontal");
        verticalInput = Input.GetAxis("Vertical");

        // Mover el jugador
        MovePlayer(horizontalInput, verticalInput);

        // Manejar las animaciones del jugador
        HandleAnimationsRequestRpc(horizontalInput, verticalInput);
    }


    void MovePlayer(float horizontalInput, float verticalInput)
    {
        //if(cameraTransform == null) { return; }
        //if (cameraTransform == null && IsOwner)
        //{
        //    GameObject camara = new GameObject();

        //    Console.WriteLine("Camara creada");

        //    camara.AddComponent<Camera>();
        //    camara.AddComponent<CameraController>();

        //    camara.GetComponent<CameraController>().player = this.transform;
        //    camara.GetComponent<CameraController>().offset = this.transform.position + new Vector3(0, 2, -5);
        //    cameraTransform = camara.transform;

        //    if (!IsOwner)
        //    {
        //        camara.SetActive(false);
        //    }
        //}


        // Calcular la dirección de movimiento en relación a la cámara
        Vector3 moveDirection = (cameraTransform.forward * verticalInput + cameraTransform.right * horizontalInput).normalized;
        moveDirection.y = 0f; // Asegurarnos de que el movimiento es horizontal (sin componente Y)

        // Mover el jugador usando el Transform
        if (moveDirection != Vector3.zero)
        {
            // Calcular la rotación en Y basada en la dirección del movimiento
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, 720f * Time.deltaTime);

            // Ajustar la velocidad si es zombie
            float adjustedSpeed = isZombie ? moveSpeed * zombieSpeedModifier : moveSpeed;

            transform.Translate(moveDirection * adjustedSpeed * Time.deltaTime, Space.World);
            // Mover al jugador en la dirección deseada
            MoverPersonajeRequestRpc(this.transform.position, this.transform.rotation);
        }

    }

    [Rpc(SendTo.ClientsAndHost)]
    void MoverPersonajeRequestRpc(Vector3 pjTransform, Quaternion pjRotation)
    {
        this.transform.position = pjTransform;
        this.transform.rotation = pjRotation;
    }

    [Rpc(SendTo.ClientsAndHost)]
    void HandleAnimationsRequestRpc(float horizontalInput, float verticalInput)
    {
        // Animaciones basadas en la dirección del movimiento
        animator.SetFloat("Speed", Mathf.Abs(horizontalInput) + Mathf.Abs(verticalInput));  // Controla el movimiento (caminar/correr)
    }

    public void CoinCollected()
    {
        if (!isZombie) // Solo los humanos pueden recoger monedas
        {
            if (IsOwner)
            {
                this.CoinsCollected++;
                UpdateCoinUI();
            }
            if (IsServer)
            {
                GameManager.Instance.TotalCoinsCollected.Value++;

            }
        }
    }

    void UpdateCoinUI()
    {
        if (coinText != null)
        {
            coinText.text = $"{CoinsCollected}";
        }
    }
}