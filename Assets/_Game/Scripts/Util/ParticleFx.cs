using UnityEngine;

namespace CrowdSurge
{
    /// <summary>One shared particle system for all bursts (deaths, gate sparkles, confetti): a single draw call.</summary>
    public sealed class ParticleFx : MonoBehaviour
    {
        private ParticleSystem _ps;

        public static ParticleFx Create(Transform parent, Material material)
        {
            var go = new GameObject("ParticleFx");
            go.transform.SetParent(parent, false);
            var fx = go.AddComponent<ParticleFx>();
            fx.Init(material);
            return fx;
        }

        private void Init(Material material)
        {
            _ps = gameObject.AddComponent<ParticleSystem>();
            _ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = _ps.main;
            main.playOnAwake = false;
            main.loop = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 800;
            main.startLifetime = 0.7f;
            main.startSpeed = 0f;
            main.startSize = 0.3f;
            main.gravityModifier = 1.4f;

            var emission = _ps.emission;
            emission.enabled = false;
            var shape = _ps.shape;
            shape.enabled = false;

            var sizeOverLife = _ps.sizeOverLifetime;
            sizeOverLife.enabled = true;
            sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0f));

            var renderer = _ps.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            _ps.Play();
        }

        public void Burst(Vector3 position, Color color, int count, float speed, float size, float lifetime = 0.6f)
        {
            var p = new ParticleSystem.EmitParams
            {
                position = position,
                startColor = color,
                startSize = size,
                startLifetime = lifetime
            };
            for (int i = 0; i < count; i++)
            {
                p.velocity = Random.onUnitSphere * speed * Random.Range(0.4f, 1f) + Vector3.up * speed * 0.5f;
                _ps.Emit(p, 1);
            }
        }

        /// <summary>Flat shockwave ring of particles (gate passes, boss kills).</summary>
        public void Ring(Vector3 center, Color color, int count = 28, float speed = 8f, float size = 0.28f)
        {
            var p = new ParticleSystem.EmitParams { position = center, startColor = color, startSize = size, startLifetime = 0.55f };
            for (int i = 0; i < count; i++)
            {
                float a = i * Mathf.PI * 2f / count;
                p.velocity = new Vector3(Mathf.Cos(a), 0.15f, Mathf.Sin(a)) * speed;
                _ps.Emit(p, 1);
            }
        }

        public void Confetti(Vector3 center, int count)
        {
            var p = new ParticleSystem.EmitParams { startLifetime = 2.2f, startSize = 0.22f };
            for (int i = 0; i < count; i++)
            {
                p.position = center + new Vector3(Random.Range(-4f, 4f), Random.Range(0f, 2f), Random.Range(-1f, 3f));
                p.velocity = new Vector3(Random.Range(-3f, 3f), Random.Range(6f, 12f), Random.Range(-2f, 4f));
                p.startColor = Color.HSVToRGB(Random.value, 0.75f, 1f);
                _ps.Emit(p, 1);
            }
        }
    }
}
