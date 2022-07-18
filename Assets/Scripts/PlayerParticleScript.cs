using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerParticleScript : MonoBehaviour
{
    // Particles objects / components
    public GameObject particlesGroup;
    private ParticleSystem runAndJumpParticles;
    private ParticleSystem landingParticles;
    private TrailRenderer dashTrail;

    // Materials while on different group
    public Material grassMaterial;
    public Material rockMaterial;
    public Material lavaMaterial;

    private void Start()
    {
        runAndJumpParticles = particlesGroup.GetComponentsInChildren<ParticleSystem>()[0];
        landingParticles = particlesGroup.GetComponentsInChildren<ParticleSystem>()[1];
        dashTrail = particlesGroup.GetComponentInChildren<TrailRenderer>();
        dashTrail.emitting = false;
    }

    public void EmitRunningAndJumpingParticles()
    {
        // TODO: Determine which material to use with this effect
        runAndJumpParticles.GetComponent<ParticleSystemRenderer>().material = grassMaterial;

        runAndJumpParticles.Play();
    }

    public void EmitLandingParticles()
    {
        // TODO: Determine which material to use with this effect
        landingParticles.GetComponent<ParticleSystemRenderer>().material = grassMaterial;

        landingParticles.Play();
    }

    public void EmitDashTrail()
    {
        StartCoroutine(BrieflyEmitTrailAnimation(0.3f));
    }

    private IEnumerator BrieflyEmitTrailAnimation(float duration)
    {
        for(float i = 0; i < 1; i += Time.deltaTime / duration)
        {
            dashTrail.emitting = true;
            yield return null;
        }
        dashTrail.emitting = false;
    }
}
