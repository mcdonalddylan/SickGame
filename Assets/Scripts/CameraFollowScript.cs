using System.Collections;
using UnityEngine;

public class CameraFollowScript : MonoBehaviour
{
    private Vector3 FACE_RIGHT_OFFSET = new Vector3(3.5f, 2f, -12);
    private Vector3 FACE_LEFT_OFFSET = new Vector3(-3.5f, 2f, -12);
    private Vector3 WALL_SLIDE_RIGHT_OFFSET = new Vector3(5.5f, 1.5f, -12);
    private Vector3 WALL_SLIDE_LEFT_OFFSET = new Vector3(-5.5f, 1.5f, -12);
    private Vector3 FACE_RIGHT_FALLING_OFFSET = new Vector3(3f, -2.5f, -12);
    private Vector3 FACE_LEFT_FALLING_OFFSET = new Vector3(-3f, -2.5f, -12);
    [SerializeField] private Transform playerTarget;
    [SerializeField] private Vector3 offset;
    private float smoothSpeedFactor = 0.2f;
    private bool isTargetFalling;

    public bool isFollowingPlayer = true;

    private Vector3 velocity = Vector3.zero;

    public GameObject temporaryTarget;

    private Rigidbody2D targetRb;
    private PlayerMovement targetMovement;

    private void Awake()
    {
        targetRb = playerTarget.gameObject.GetComponent<Rigidbody2D>();
        targetMovement = playerTarget.gameObject.GetComponent<PlayerMovement>();
    }

    // Called after every other update
    private void LateUpdate()
    {
        if (isFollowingPlayer)
        {
            IsTargetFalling();

            if (targetMovement.isWallSliding && targetMovement.IsFacingRight)
            {
                offset = WALL_SLIDE_LEFT_OFFSET;
            }
            else if (targetMovement.isWallSliding && !targetMovement.IsFacingRight)
            {
                offset = WALL_SLIDE_RIGHT_OFFSET;
            }
            else if (!isTargetFalling && targetMovement.IsFacingRight)
            {
                offset = FACE_RIGHT_OFFSET;
            }
            else if (!isTargetFalling && !targetMovement.IsFacingRight)
            {
                offset = FACE_LEFT_OFFSET;
            }
            else if (isTargetFalling && targetMovement.IsFacingRight)
            {
                offset = FACE_RIGHT_FALLING_OFFSET;
            }
            else if (isTargetFalling && !targetMovement.IsFacingRight)
            {
                offset = FACE_LEFT_FALLING_OFFSET;
            }
            Vector3 desiredPosition = playerTarget.position + offset;
            transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref velocity, smoothSpeedFactor);
        }
    }

    private void IsTargetFalling()
    {
        if (targetMovement.fallTimer > 0.75f)
        {
            isTargetFalling = true;
            smoothSpeedFactor = 0.1f;
        }
        else
        {
            isTargetFalling = false;
            smoothSpeedFactor = 0.2f;
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
