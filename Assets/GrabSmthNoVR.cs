using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrabSmthNoVR : MonoBehaviour
{
    private GameObject collidingObject;
    private GameObject objectInHand;

    private void SetCollidingObject(Collider col)
    {
        if (collidingObject || !col.GetComponent<Rigidbody>())
        {
            return;
        }
        collidingObject = col.gameObject;
    }

    public void OnCollisionEnter(Collision col)
    {
        SetCollidingObject(col.collider);
        ToggleHighlight(true, collidingObject);
    }

    public void OnCollisionStay(Collision col)
    {
        SetCollidingObject(col.collider);
        ToggleHighlight(true, collidingObject);
    }

    public void OnCollisionExit(Collision col)
    {
        if (!collidingObject)
        {
            return;
        }
        ToggleHighlight(false, collidingObject);
        collidingObject = null;
    }

    private void GrabObject()
    {
        objectInHand = collidingObject;
        collidingObject = null;
        var joint = AddFixedJoint();
        joint.connectedBody = objectInHand.GetComponent<Rigidbody>();
    }

    private FixedJoint AddFixedJoint()
    {
        FixedJoint fx = gameObject.AddComponent<FixedJoint>();
        fx.breakForce = 20000;
        fx.breakTorque = 20000;
        return fx;
    }

    private void ReleaseObject()
    {
        if (GetComponent<FixedJoint>())
        {
            GetComponent<FixedJoint>().connectedBody = null;
            Destroy(GetComponent<FixedJoint>());
        }
        objectInHand = null;
    }

    private void ToggleHighlight(bool highlight, GameObject obj)
    {
        // TODO Wait for Ajia's script/material
        if (highlight)
        {

        }
        else
        {

        }
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (collidingObject)
            {
                GrabObject();
            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            if (objectInHand)
            {
                ReleaseObject();
            }
        }
    }
}
