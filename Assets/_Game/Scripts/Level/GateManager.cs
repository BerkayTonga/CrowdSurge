using System.Collections.Generic;
using UnityEngine;

namespace CrowdSurge
{
    /// <summary>Spawns the +/-/x/÷ gates and applies the chosen side to the crowd when it runs through.</summary>
    public sealed class GateManager
    {
        private sealed class Gate
        {
            public GateDef def;
            public Transform root;
            public WorldLabel leftLabel, rightLabel;
            public bool consumed;
            public float punchTimer;
            public int punchSide;
        }

        private static readonly Color GoodLabel = Color.white;
        private static readonly Color BadLabel = new Color(1f, 0.92f, 0.92f);

        private readonly List<Gate> _gates = new List<Gate>();
        private readonly CrowdController _crowd;
        private readonly ParticleFx _fx;
        private readonly CameraRig _camera;
        private readonly FloatingTexts _texts;

        public GateManager(Transform parent, GameAssets assets, LevelConfig level, CrowdController crowd, ParticleFx fx, CameraRig camera, FloatingTexts texts)
        {
            _crowd = crowd;
            _fx = fx;
            _camera = camera;
            _texts = texts;

            var root = new GameObject("Gates").transform;
            root.SetParent(parent, false);
            foreach (var def in level.gates) _gates.Add(Spawn(root, assets, def));
        }

        public void Tick(float dt)
        {
            float crowdZ = _crowd.Center.z;
            for (int i = 0; i < _gates.Count; i++)
            {
                var gate = _gates[i];
                AnimatePunch(gate, dt);
                if (gate.consumed || crowdZ < gate.def.z) continue;

                gate.consumed = true;
                bool right = _crowd.Center.x >= 0f;
                Trigger(gate, right);
            }
        }

        private void Trigger(Gate gate, bool right)
        {
            GateEffect effect = right ? gate.def.right : gate.def.left;
            _crowd.SetCount(effect.Apply(_crowd.Count));

            Haptics.Light();
            gate.punchTimer = 0.35f;
            gate.punchSide = right ? 1 : -1;

            Vector3 pos = new Vector3(gate.punchSide * 2.5f, 1.5f, gate.def.z);
            Vector3 above = _crowd.Center + new Vector3(0f, 3.4f, 0f);
            _texts.Spawn(above, effect.Label, effect.IsPositive ? GameAssets.GoodColor : GameAssets.BadColor, 1.3f);
            if (effect.IsPositive)
            {
                _fx.Ring(_crowd.Center + Vector3.up * 0.3f, GameAssets.GoodColor);
                _fx.Burst(pos, new Color(0.4f, 0.85f, 1f), 26, 6f, 0.3f);
                AudioManager.Sound(Sfx.GatePositive, 0.8f, 0.02f, 0.1f);
                _camera.Punch(0.8f);
            }
            else
            {
                _fx.Ring(_crowd.Center + Vector3.up * 0.3f, GameAssets.BadColor, 20, 6f);
                _fx.Burst(pos, new Color(1f, 0.35f, 0.3f), 26, 6f, 0.3f);
                AudioManager.Sound(Sfx.GateNegative, 0.75f, 0.02f, 0.1f);
                _camera.Shake(0.25f, 0.3f);
            }
        }

        private static void AnimatePunch(Gate gate, float dt)
        {
            if (gate.punchTimer <= 0f) return;
            gate.punchTimer -= dt;
            float k = Mathf.Clamp01(gate.punchTimer / 0.35f);
            float scale = 1f + Mathf.Sin(k * Mathf.PI) * 0.35f;
            var label = gate.punchSide > 0 ? gate.rightLabel : gate.leftLabel;
            label.transform.localScale = Vector3.one * scale;
        }

        private static Gate Spawn(Transform parent, GameAssets assets, GateDef def)
        {
            var gate = new Gate { def = def };
            var root = new GameObject("Gate_" + def.z).transform;
            root.SetParent(parent, false);
            root.localPosition = new Vector3(0f, 0f, def.z);
            gate.root = root;

            gate.leftLabel = BuildSide(root, assets, -1f, def.left);
            gate.rightLabel = BuildSide(root, assets, 1f, def.right);

            // Shared centre post
            MeshFactory.CreateBox(root, "PostMid", new Vector3(0f, 1.5f, 0f), new Vector3(0.3f, 3f, 0.3f), assets.gateFrameGood);
            return gate;
        }

        private static WorldLabel BuildSide(Transform root, GameAssets assets, float side, GateEffect effect)
        {
            bool good = effect.IsPositive;
            Material panelMat = good ? assets.gateGood : assets.gateBad;
            Material frameMat = good ? assets.gateFrameGood : assets.gateFrameBad;

            float cx = side * 2.5f;
            MeshFactory.CreateBox(root, "Panel", new Vector3(cx, 1.55f, 0f), new Vector3(4.7f, 2.3f, 0.06f), panelMat);
            MeshFactory.CreateBox(root, "Base", new Vector3(cx, 0.2f, 0f), new Vector3(4.9f, 0.4f, 0.35f), frameMat);
            MeshFactory.CreateBox(root, "Top", new Vector3(cx, 2.85f, 0f), new Vector3(4.9f, 0.3f, 0.35f), frameMat);
            MeshFactory.CreateBox(root, "PostOuter", new Vector3(side * 4.85f, 1.5f, 0f), new Vector3(0.3f, 3f, 0.3f), frameMat);

            var label = WorldLabel.Create(root, "Label", 1.25f, good ? GoodLabel : BadLabel);
            label.transform.localPosition = new Vector3(cx, 1.5f, -0.25f);
            label.SetText(effect.Label);
            return label;
        }
    }
}
