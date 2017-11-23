using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LaserPointerNoVR : MonoBehaviour
{
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

    private void ShowLaser(RaycastHit hit)
    {
        laser.SetActive(true);
        laserTransform.position = Vector3.Lerp(transform.position, hitPoint, .5f);
        laserTransform.LookAt(hitPoint);
        laserTransform.localScale = new Vector3(laserTransform.localScale.x, laserTransform.localScale.y, hit.distance);
    }

    // Use this for initialization
    void Start()
    {
        laser = Instantiate(laserPrefab);
        laserTransform = laser.transform;
        reticle = Instantiate(teleportReticlePrefab);
        teleportReticleTransform = reticle.transform;
    }

    // Update is called once per frame
    void Update()
    {
        var camera = GetComponentInChildren<Camera>();
        var ray = camera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 1000))
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

        if (Physics.Raycast(ray, out hit, 1000, LayerMask.GetMask("Grabbable")))
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

        if (hitObject && Input.GetMouseButtonDown(0))
        {
            grabbedObject = hitObject;
            distance = hit.distance;
        }
        if (grabbedObject && Input.GetMouseButton(0))
        {
            distance = System.Math.Max(0.5f, distance * 0.95f);
            grabbedObject.transform.position = ray.origin + (ray.direction * distance);
            laser.GetComponent<MeshRenderer>().material = blueLaser;
        }
        if (grabbedObject && Input.GetMouseButtonUp(0))
        {
            grabbedObject = null;
        }

        if (Input.GetMouseButtonUp(1) && shouldTeleport)
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
