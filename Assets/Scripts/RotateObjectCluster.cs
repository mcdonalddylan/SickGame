using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotateObjectCluster : MonoBehaviour
{
    public float timeScale = 1.0f;
    void Update()
    {
        Time.timeScale = timeScale;
        this.gameObject.transform.Rotate(0f, 0.01f * Time.timeScale, 0f);
    }
}
