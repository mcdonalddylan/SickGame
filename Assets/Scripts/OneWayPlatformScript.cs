using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OneWayPlatformScript : MonoBehaviour
{
    public BoxCollider solidCollider = null;
    private new BoxCollider collisionCheckTrigger = null;
    [SerializeField]
    private Vector3 entryDirection = Vector3.up;

    void Awake()
    {
        //entryDirection = transform.TransformDirection(entryDirection);
        solidCollider = gameObject.GetComponent<BoxCollider>();
        solidCollider.isTrigger = false;

        collisionCheckTrigger = gameObject.AddComponent<BoxCollider>();
        collisionCheckTrigger.size = new Vector3(solidCollider.size.x * 1.5f, solidCollider.size.y * 2f, solidCollider.size.z * 1.5f);
        collisionCheckTrigger.center = solidCollider.center;
        collisionCheckTrigger.isTrigger = true;
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.tag.Equals("Player") && Physics.ComputePenetration(
            collisionCheckTrigger,
            transform.position,
            transform.rotation,
            other,
            other.transform.position,
            other.transform.rotation,
            out Vector3 collisionDirection,
            out float penetrationDepth  //nneded by this function but goes unused
            ))
        {
            print("player entry direction: " + collisionDirection);
            float dotTop = Vector3.Dot(entryDirection, collisionDirection);
            float dotLeft = Vector3.Dot(Vector3.left, collisionDirection);
            float dotRight = Vector3.Dot(Vector3.right, collisionDirection);
            // opposite direction passing is not allowed
            if (dotTop < 0 || dotLeft < 0 || dotRight < 0)
            {
                Physics.IgnoreCollision(GetComponent<Collider>(), other, false);
            }
            else
            {
                Physics.IgnoreCollision(GetComponent<Collider>(), other, true);
            }
        }
    }


}
