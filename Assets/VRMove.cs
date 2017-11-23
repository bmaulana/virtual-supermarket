using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

public class VRMove : MonoBehaviour {

    private SteamVR_TrackedObject trackedObj;
    private SteamVR_Controller.Device Controller
    {
        get { return SteamVR_Controller.Input((int)trackedObj.index); }
    }
    public GameObject player;
    private Vector3 playerPos;

    void Awake()
    {
        trackedObj = GetComponent<SteamVR_TrackedObject>();
    }

    // Update is called once per frame
    void Update ()
    {
        //If finger is on touchpad
        if (Controller.GetTouch(SteamVR_Controller.ButtonMask.Touchpad))
        {
            //Read the touchpad values
            Vector2 touchpad = Controller.GetAxis(EVRButtonId.k_EButton_SteamVR_Touchpad);

            // Handle movement via touchpad
            if (touchpad.y > 0.2f || touchpad.y < -0.2f)
            {
                // Move Forward
                player.transform.position -= player.transform.forward * Time.deltaTime * (touchpad.y * 5f);

                // Adjust height to terrain height at player position
                playerPos = player.transform.position;
                playerPos.y = Terrain.activeTerrain.SampleHeight(player.transform.position);
                player.transform.position = playerPos;
            }

            // handle rotation via touchpad
            if (touchpad.x > 0.3f || touchpad.x < -0.3f)
            {
                player.transform.Rotate(0, touchpad.x * 1.5f, 0);
            }
        }
    }
}
