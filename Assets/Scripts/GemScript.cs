using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GemScript : MonoBehaviour
{
    public SphereCollider collider;
    public GameObject particleEffect;

    private void OnTriggerEnter(Collider other)
    {
        if (other.tag.Equals("Player"))
        {
            collider.enabled = false;
            GameManager.gemsCount += 1;
            //CameraFollowScript camScript = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<CameraFollowScript>();
            //camScript.ShakeCamera(0.2f);
            GameUIScript gameUI = GameObject.FindGameObjectWithTag("InGameUI").GetComponent<GameUIScript>();
            gameUI.UpdateGemsCountOnUI();

            // replace self with particle effect and then destroy self after particle effect
            if (GameManager.particleEffectsEnabled)
            {
                Instantiate(particleEffect, gameObject.transform.position, Quaternion.identity);
            }
            Destroy(gameObject);
        }
    }
}
