using UnityEngine;

// Procedural particle effects used when no prefab is assigned: a grenade-style explosion with
// shrapnel, and tiny looping explosions around a damaged body. Assign real prefabs for final art.
public static class EnemyEffects
{
    private static Material particleMaterial;

    private static Material GetMaterial()
    {
        if (particleMaterial == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            particleMaterial = new Material(shader);
        }
        return particleMaterial;
    }

    private static ParticleSystem CreateSystem(string name, Transform parent)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        go.GetComponent<ParticleSystemRenderer>().sharedMaterial = GetMaterial();
        return ps;
    }

    // Big explosion that blasts out along 'direction' from 'position'
    public static void SpawnExplosion(Vector3 position, Vector3 direction)
    {
        if (direction.sqrMagnitude < 0.0001f) direction = Vector3.up;

        GameObject root = new GameObject("EnemyExplosion");
        root.transform.position = position;
        root.transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);

        // Fireball
        ParticleSystem fire = CreateSystem("Fireball", root.transform);
        var fireMain = fire.main;
        fireMain.duration = 1f;
        fireMain.loop = false;
        fireMain.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.9f);
        fireMain.startSpeed = new ParticleSystem.MinMaxCurve(4f, 10f);
        fireMain.startSize = new ParticleSystem.MinMaxCurve(1.5f, 3.5f);
        fireMain.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.55f, 0.1f), new Color(1f, 0.9f, 0.3f));
        fireMain.simulationSpace = ParticleSystemSimulationSpace.World;
        var fireEmission = fire.emission;
        fireEmission.rateOverTime = 0f;
        fireEmission.SetBursts(new[] { new ParticleSystem.Burst(0f, 70) });
        var fireShape = fire.shape;
        fireShape.shapeType = ParticleSystemShapeType.Cone;
        fireShape.angle = 40f;
        fireShape.radius = 0.3f;
        var fireSize = fire.sizeOverLifetime;
        fireSize.enabled = true;
        fireSize.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0f));
        var fireColor = fire.colorOverLifetime;
        fireColor.enabled = true;
        Gradient fade = new Gradient();
        fade.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(0.4f, 0.1f, 0.05f), 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
        fireColor.color = fade;

        // Shrapnel
        ParticleSystem shrapnel = CreateSystem("Shrapnel", root.transform);
        var shMain = shrapnel.main;
        shMain.duration = 1f;
        shMain.loop = false;
        shMain.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 1.4f);
        shMain.startSpeed = new ParticleSystem.MinMaxCurve(12f, 26f);
        shMain.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.22f);
        shMain.startColor = new ParticleSystem.MinMaxGradient(new Color(0.25f, 0.25f, 0.25f), new Color(0.8f, 0.7f, 0.5f));
        shMain.gravityModifier = 1.5f;
        shMain.simulationSpace = ParticleSystemSimulationSpace.World;
        var shEmission = shrapnel.emission;
        shEmission.rateOverTime = 0f;
        shEmission.SetBursts(new[] { new ParticleSystem.Burst(0f, 30) });
        var shShape = shrapnel.shape;
        shShape.shapeType = ParticleSystemShapeType.Cone;
        shShape.angle = 55f;
        shShape.radius = 0.2f;
        var shRenderer = shrapnel.GetComponent<ParticleSystemRenderer>();
        shRenderer.renderMode = ParticleSystemRenderMode.Stretch;
        shRenderer.velocityScale = 0.06f;
        shRenderer.lengthScale = 2f;

        fire.Play();
        shrapnel.Play();
        Object.Destroy(root, 3f);
    }

    // 3 short black lines fanning out in a pie shape in the facing direction, vanishing quickly
    public static void SpawnAlertLines(Vector3 position, float facingSign)
    {
        GameObject root = new GameObject("EnemyAlertLines");
        root.transform.position = position;
        root.transform.rotation = Quaternion.LookRotation(Vector3.right * Mathf.Sign(facingSign), Vector3.up);

        ParticleSystem ps = CreateSystem("Lines", root.transform);
        var main = ps.main;
        main.duration = 0.2f;
        main.loop = false;
        main.startLifetime = 0.15f;
        main.startSpeed = 9f;
        main.startSize = 0.08f;
        main.startColor = Color.black;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 3) });
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.01f;
        shape.arc = 60f;
        shape.arcMode = ParticleSystemShapeMultiModeValue.BurstSpread;
        shape.arcSpread = 0f;
        // Circle shape emits in its local XY plane outward; turn it so the fan lies in the world XY plane
        shape.rotation = new Vector3(0f, -90f, -30f);
        shape.position = Vector3.zero;
        var color = ps.colorOverLifetime;
        color.enabled = true;
        Gradient fade = new Gradient();
        fade.SetKeys(
            new[] { new GradientColorKey(Color.black, 0f), new GradientColorKey(Color.black, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.6f), new GradientAlphaKey(0f, 1f) });
        color.color = fade;
        var r = ps.GetComponent<ParticleSystemRenderer>();
        r.renderMode = ParticleSystemRenderMode.Stretch;
        r.velocityScale = 0f;
        r.lengthScale = 6f;

        ps.Play();
        Object.Destroy(root, 1f);
    }

    // Tiny explosions popping around a body, parented so they follow it
    public static ParticleSystem CreateSmallExplosions(Transform parent, float radius)
    {
        ParticleSystem ps = CreateSystem("DamageExplosions", parent);
        var main = ps.main;
        main.loop = true;
        main.startLifetime = 0.35f;
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.3f, 0.7f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.4f, 0.05f), new Color(1f, 0.85f, 0.2f));
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        var emission = ps.emission;
        emission.rateOverTime = 7f;
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = radius;
        var size = ps.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0f, 0.2f), new Keyframe(0.3f, 1f), new Keyframe(1f, 0f)));
        ps.Play();
        return ps;
    }
}
