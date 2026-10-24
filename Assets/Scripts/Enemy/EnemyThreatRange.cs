using UnityEngine;

// Put this on a child object of the enemy that has a SphereCollider (Is Trigger = true).
// That sphere is the enemy's threat range.
[RequireComponent(typeof(SphereCollider))]
public class EnemyThreatRange : MonoBehaviour
{
    public PlayerMovement Player { get; private set; }
    public bool PlayerInRange => Player != null;

    public event System.Action PlayerEntered;

    private readonly System.Collections.Generic.HashSet<Collider> collidersInRange = new System.Collections.Generic.HashSet<Collider>();

    public float Radius
    {
        get
        {
            SphereCollider sphere = GetComponent<SphereCollider>();
            return sphere.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.y);
        }
    }

    private void Reset()
    {
        GetComponent<SphereCollider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerMovement player = other.GetComponentInParent<PlayerMovement>();
        if (player == null) return;

        // The player may have several colliders; only the first one entering counts as entering the range
        collidersInRange.Add(other);
        if (Player == null)
        {
            Player = player;
            PlayerEntered?.Invoke();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        PlayerMovement player = other.GetComponentInParent<PlayerMovement>();
        if (player == null) return;

        collidersInRange.Remove(other);
        collidersInRange.RemoveWhere(c => c == null);
        if (collidersInRange.Count == 0 && player == Player) Player = null;
    }
}
