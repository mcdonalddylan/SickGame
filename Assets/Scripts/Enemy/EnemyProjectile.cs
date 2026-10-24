using UnityEngine;

// Simple straight-flying projectile. Needs a trigger collider and (kinematic) Rigidbody on the prefab.
public class EnemyProjectile : MonoBehaviour
{
    public float speed = 15f;
    public float lifetime = 4f;
    public float damage = 1f;
    public LayerMask groundLayer;

    private Vector3 direction = Vector3.right;

    public void Launch(Vector3 dir)
    {
        direction = new Vector3(dir.x, dir.y, 0f).normalized;
        Destroy(gameObject, lifetime);
    }

    private void Update()
    {
        transform.position += direction * (speed * Time.deltaTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerMovement player = other.GetComponentInParent<PlayerMovement>();
        if (player != null)
        {
            player.SendMessage("TakeDamage", damage, SendMessageOptions.DontRequireReceiver);
            Destroy(gameObject);
            return;
        }

        if (((1 << other.gameObject.layer) & groundLayer.value) != 0)
            Destroy(gameObject);
    }
}
