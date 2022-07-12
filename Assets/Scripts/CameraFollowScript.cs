using System.Collections;
using UnityEngine;

public class CameraFollowScript : MonoBehaviour
{
    private Vector3 FACE_RIGHT_OFFSET = new Vector3(3.5f, 1.5f, -10);
    private Vector3 FACE_LEFT_OFFSET = new Vector3(-3.5f, 1.5f, -10);
    [SerializeField]
    private Transform playerTarget;
    [SerializeField]
    private Vector3 offset;
    [SerializeField]
    private float smoothSpeedFactor = 0.125f;

    public bool isFollowingPlayer = true;

    private Vector3 velocity = Vector3.zero;

    public GameObject temporaryTarget;

    // Called after every other update
    private void LateUpdate()
    {
        if (isFollowingPlayer)
        {
            if (playerTarget.gameObject.GetComponent<PlayerControllerScript>().faceRightState)
            {
                offset = FACE_RIGHT_OFFSET;
            }
            else
            {
                offset = FACE_LEFT_OFFSET;
            }
            Vector3 desiredPosition = playerTarget.position + offset;
            transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref velocity, smoothSpeedFactor);
        }
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
