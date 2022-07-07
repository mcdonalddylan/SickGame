using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CheckpointScript : MonoBehaviour
{
    [SerializeField]
    public int checkpointNumber = 0;
    public BoxCollider collider;
    public ParticleSystem checkPointBeam;

    private void Awake()
    {
        checkPointBeam.gameObject.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.tag.Equals("Player"))
        {
            GameManager.playerCheckpoint = checkpointNumber;

            // Turn on particle effect for this checkpoint and turn off effect for other checkpoints
            CheckpointScript[] checkpoints = FindObjectsOfType<CheckpointScript>();
            foreach(CheckpointScript cp in checkpoints)
            {
                if (cp.checkpointNumber != GameManager.playerCheckpoint)
                {
                    cp.checkPointBeam.gameObject.SetActive(false);
                }
            }

            checkPointBeam.gameObject.SetActive(true);
            checkPointBeam.Play();
        }
    }
}
