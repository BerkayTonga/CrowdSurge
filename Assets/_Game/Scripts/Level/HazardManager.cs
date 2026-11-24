using System.Collections.Generic;
using UnityEngine;

namespace CrowdSurge
{
    /// <summary>Spinning saw blades: static or patrolling. Every drawn crowd unit touching a blade is destroyed.</summary>
    public sealed class HazardManager
    {
        private const float BladeRadius = 1.45f;
        private const float HitRadius = 1.3f;
        private const float SpinSpeed = 420f;

        private sealed class Saw
        {
            public SawDef def;
            public Transform transform;
            public float angle;
        }

        private readonly List<Saw> _saws = new List<Saw>();
        private readonly CrowdController _crowd;
        private readonly ParticleFx _fx;
        private readonly CameraRig _camera;
        private readonly FloatingTexts _texts;
        private readonly Color _deathColor;
        private float _time;

        public HazardManager(Transform parent, GameAssets assets, LevelConfig level, CrowdController crowd, ParticleFx fx, CameraRig camera, FloatingTexts texts)
        {
            _crowd = crowd;
            _fx = fx;
            _camera = camera;
            _texts = texts;
            _deathColor = GameAssets.PlayerColor;

            var root = new GameObject("Hazards").transform;
            root.SetParent(parent, false);
            var mesh = MeshFactory.CreateSawBlade(BladeRadius);

            foreach (var def in level.saws)
            {
                var go = new GameObject("Saw_" + def.z);
                go.transform.SetParent(root, false);
                go.transform.position = new Vector3(def.x, 0.2f, def.z);
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                var mr = go.AddComponent<MeshRenderer>();
                mr.sharedMaterials = new[] { assets.sawBlade, assets.sawHub };
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                mr.receiveShadows = false;
                _saws.Add(new Saw { def = def, transform = go.transform });
            }
        }

        /// <param name="checkCollisions">False after the run is over so a celebrating crowd stays alive.</param>
        public void Tick(float dt, bool checkCollisions)
        {
            _time += dt;
            float crowdZ = _crowd.Center.z;
            float reach = _crowd.Radius + BladeRadius + 1f;

            for (int i = 0; i < _saws.Count; i++)
            {
                var saw = _saws[i];
                float dz = saw.def.z - crowdZ;
                if (dz > 90f || dz < -20f) continue; // off-screen: skip all work

                saw.angle = (saw.angle + SpinSpeed * dt) % 360f;
                float x = saw.def.x;
                if (saw.def.patrolAmplitude > 0f)
                    x += Mathf.Sin(_time * saw.def.patrolSpeed) * saw.def.patrolAmplitude;
                saw.transform.SetPositionAndRotation(new Vector3(x, 0.2f, saw.def.z), Quaternion.Euler(0f, saw.angle, 0f));

                if (!checkCollisions || Mathf.Abs(dz) > reach) continue;

                int killed = _crowd.KillInCircle(x, saw.def.z, HitRadius);
                if (killed <= 0) continue;

                foreach (var p in _crowd.RecentDeaths)
                    _fx.Burst(p + Vector3.up * 0.5f, _deathColor, 5, 4f, 0.22f);
                _texts.Spawn(new Vector3(x, 2.2f, saw.def.z), "-" + killed, GameAssets.BadColor, 1.1f);
                AudioManager.Sound(Sfx.Saw, 0.5f, 0.08f, 0.12f);
                AudioManager.Sound(Sfx.Pop, 0.45f, 0.15f, 0.05f);
                _camera.Shake(0.12f, 0.15f);
                Haptics.Light();
            }
        }
    }
}
