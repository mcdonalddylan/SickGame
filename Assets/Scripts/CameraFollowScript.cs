using System.Collections;
using UnityEngine;

public class CameraFollowScript : MonoBehaviour
{
    private Vector3 FACE_RIGHT_OFFSET = new Vector3(3.5f, 1.5f, -12);
    private Vector3 FACE_LEFT_OFFSET = new Vector3(-3.5f, 1.5f, -12);
    [SerializeField]
    private Transform playerTarget;
    [SerializeField]
    private Vector3 offset;
    [SerializeField]
    private float smoothSpeedFactor;

    public bool isFollowingPlayer = true;

    private Vector3 velocity = Vector3.zero;

    public GameObject temporaryTarget;

    private Rigidbody2D targetRb;

    private void Awake()
    {
        targetRb = playerTarget.gameObject.GetComponent<Rigidbody2D>();
    }

    // Called after every other update
    private void LateUpdate()
    {
        if (isFollowingPlayer)
        {
            // If the player is facing the right direction, use the "right facing" camera offset
            if (playerTarget.gameObject.GetComponent<PlayerMovement>().isFacingRight)
            {
                offset = FACE_RIGHT_OFFSET;
            }
            // If the player is facing the left direction, use the "left facing" camera offset
            else
            {
                offset = FACE_LEFT_OFFSET;
            }
            // If the player's speed is greater than 1, increase the smooth speed to the normal amount
            if (Mathf.Abs(targetRb.linearVelocity.x) >= 1f || targetRb.linearVelocity.y > 1f || targetRb.linearVelocity.y < -1.1f)
            {
                smoothSpeedFactor = 0.4f;
            }
            // If the player's speed is greater than 1, increase the smooth speed to the normal amount
            else
            {
                smoothSpeedFactor = 0.4f;
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
            transform.position.x + (Random.Range(-1, 1) * intensity),
            transform.position.y + (Random.Range(-1, 1) * intensity),
            transform.position.z);
        yield return new WaitForSeconds(0.01f);
        transform.position = new Vector3(
            transform.position.x + (Random.Range(-0.5f, 0.5f) * intensity),
            transform.position.y + (Random.Range(-0.5f, 0.5f) * intensity),
            transform.position.z);
        yield return new WaitForSeconds(0.01f);
        transform.position = new Vector3(
            transform.position.x + (Random.Range(-0.1f, 0.1f) * intensity),
            transform.position.y + (Random.Range(-0.1f, 0.1f) * intensity),
            transform.position.z);
        yield return new WaitForSeconds(0.01f);
        transform.position = originalTransform;
    }
}
