using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OneWayPlatformScript : MonoBehaviour
{
    private BoxCollider2D solidCollider = null;
    private new BoxCollider2D collisionCheckTrigger = null;
    [SerializeField]
    private Vector3 entryDirection = Vector3.up;

    // void Awake()
    // {
    //    //entryDirection = transform.TransformDirection(entryDirection);
    //    solidCollider = gameObject.GetComponent<BoxCollider2D>();
    //    solidCollider.isTrigger = true;

    //    collisionCheckTrigger = gameObject.AddComponent<BoxCollider2D>();
    //    collisionCheckTrigger.size = new Vector3(solidCollider.size.x, solidCollider.size.y * 1.1f);
    //    collisionCheckTrigger.offset = solidCollider.offset;
    //    collisionCheckTrigger.isTrigger = true;
    // }

    // private void OnTriggerStay(Collider other)
    // {
    //    if (other.tag.Equals("Player") && Physics.ComputePenetration(
    //        collisionCheckTrigger,
    //        transform.position,
    //        transform.rotation,
    //        other,
    //        other.transform.position,
    //        other.transform.rotation,
    //        out Vector3 collisionDirection,
    //        out float penetrationDepth  //nneded by this function but goes unused
    //        ))
    //    {
    //        print("player entry direction: " + collisionDirection);
    //        float dotTop = Vector3.Dot(entryDirection, collisionDirection);
    //        float dotLeft = Vector3.Dot(Vector3.left, collisionDirection);
    //        float dotRight = Vector3.Dot(Vector3.right, collisionDirection);
    //        // opposite direction passing is not allowed
    //        if (dotTop < 0 || dotLeft < 0 || dotRight < 0)
    //        {
    //            Physics.IgnoreCollision(GetComponent<Collider>(), other, false);
    //        }
    //        else
    //        {
    //            Physics.IgnoreCollision(GetComponent<Collider>(), other, true);
    //        }
    //    }
    // }
}
