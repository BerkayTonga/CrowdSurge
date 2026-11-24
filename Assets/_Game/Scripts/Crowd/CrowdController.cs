using System.Collections.Generic;
using UnityEngine;

namespace CrowdSurge
{
    /// <summary>The player's crowd: forward run, drag steering, count label and rendering.</summary>
    public sealed class CrowdController : MonoBehaviour
    {
        private const float RunSpeed = 9f;
        private const float SteerSharpness = 14f;
        private const float LabelHeight = 3.0f;

        private Swarm _swarm;
        private WorldLabel _label;
        private float _targetX;
        private float _hop;
        private int _lastCount = -1;
        private float _labelPop;
        private float _runSpeed = 1f;
        private bool _labelHidden;

        public Vector3 Center => _swarm.Center;
        public int Count => _swarm.Count;
        public float Radius => _swarm.Radius;
        public List<Vector3> RecentDeaths => _swarm.RecentDeaths;
        public Swarm Swarm => _swarm;

        public static CrowdController Create(Transform parent, GameAssets assets)
        {
            var go = new GameObject("Crowd");
            go.transform.SetParent(parent, false);
            var crowd = go.AddComponent<CrowdController>();
            crowd.Init(assets);
            return crowd;
        }

        private void Init(GameAssets assets)
        {
            _swarm = new Swarm(new Swarm.Settings
            {
                maxVisible = 120,
                preferredSpacing = 0.42f,
                maxRadius = 3.1f,
                unitScale = 1f,
                yaw = 0f,
                followSharpness = 10f,
                runRate = 1f / assets.character.runCycleSeconds
            }, assets.character, assets.playerMaterials);

            _label = WorldLabel.Create(transform, "CountLabel", 0.85f, new Color(0.15f, 0.55f, 1f));
        }

        public void ResetCrowd(int startCount)
        {
            _swarm.Center = new Vector3(0f, 0f, GameConstants.CrowdStartZ);
            _swarm.EngageOffset = Vector3.zero;
            _targetX = 0f;
            _swarm.SetCount(startCount);
            _swarm.SnapToFormation();
        }

        public void SetCount(int count) => _swarm.SetCount(Mathf.Clamp(count, 0, GameConstants.MaxCrowd));

        public int KillInCircle(float cx, float cz, float radius) => _swarm.KillInCircle(cx, cz, radius);

        public void SetLabelVisible(bool visible) => _labelHidden = !visible;

        /// <summary>Victory hopping height in metres.</summary>
        public void SetHop(float height) => _hop = height;

        /// <summary>Standing pose (menu, ready screen).</summary>
        public void SetIdle(bool idle) => _swarm.Idle = idle;

        public void SetRunSpeed(float speed) => _runSpeed = speed;

        public void SetTargetX(float x) => _targetX = x;

        /// <summary>Runs forward (optional) and steers by a world-space horizontal delta.</summary>
        public void Tick(float dt, bool running, float steerDeltaX)
        {
            Vector3 c = _swarm.Center;
            if (running)
            {
                c.z += RunSpeed * dt;
                _targetX += steerDeltaX;
            }

            float limit = Mathf.Max(0f, GameConstants.RoadHalfWidth - Mathf.Min(_swarm.Radius * 0.75f, 3.6f) - 0.25f);
            _targetX = Mathf.Clamp(_targetX, -limit, limit);
            c.x = Mathf.Lerp(c.x, _targetX, 1f - Mathf.Exp(-SteerSharpness * dt));
            _swarm.Center = c;
        }

        /// <summary>Moves the crowd centre toward a point on the XZ plane (used while charging an enemy).</summary>
        public void MoveToward(Vector3 target, float speed, float dt)
        {
            Vector3 c = _swarm.Center;
            Vector3 d = target - c;
            d.y = 0f;
            float step = speed * dt;
            if (d.sqrMagnitude <= step * step) return;
            c += d.normalized * step;
            _swarm.Center = c;
            _targetX = c.x;
        }

        public void SetEngageOffset(Vector3 offset) => _swarm.EngageOffset = offset;

        public void Refresh(float dt, float time)
        {
            _swarm.Tick(dt);
            _swarm.Render(time, _runSpeed, _hop);

            bool visible = _swarm.Count > 0 && !_labelHidden;
            _label.SetVisible(visible);
            if (!visible) return;

            if (_swarm.Count != _lastCount)
            {
                _label.SetColor(_swarm.Count > _lastCount ? new Color(0.3f, 1f, 0.55f) : new Color(1f, 0.45f, 0.4f));
                _labelPop = 1f;
                _lastCount = _swarm.Count;
            }
            _labelPop = Mathf.Max(0f, _labelPop - dt * 3.2f);
            _label.SetColor(Color.Lerp(new Color(0.2f, 0.6f, 1f), _label.CurrentColor, _labelPop));

            _label.SetText(NumberFormat.Short(_swarm.Count));
            float scale = (1f + Mathf.Min(0.6f, _swarm.Radius * 0.12f)) * (1f + Mathf.Sin(_labelPop * Mathf.PI) * 0.3f);
            _label.transform.localScale = Vector3.one * scale;
            _label.transform.position = _swarm.Center + new Vector3(0f, LabelHeight * scale * 0.85f + _swarm.Radius * 0.35f, 0f);
        }
    }
}
