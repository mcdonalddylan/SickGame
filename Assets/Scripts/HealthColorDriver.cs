using UnityEngine;

// Drives the "_HealthDamage" value of the cartoon shader so a character's health shows up as a color
// slowly taking over its body. Works for any character (enemy or player): call SetDamage01 with
// 0 = healthy and 1 = dead. The material must have "Use Health Color" ticked for it to render.
public class HealthColorDriver : MonoBehaviour
{
    private static readonly int HealthDamageId = Shader.PropertyToID("_HealthDamage");

    [Tooltip("Renderers to tint. Leave empty to use every renderer on this object and its children")]
    public Renderer[] targetRenderers;

    [Tooltip("How fast the health color spreads, in damage units (0-1) per second")]
    public float fillSpeed = 1.5f;

    private MaterialPropertyBlock block;
    private float targetDamage;
    private float shownDamage;

    private void Awake()
    {
        block = new MaterialPropertyBlock();

        if (targetRenderers == null || targetRenderers.Length == 0)
        {
            Renderer[] all = GetComponentsInChildren<Renderer>();
            var list = new System.Collections.Generic.List<Renderer>();
            foreach (Renderer r in all)
            {
                if (!(r is ParticleSystemRenderer)) list.Add(r);
            }
            targetRenderers = list.ToArray();
        }

        Apply(0f);
    }

    public void SetDamage01(float damage01)
    {
        targetDamage = Mathf.Clamp01(damage01);
    }

    private void Update()
    {
        if (Mathf.Approximately(shownDamage, targetDamage)) return;

        shownDamage = Mathf.MoveTowards(shownDamage, targetDamage, fillSpeed * Time.deltaTime);
        Apply(shownDamage);
    }

    private void Apply(float value)
    {
        foreach (Renderer r in targetRenderers)
        {
            if (r == null) continue;
            r.GetPropertyBlock(block);
            block.SetFloat(HealthDamageId, value);
            r.SetPropertyBlock(block);
        }
    }
}
