using System;
using System.Collections.Generic;
using UnityEngine;

namespace CrowdSurge
{
    /// <summary>Enemy squads and the boss. Handles the charge-and-clash battle against the player crowd.</summary>
    public sealed class EnemyManager
    {
        private enum Phase { Idle, Charging, Clashing, Defeated }

        private sealed class Group
        {
            public EnemyDef def;
            public Swarm swarm;
            public WorldLabel label;
            public Phase phase;
        }

        private const float EngageDistance = 15f;
        private const float ChargeSpeed = 11f;
        private const float ClashDuration = 1.3f;
        private const float MinKillRate = 16f;

        private readonly List<Group> _groups = new List<Group>();
        private readonly CrowdController _crowd;
        private readonly ParticleFx _fx;
        private readonly CameraRig _camera;
        private readonly FloatingTexts _texts;
        private readonly Color _crowdColor, _enemyColor;

        private Group _active;
        private float _killRate;
        private float _killAccumulator;

        public bool InBattle => _active != null;

        /// <summary>Raised when a battle ends. Args: player won it, the defeated/defeating group was the boss.</summary>
        public event Action<bool, bool> BattleEnded;
        public event Action BattleStarted;

        public EnemyManager(Transform parent, GameAssets assets, LevelConfig level, CrowdController crowd, ParticleFx fx, CameraRig camera, FloatingTexts texts)
        {
            _crowd = crowd;
            _fx = fx;
            _camera = camera;
            _texts = texts;
            _crowdColor = GameAssets.PlayerColor;
            _enemyColor = GameAssets.EnemyColor;

            var root = new GameObject("Enemies").transform;
            root.SetParent(parent, false);

            foreach (var def in level.enemies)
            {
                bool boss = def.isBoss;
                var swarm = new Swarm(new Swarm.Settings
                {
                    maxVisible = boss ? 40 : 60,
                    preferredSpacing = boss ? 0.95f : 0.42f,
                    maxRadius = boss ? 3.4f : 2.6f,
                    unitScale = boss ? 2.2f : 1f,
                    yaw = 180f,
                    followSharpness = 9f,
                    runRate = 1f / assets.character.runCycleSeconds
                }, assets.character, boss ? assets.bossMaterials : assets.enemyMaterials);

                swarm.Center = new Vector3(def.x, 0f, def.z);
                swarm.SetCount(def.count);
                swarm.SnapToFormation();

                var label = WorldLabel.Create(root, boss ? "BossLabel" : "EnemyLabel", boss ? 1.5f : 0.85f, boss ? new Color(1f, 0.25f, 0.2f) : new Color(1f, 0.3f, 0.3f));
                _groups.Add(new Group { def = def, swarm = swarm, label = label });
            }
        }

        public void Tick(float dt, float time)
        {
            float crowdZ = _crowd.Center.z;

            if (_active == null) FindEngagement(crowdZ);
            else UpdateBattle(dt);

            for (int i = 0; i < _groups.Count; i++)
            {
                var g = _groups[i];
                if (g.phase == Phase.Defeated) continue;

                float dz = g.swarm.Center.z - crowdZ;
                bool near = dz < 90f && dz > -25f;
                g.label.SetVisible(near);
                if (!near) continue;

                g.swarm.Tick(dt);
                g.swarm.Idle = g.phase == Phase.Idle;
                g.swarm.Render(time, g.phase == Phase.Idle ? 1f : 1.5f, 0f);

                float scale = g.def.isBoss ? 1.3f : 1f;
                g.label.SetText(NumberFormat.Short(g.swarm.Count));
                g.label.transform.position = g.swarm.Center + new Vector3(0f, (g.def.isBoss ? 6.2f : 2.8f) + g.swarm.Radius * 0.3f, 0f);
                g.label.transform.localScale = Vector3.one * scale;
            }
        }

        private void FindEngagement(float crowdZ)
        {
            for (int i = 0; i < _groups.Count; i++)
            {
                var g = _groups[i];
                if (g.phase != Phase.Idle) continue;
                if (g.swarm.Center.z - crowdZ > EngageDistance + _crowd.Radius + g.swarm.Radius) continue;

                g.phase = Phase.Charging;
                _active = g;
                _killAccumulator = 0f;
                float strongest = Mathf.Min(_crowd.Count, g.swarm.Count);
                _killRate = Mathf.Max(MinKillRate, strongest / ClashDuration);

                AudioManager.Sound(Sfx.BattleCry, 0.7f, 0.05f, 0.3f);
                BattleStarted?.Invoke();
                return;
            }
        }

        private void UpdateBattle(float dt)
        {
            var g = _active;
            Vector3 toCrowd = _crowd.Center - g.swarm.Center;
            toCrowd.y = 0f;
            float gap = toCrowd.magnitude;
            float touchDistance = (_crowd.Radius + g.swarm.Radius) * 0.7f;

            if (g.phase == Phase.Charging)
            {
                if (gap > touchDistance)
                {
                    Vector3 dir = toCrowd / Mathf.Max(0.001f, gap);
                    g.swarm.Center += dir * (ChargeSpeed * dt);
                    _crowd.MoveToward(g.swarm.Center, ChargeSpeed * 0.6f, dt);
                }
                else
                {
                    g.phase = Phase.Clashing;
                }
                return;
            }

            // Clashing: both sides lose units at the same pace until one is wiped out.
            _killAccumulator += _killRate * dt;
            int kills = (int)_killAccumulator;
            if (kills > 0)
            {
                _killAccumulator -= kills;
                kills = Mathf.Min(kills, Mathf.Min(_crowd.Count, g.swarm.Count));
                _crowd.SetCount(_crowd.Count - kills);
                g.swarm.SetCount(g.swarm.Count - kills);
                SpawnClashFx();
                AudioManager.Sound(Sfx.Clash, 0.45f, 0.12f, 0.06f);
                _camera.Shake(0.08f, 0.1f);
            }

            if (g.swarm.Count > 0 && _crowd.Count > 0) return;

            bool playerWon = _crowd.Count > 0;
            if (playerWon)
            {
                g.phase = Phase.Defeated;
                g.label.SetVisible(false);
                bool boss = g.def.isBoss;
                SaveData.TotalDefeated += g.def.count;
                _fx.Burst(g.swarm.Center + Vector3.up, _enemyColor, boss ? 90 : 40, boss ? 11f : 7f, 0.35f, 0.9f);
                _fx.Ring(g.swarm.Center + Vector3.up * 0.4f, Color.white, boss ? 44 : 28, boss ? 12f : 8f, 0.34f);
                _texts.Spawn(g.swarm.Center + new Vector3(0f, boss ? 5f : 3.2f, 0f), boss ? "PATRON YENİLDİ!" : "YENDİN!", new Color(1f, 0.9f, 0.3f), boss ? 1.8f : 1.2f);
                AudioManager.Sound(boss ? Sfx.BossDown : Sfx.GatePositive, boss ? 0.95f : 0.6f, 0.02f, 0.2f);
                _camera.Punch(boss ? 6f : 3f);
            }
            _active = null;
            _crowd.SetEngageOffset(Vector3.zero);
            BattleEnded?.Invoke(playerWon, g.def.isBoss);
        }

        private void SpawnClashFx()
        {
            var g = _active;
            _fx.Burst(g.swarm.RandomUnitPosition() + Vector3.up * 0.6f, _enemyColor, 4, 4f, 0.22f);
            _fx.Burst(_crowd.Swarm.RandomUnitPosition() + Vector3.up * 0.6f, _crowdColor, 4, 4f, 0.22f);
        }
    }
}
