using UnityEngine;

// Enemy health. Health is shown through the cartoon shader's health color (see HealthColorDriver),
// not a UI bar. Call TakeDamage from the player's attack when it lands a hit.
[RequireComponent(typeof(EnemyAI))]
public class EnemyHealth : MonoBehaviour
{
    [Header("Health")]
    public float maxHealth = 100f;

    [Header("Health Color")]
    [Tooltip("Drives the shader's health color. Found automatically if left empty")]
    public HealthColorDriver colorDriver;

    [Header("Low Health Effect")]
    [Tooltip("Tiny explosions around the body start once health is at or below this fraction of max")]
    [Range(0f, 1f)] public float lowHealthFraction = 0.3f;
    [Tooltip("Optional. If empty, a simple procedural effect is created")]
    public ParticleSystem lowHealthParticles;
    public float lowHealthEffectRadius = 0.6f;

    [Header("Coins")]
    public GameObject coinPrefab;
    public int minCoins = 1;
    public int maxCoins = 3;
    public float coinScatterSpeed = 6f;

    public float CurrentHealth { get; private set; }
    public bool IsDead { get; private set; }

    private EnemyAI ai;

    private void Awake()
    {
        ai = GetComponent<EnemyAI>();
        if (colorDriver == null) colorDriver = GetComponentInChildren<HealthColorDriver>();
        CurrentHealth = maxHealth;
    }

    // hitDirection: the direction the hit sends the enemy (x > 0 = right)
    public void TakeDamage(float amount, Vector2 hitDirection)
    {
        if (IsDead || amount <= 0f) return;

        CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);

        if (colorDriver != null)
            colorDriver.SetDamage01(1f - CurrentHealth / maxHealth);

        if (CurrentHealth <= 0f)
        {
            Die(hitDirection);
            return;
        }

        UpdateLowHealthEffect();
        ai.TakeHit(hitDirection);
    }

    private void UpdateLowHealthEffect()
    {
        if (CurrentHealth > maxHealth * lowHealthFraction) return;

        if (lowHealthParticles == null)
            lowHealthParticles = EnemyEffects.CreateSmallExplosions(transform, lowHealthEffectRadius);
        else if (!lowHealthParticles.isPlaying)
            lowHealthParticles.Play();
    }

    private void Die(Vector2 hitDirection)
    {
        IsDead = true;

        if (lowHealthParticles != null)
            lowHealthParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);

        DropCoins();
        ai.Die(hitDirection);
    }

    private void DropCoins()
    {
        if (coinPrefab == null) return;

        int count = Random.Range(minCoins, maxCoins + 1);
        for (int i = 0; i < count; i++)
        {
            GameObject coin = Instantiate(coinPrefab, transform.position, Quaternion.identity);
            Rigidbody coinBody = coin.GetComponent<Rigidbody>();
            if (coinBody != null && !coinBody.isKinematic)
            {
                Vector3 burst = new Vector3(Random.Range(-1f, 1f), Random.Range(0.6f, 1.2f), 0f).normalized;
                coinBody.linearVelocity = burst * coinScatterSpeed;
            }
        }
    }
}
