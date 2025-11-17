using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovingPlatformScript : MonoBehaviour
{
    public GameObject[] waypoints;
    private Vector3 lastPosition;
    private List<Collider2D> riders = new List<Collider2D>();
    private BoxCollider2D solidCollider = null;
    private BoxCollider2D collisionCheckTrigger = null;
    private Vector3 move;

    private int currentWaypointIndex = 0;
    public float speedBetweenPoints = 3.5f;

    void Awake()
    {
        lastPosition = transform.position;

        solidCollider = gameObject.GetComponent<BoxCollider2D>();
        collisionCheckTrigger = gameObject.AddComponent<BoxCollider2D>();
        collisionCheckTrigger.size = new Vector3(solidCollider.size.x, solidCollider.size.y * 1.1f);
        collisionCheckTrigger.offset = solidCollider.offset;
        collisionCheckTrigger.isTrigger = true;
    }

    void FixedUpdate()
    {

        if (Vector3.Distance(transform.position, waypoints[currentWaypointIndex].transform.position) < 0.1f)
        {
            currentWaypointIndex++;
            if (currentWaypointIndex >= waypoints.Length)
            {
                currentWaypointIndex = 0;
            }
        }
        transform.position = Vector3.MoveTowards(transform.position, waypoints[currentWaypointIndex].transform.position, (speedBetweenPoints * Time.deltaTime) * GameManager.timeScale * GameManager.nonPlayerTimeScale);
        //MovePassengers();
    }

    private void MovePassengers()
    {
        Vector3 delta = transform.position - lastPosition;
        lastPosition = transform.position;

        foreach (var rider in riders)
        {
            if (rider != null)
            {
                var rb = rider.attachedRigidbody;
                if (rb != null)
                {
                    rb.MovePosition(rb.position + (Vector2)delta);
                }
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            riders.Add(other);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            riders.Remove(other);
        }
    }
}
