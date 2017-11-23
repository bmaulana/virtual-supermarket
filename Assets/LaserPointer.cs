using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LaserPointer : MonoBehaviour {

    // track controller
    private SteamVR_TrackedObject trackedObj;
    private SteamVR_Controller.Device Controller
    {
        get { return SteamVR_Controller.Input((int)trackedObj.index); }
    }

    // laser
    public GameObject laserPrefab;
    private GameObject laser;
    private Transform laserTransform;
    private Vector3 hitPoint;
    public Material redLaser;
    public Material blueLaser;
    public Material greenLaser;

    // detect & grab objects
    private GameObject hitObject;
    private GameObject grabbedObject;
    private float distance;

    // teleport
    public Transform cameraRigTransform;
    public GameObject teleportReticlePrefab;
    private GameObject reticle;
    private Transform teleportReticleTransform;
    public Transform headTransform;
    public Vector3 teleportReticleOffset;
    private bool shouldTeleport;

    void Awake()
    {
        trackedObj = GetComponent<SteamVR_TrackedObject>();
    }

    private void ShowLaser(RaycastHit hit)
    {
        laser.SetActive(true);
        laserTransform.position = Vector3.Lerp(trackedObj.transform.position, hitPoint, .5f);
        laserTransform.LookAt(hitPoint);
        laserTransform.localScale = new Vector3(laserTransform.localScale.x, laserTransform.localScale.y, hit.distance);
    }

    // Use this for initialization
    void Start ()
    {
        laser = Instantiate(laserPrefab);
        laserTransform = laser.transform;
        reticle = Instantiate(teleportReticlePrefab);
        teleportReticleTransform = reticle.transform;
    }

    // Update is called once per frame
    void Update ()
    {
        RaycastHit hit;
        if (Physics.Raycast(trackedObj.transform.position, transform.forward, out hit, 1000))
        {
            hitPoint = hit.point;
            ShowLaser(hit);

            if (hitPoint.y < 0.1f)
            {
                laser.GetComponent<MeshRenderer>().material = greenLaser;
                reticle.SetActive(true);
                teleportReticleTransform.position = hitPoint + teleportReticleOffset;
                shouldTeleport = true;
            }
            else
            {
                reticle.SetActive(false);
                laser.GetComponent<MeshRenderer>().material = redLaser;
                shouldTeleport = false;
            }
        }

        if (Physics.Raycast(trackedObj.transform.position, transform.forward, out hit, 1000, LayerMask.GetMask("Grabbable")))
        {
            // TODO unhighlight previous object
            hitObject = hit.transform.gameObject;
            // TODO highlight object
            laser.GetComponent<MeshRenderer>().material = blueLaser;
        }
        else
        {
            // TODO unhighlight object
            hitObject = null;
        }

        if (hitObject && Controller.GetPressDown(SteamVR_Controller.ButtonMask.Grip))
        {
            grabbedObject = hitObject;
            distance = hit.distance;
        }
        if (grabbedObject && Controller.GetPress(SteamVR_Controller.ButtonMask.Grip))
        {
            distance = System.Math.Max(0.1f, distance * 0.95f);
            grabbedObject.transform.position = trackedObj.transform.position + (transform.forward * distance);
            laser.GetComponent<MeshRenderer>().material = blueLaser;
        }
        if (grabbedObject && Controller.GetPressUp(SteamVR_Controller.ButtonMask.Grip))
        {
            grabbedObject = null;
        }

        if (Controller.GetHairTriggerUp() && shouldTeleport)
        {
            Teleport();
        }
    }

    private void Teleport()
    {
        shouldTeleport = false;
        reticle.SetActive(false);
        Vector3 difference = cameraRigTransform.position - headTransform.position;
        difference.y = 0;
        cameraRigTransform.position = hitPoint + difference;
    }
}
