using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.UIElements;

public class NamePlayers : MonoBehaviour
{
    public GameObject camera;
    public GameObject playerOwner;


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
                break;
            }
        }
    }

    // Update is called once per frame
    void Update()
    {

        //transform.rotation = Quaternion.LookRotation(this.transform.position - camera.transform.position);
    }
}
