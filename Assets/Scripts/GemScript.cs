using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GemScript : MonoBehaviour
{
    public SphereCollider collider;
    public GameObject particleEffect;
    public Light gemPointLight;

    private void Start()
    {
        if (!GameManager.particleEffectsEnabled)
        {
            gemPointLight.gameObject.SetActive(false);
        }
        StartCoroutine(AnimateGems(0.4f));    
    }

    private IEnumerator AnimateGems(float duration)
    {
        Vector3 originalPos = gameObject.transform.position;
        Vector3 adjustedPos = new Vector3(originalPos.x, originalPos.y + 0.4f, originalPos.z);
        while (true)
        {
            for(float i = 0; i < 1; i += (Time.deltaTime / duration) * GameManager.timeScale)
            {
                gameObject.transform.position = Vector3.Lerp(originalPos, adjustedPos, Mathf.SmoothStep(0.0f, 1.0f, i));
                yield return null;
            }
            gameObject.transform.position = adjustedPos;

            for (float i = 0; i < 1; i += (Time.deltaTime / duration) * GameManager.timeScale)
            {
                gameObject.transform.position = Vector3.Lerp(adjustedPos, originalPos, Mathf.SmoothStep(0.0f, 1.0f, i));
                yield return null;
            }
            gameObject.transform.position = originalPos;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.tag.Equals("Player"))
        {
            collider.enabled = false;
            GameManager.gemsCount += 1;
            //CameraFollowScript camScript = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<CameraFollowScript>();
            //camScript.ShakeCamera(0.00001f);
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
