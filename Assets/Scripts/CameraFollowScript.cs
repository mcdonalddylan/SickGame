using System.Collections;
using UnityEngine;

public class CameraFollowScript : MonoBehaviour
{
    [SerializeField]
    private Transform target;
    [SerializeField]
    private Vector3 offset = new Vector3(0, 1.5f, -10);
    [SerializeField]
    private float smoothSpeedFactor = 0.125f;

    private Vector3 velocity = Vector3.zero;

    // Called after every other update
    private void LateUpdate()
    {
        Vector3 desiredPosition = target.position + offset;
        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref velocity, smoothSpeedFactor);
    }

    public void ShakeCamera(float intensity)
    {
        StartCoroutine(ShakeCam(intensity));
    }

    private IEnumerator ShakeCam(float intensity)
    {
        Vector3 originalTransform = transform.position;
        transform.position = new Vector3(
            transform.position.x + (Random.Range(-3, 3) * intensity),
            transform.position.y + (Random.Range(-3, 3) * intensity),
            transform.position.z);
        yield return new WaitForSeconds(0.02f);
        transform.position = new Vector3(
            transform.position.x + (Random.Range(-2, 2) * intensity),
            transform.position.y + (Random.Range(-2, 2) * intensity),
            transform.position.z);
        yield return new WaitForSeconds(0.02f);
        transform.position = new Vector3(
            transform.position.x + (Random.Range(-1, 1) * intensity),
            transform.position.y + (Random.Range(-1, 1) * intensity),
            transform.position.z);
        yield return new WaitForSeconds(0.02f);
        transform.position = originalTransform;
    }
}
