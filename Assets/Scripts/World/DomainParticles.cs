using UnityEngine;

namespace NitroRhythm.World
{
    /// <summary>Keeps the ambient particle volume around a target (the lead player / camera).</summary>
    public class ParticleFollow : MonoBehaviour
    {
        public Transform Target;
        public Vector3 Offset = new Vector3(18f, 6f, 0f);

        private void LateUpdate()
        {
            if (Target != null) transform.position = Target.position + Offset;
        }
    }

    /// <summary>Ambient particle presets per domain (dust, sparks, embers, fireflies, wisps, stars).</summary>
    public static class DomainParticles
    {
        /// <summary>All three axes must use the same curve mode, so every axis is a random-between-two-constants.</summary>
        private static void SetVelocity(ParticleSystem.VelocityOverLifetimeModule v, float x0, float x1, float y0, float y1, float z0, float z1)
        {
            v.enabled = true;
            v.space = ParticleSystemSimulationSpace.World;
            v.x = new ParticleSystem.MinMaxCurve(x0, x1);
            v.y = new ParticleSystem.MinMaxCurve(y0, y1);
            v.z = new ParticleSystem.MinMaxCurve(z0, z1);
        }

        public static GameObject Create(DomainTheme theme, Transform follow)
        {
            GameObject go = new GameObject($"Ambient_{theme.particles}");
            go.AddComponent<ParticleFollow>().Target = follow;

            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = ps.main;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.loop = true;
            main.playOnAwake = false;
            main.maxParticles = 600;

            ParticleSystem.ShapeModule shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(90f, 34f, 70f);

            ParticleSystem.EmissionModule emission = ps.emission;
            ParticleSystem.NoiseModule noise = ps.noise;
            ParticleSystem.VelocityOverLifetimeModule velocity = ps.velocityOverLifetime;
            ParticleSystem.ColorOverLifetimeModule colorLife = ps.colorOverLifetime;
            colorLife.enabled = true;

            Color c = theme.particleColor;
            Color c2 = Color.Lerp(c, Color.white, 0.5f);

            // Fade in and out so particles never pop.
            Gradient fade = new Gradient();
            fade.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.2f), new GradientAlphaKey(1f, 0.75f), new GradientAlphaKey(0f, 1f) });
            colorLife.color = fade;

            switch (theme.particles)
            {
                case ParticleKind.Dust:
                    main.startLifetime = new ParticleSystem.MinMaxCurve(8f, 12f);
                    main.startSize = new ParticleSystem.MinMaxCurve(0.15f, 0.4f);
                    main.startSpeed = new ParticleSystem.MinMaxCurve(0.3f, 1.2f);
                    main.startColor = new ParticleSystem.MinMaxGradient(c, c2);
                    emission.rateOverTime = 32f;
                    SetVelocity(velocity, 2f, 3f, -0.3f, 0.3f, -0.3f, 0.3f);
                    noise.enabled = true; noise.strength = 0.6f; noise.frequency = 0.2f;
                    break;
                case ParticleKind.Sparks:
                    main.startLifetime = new ParticleSystem.MinMaxCurve(3f, 5f);
                    main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.22f);
                    main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 5f);
                    main.startColor = new ParticleSystem.MinMaxGradient(c, new Color(1f, 0.4f, 0.8f));
                    emission.rateOverTime = 40f;
                    SetVelocity(velocity, -0.5f, 0.5f, 2f, 6f, -0.5f, 0.5f);
                    break;
                case ParticleKind.Embers:
                    main.startLifetime = new ParticleSystem.MinMaxCurve(6f, 9f);
                    main.startSize = new ParticleSystem.MinMaxCurve(0.14f, 0.32f);
                    main.startSpeed = new ParticleSystem.MinMaxCurve(0.2f, 0.8f);
                    main.startColor = new ParticleSystem.MinMaxGradient(c, c2);
                    emission.rateOverTime = 30f;
                    SetVelocity(velocity, -0.3f, 0.3f, 1f, 3f, -0.3f, 0.3f);
                    noise.enabled = true; noise.strength = 0.8f; noise.frequency = 0.25f;
                    break;
                case ParticleKind.Fireflies:
                    main.startLifetime = new ParticleSystem.MinMaxCurve(5f, 8f);
                    main.startSize = new ParticleSystem.MinMaxCurve(0.16f, 0.32f);
                    main.startSpeed = 0.4f;
                    main.startColor = new ParticleSystem.MinMaxGradient(c, new Color(0.8f, 1f, 0.4f));
                    emission.rateOverTime = 24f;
                    noise.enabled = true; noise.strength = 2.2f; noise.frequency = 0.35f;
                    break;
                case ParticleKind.Wisps:
                    main.startLifetime = new ParticleSystem.MinMaxCurve(10f, 16f);
                    main.startSize = new ParticleSystem.MinMaxCurve(4f, 9f);
                    main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 1.2f);
                    main.startColor = new Color(1f, 1f, 1f, 0.18f);
                    emission.rateOverTime = 5f;
                    SetVelocity(velocity, -3.5f, -2.5f, -0.2f, 0.2f, -0.2f, 0.2f);
                    break;
                case ParticleKind.Stars:
                    main.startLifetime = new ParticleSystem.MinMaxCurve(6f, 10f);
                    main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.24f);
                    main.startSpeed = 0f;
                    main.startColor = new ParticleSystem.MinMaxGradient(c, Color.white);
                    emission.rateOverTime = 70f;
                    break;
                default:
                    main.startLifetime = 6f; main.startSize = 0.2f; main.startSpeed = 0.5f; main.startColor = c; emission.rateOverTime = 20f;
                    break;
            }

            ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sharedMaterial = DomainMaterials.Particle(Color.white);
            renderer.sortingFudge = 10f;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            // Pre-warm so the air is already full when the level starts.
            ps.Simulate(8f, true, true);
            ps.Play();
            return go;
        }
    }
}
