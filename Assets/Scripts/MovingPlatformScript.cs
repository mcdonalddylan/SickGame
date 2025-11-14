using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovingPlatformScript : RaycastControllerScript
{
    public GameObject[] waypoints;
    public LayerMask passengerMask;
    public Vector3 move;

    private int currentWaypointIndex = 0;
    public float speedBetweenPoints = 3.5f;

    public override void Start()
    {
        base.Start();
        CalculateRaySpacing();
    }

    void Update()
    {
        UpdateRaycastOrigins();

        if (Vector3.Distance(transform.position, waypoints[currentWaypointIndex].transform.position) < 0.1f)
        {
            currentWaypointIndex++;
            if (currentWaypointIndex >= waypoints.Length)
            {
                currentWaypointIndex = 0;
            }
        }
        //transform.position = Vector3.MoveTowards(transform.position, waypoints[currentWaypointIndex].transform.position, (speedBetweenPoints * Time.deltaTime) * GameManager.timeScale);
        Vector3 velocity = move * Time.deltaTime;
        transform.Translate(velocity);
        MovePassengers(velocity);
    }

    void MovePassengers(Vector3 velocity)
    {
        HashSet<Transform> movedPassengers = new HashSet<Transform>();
        float directionX = Mathf.Sign(velocity.x);
        float directionY = Mathf.Sign(velocity.y);

        

        if (directionY == 1) // If platform is going up
        {
            float rayLength = Mathf.Abs(velocity.y) + SKIN_WIDTH;

            for (int i = 0; i < verticalRayCount; i++)
            {
                Vector2 rayOrigin = raycastOrigins.topLeft + Vector2.right * (verticalRaySpacing * i + velocity.x);
                RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.up, rayLength, passengerMask);
                Debug.DrawRay(rayOrigin, Vector2.up, Color.red);

                if (hit && !movedPassengers.Contains(hit.transform))
                {
                    movedPassengers.Add(hit.transform);
                    bool playersFacingRight = hit.transform.gameObject.GetComponent<PlayerControllerScript>().faceRightState;
                    //print("players X Direction: " + playersFacingRight);
                    float pushY = velocity.y - (hit.distance - SKIN_WIDTH) * directionY;
                    float pushX = playersFacingRight ? velocity.x : -velocity.x;
                    //print("platform push x: " + pushX + " | y: " + pushY);

                    hit.transform.Translate(new Vector3(pushX, pushY));
                }
            }
        }
        else if (directionY == -1) // If platform is heading down
        {
            float rayLength = SKIN_WIDTH * 2;

            for (int i = 0; i < verticalRayCount; i++)
            {
                Vector2 rayOrigin = raycastOrigins.topLeft + Vector2.right * (verticalRaySpacing * i + velocity.x);
                RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.up, rayLength, passengerMask);
                Debug.DrawRay(rayOrigin, Vector2.up, Color.red);

                if (hit && !movedPassengers.Contains(hit.transform))
                {
                    movedPassengers.Add(hit.transform);
                    bool playersFacingRight = hit.transform.gameObject.GetComponent<PlayerControllerScript>().faceRightState;
                    //print("players X Direction: " + playersFacingRight);
                    float pushY = velocity.y;
                    float pushX = playersFacingRight ? velocity.x : -velocity.x;
                    //print("platform push x: " + pushX + " | y: " + pushY);

                    hit.transform.Translate(new Vector3(pushX, pushY));
                }
            }
        }
        print("moved Passengers: " + movedPassengers.Count);
    }
}
