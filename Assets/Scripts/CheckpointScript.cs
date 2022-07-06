using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CheckpointScript : MonoBehaviour
{
    [SerializeField]
    public int checkpointNumber = 0;
    public BoxCollider collider;

    private void OnTriggerEnter(Collider other)
    {
        if (other.tag.Equals("Player") && GameManager.playerCheckpoint != checkpointNumber)
        {
            GameManager.playerCheckpoint = checkpointNumber;
        }
    }
}
