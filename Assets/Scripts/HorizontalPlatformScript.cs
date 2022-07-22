using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HorizontalPlatformScript : MonoBehaviour
{
    public GameObject[] waypoints;
    private int currentWaypointIndex = 0;
    public float speedBetweenPoints = 3.5f;
    private new BoxCollider collisionCheckTrigger = null;
    private Vector3 movementDelta = Vector3.zero;

    private void Awake()
    {
        BoxCollider solidCollider = GetComponent<BoxCollider>();
        collisionCheckTrigger = gameObject.AddComponent<BoxCollider>();
        collisionCheckTrigger.size = new Vector3(1f, solidCollider.size.y * 1.5f, 1f);
        collisionCheckTrigger.center = solidCollider.center;
        collisionCheckTrigger.isTrigger = true;
        //movementDelta = transform.position;
    }

    void Update()
    {
        if (Vector3.Distance(transform.position, waypoints[currentWaypointIndex].transform.position) < 0.1f)
        {
            currentWaypointIndex++;
            if (currentWaypointIndex >= waypoints.Length)
            {
                currentWaypointIndex = 0;
            }
        }
        transform.position = Vector3.MoveTowards(transform.position, waypoints[currentWaypointIndex].transform.position, (speedBetweenPoints * Time.deltaTime) * GameManager.timeScale);
        //movementDelta = transform.position - movementDelta;
    }

    private void OnTriggerEnter(Collider collision)
    {
        print("who entered collision: " + collision.tag);
        if (collision.tag.Equals("Player"))
        {
            print("player entered collision");
            //collision.gameObject.transform.position = transform.position + movementDelta;
            //collision.gameObject.transform.position = transform.position;
            //collision.gameObject.transform.SetParent(transform, true);
        }
    }

    private void OnTriggerExit(Collider collision)
    {
        print("who entered collision: " + collision.tag);
        if (collision.tag.Equals("Player"))
        {
            print("player exited collision");
            //collision.gameObject.transform.position = transform.position + movementDelta;
            //collision.gameObject.transform.position = transform.position;
            //collision.gameObject.transform.SetParent(null);
        }
    }
}
