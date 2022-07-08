using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CheckpointScript : MonoBehaviour
{
    [SerializeField]
    public int checkpointNumber = 0;
    public BoxCollider collider;
    public ParticleSystem checkpointBeam;
    public Light checkpointLight; 

    private void Awake()
    {
        checkpointBeam.gameObject.SetActive(false);
        checkpointLight.gameObject.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.tag.Equals("Player") && GameManager.playerCheckpoint != checkpointNumber)
        {
            GameManager.playerCheckpoint = checkpointNumber;

            // Turn on particle effect for this checkpoint and turn off effect for other checkpoints
            CheckpointScript[] checkpoints = FindObjectsOfType<CheckpointScript>();
            foreach(CheckpointScript cp in checkpoints)
            {
                if (cp.checkpointNumber != GameManager.playerCheckpoint)
                {
                    cp.checkpointBeam.gameObject.SetActive(false);
                    cp.checkpointLight.gameObject.SetActive(false);
                }
            }

            checkpointBeam.gameObject.SetActive(true);
            checkpointBeam.Play();

            FadeInLight();
        }
    }

    private void FadeInLight()
    {
        StartCoroutine(FadeInLight(0.8f));
    }

    private IEnumerator FadeInLight(float duration)
    {
        checkpointLight.gameObject.SetActive(true);
        for (float i = 0; i < 1; i += Time.deltaTime / duration)
        {
            checkpointLight.intensity = Mathf.Lerp(0, 1, i);
            yield return null;
        }
        checkpointLight.intensity = 1;
    }
}
