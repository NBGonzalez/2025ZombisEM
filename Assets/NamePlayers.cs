using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.UIElements;

public class NamePlayers : MonoBehaviour
{
    public GameObject camera;
    public GameObject playerOwner;
    public List<GameObject> playersNames = new List<GameObject>();

    private Vector3 worldUp;


    // Start is called before the first frame update
    void Start()
    {

        var allPlayers = GameObject.FindGameObjectsWithTag("Player");
        foreach (var player in allPlayers)
        {
            if (player.GetComponent<NetworkObject>().IsOwner)
            {
                playerOwner = player;
                camera = playerOwner.transform.GetChild(3).gameObject;
                playersNames.Add(player.transform.GetChild(4).gameObject);
            }
            else
            {
                playersNames.Add(player.transform.GetChild(4).gameObject);
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        foreach (var playerName in playersNames)
        {
            playerName.transform.LookAt(camera.transform, worldUp);
            playerName.transform.rotation = Quaternion.Euler(0, playerName.transform.rotation.eulerAngles.y, 0);
        }
    }
}
