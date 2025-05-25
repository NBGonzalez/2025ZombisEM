using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class NamePlayers : MonoBehaviour
{
    public GameObject camera;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        //transform.rotation = Quaternion.Euler(0, 0, 0);
        transform.rotation = Quaternion.LookRotation(transform.position - camera.transform.position);
        //this.transform.rotation.SetLookRotation(camera.transform.position);
    }
}
