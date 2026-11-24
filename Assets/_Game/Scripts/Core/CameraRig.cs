using UnityEngine;

namespace CrowdSurge
{
    /// <summary>Third-person chase camera with shake, FOV punch and a victory swing around to face the crowd.</summary>
    public sealed class CameraRig : MonoBehaviour
    {
        private const float BaseFov = 58f;

        private Camera _camera;
        private CrowdController _crowd;
        private Vector3 _smoothedTarget;
        private float _shakeTime, _shakeDuration, _shakeMagnitude;
        private float _fovPunch;
        private bool _celebrating;
        private float _menuBlend, _menuTarget;
        private float _celebrateBlend;

        public static CameraRig Create(Camera camera, CrowdController crowd)
        {
            var rig = camera.gameObject.AddComponent<CameraRig>();
            rig._camera = camera;
            rig._crowd = crowd;
            camera.fieldOfView = BaseFov;
            rig._smoothedTarget = crowd.Center;
            rig.SnapToTarget();
            return rig;
        }

        public void Shake(float magnitude, float duration)
        {
            if (magnitude < _shakeMagnitude * Mathf.Clamp01(_shakeTime / Mathf.Max(0.01f, _shakeDuration))) return;
            _shakeMagnitude = magnitude;
            _shakeDuration = duration;
            _shakeTime = duration;
        }

        public void Punch(float amount) => _fovPunch = Mathf.Max(_fovPunch, amount * 6f);

        public void Celebrate() => _celebrating = true;

        /// <summary>Menu pose: the camera swings round in front of the crowd so it cheers at the player.</summary>
        public void SetMenuMode(bool on, bool instant = false)
        {
            _menuTarget = on ? 1f : 0f;
            if (instant) _menuBlend = _menuTarget;
        }

        public void SnapToTarget()
        {
            _smoothedTarget = _crowd.Center;
            ApplyPose(0f, true);
        }

        private void LateUpdate() => ApplyPose(Time.deltaTime, false);

        private void ApplyPose(float dt, bool snap)
        {
            Vector3 c = _crowd.Center;
            float follow = snap ? 1f : 1f - Mathf.Exp(-9f * dt);
            _smoothedTarget.z = Mathf.Lerp(_smoothedTarget.z, c.z, snap ? 1f : 1f - Mathf.Exp(-14f * dt));
            _smoothedTarget.x = Mathf.Lerp(_smoothedTarget.x, c.x * 0.55f, follow);

            float spread = Mathf.Min(_crowd.Radius, 4f);
            Vector3 chasePos = new Vector3(_smoothedTarget.x, 8.2f + spread * 0.8f, _smoothedTarget.z - 9.5f - spread * 0.9f);
            Quaternion chaseRot = Quaternion.Euler(36f, 0f, 0f);

            Vector3 pos = chasePos;
            Quaternion rot = chaseRot;

            if (_celebrating)
            {
                _celebrateBlend = Mathf.MoveTowards(_celebrateBlend, 1f, dt * 0.8f);
                float e = Mathf.SmoothStep(0f, 1f, _celebrateBlend);
                Vector3 look = c + Vector3.up * 1.2f;
                Vector3 celebratePos = c + new Vector3(2.5f, 3.6f, 11f);
                pos = Vector3.Lerp(chasePos, celebratePos, e);
                rot = Quaternion.Slerp(chaseRot, Quaternion.LookRotation(look - celebratePos), e);
            }

            if (_shakeTime > 0f)
            {
                _shakeTime -= dt;
                float falloff = Mathf.Clamp01(_shakeTime / Mathf.Max(0.01f, _shakeDuration));
                pos += Random.insideUnitSphere * (_shakeMagnitude * falloff);
            }

            _fovPunch = Mathf.MoveTowards(_fovPunch, 0f, dt * 22f);
            float fov = BaseFov + _fovPunch;

            _menuBlend = Mathf.MoveTowards(_menuBlend, snap ? _menuTarget : _menuTarget, Time.unscaledDeltaTime * 2.4f);
            float m = Mathf.SmoothStep(0f, 1f, _menuBlend);
            if (m > 0f)
            {
                Vector3 menuPos = c + new Vector3(0.6f, 2.2f, 7.6f);
                Quaternion menuRot = Quaternion.LookRotation(c + Vector3.up * 0.9f - menuPos);
                pos = Vector3.Lerp(pos, menuPos, m);
                rot = Quaternion.Slerp(rot, menuRot, m);
                fov = Mathf.Lerp(fov, 48f, m);
            }

            _camera.fieldOfView = fov;
            transform.SetPositionAndRotation(pos, rot);
        }
    }
}
