using UnityEngine;

namespace CrowdSurge
{
    /// <summary>Unified touch/mouse horizontal drag. Unity maps touches to mouse events, so one code path serves both.</summary>
    public sealed class PointerSteering
    {
        private const float Sensitivity = 1.15f;

        private float _lastX;
        private bool _dragging;

        /// <summary>World-space horizontal movement requested this frame.</summary>
        public float DeltaX { get; private set; }
        public bool PressedThisFrame { get; private set; }

        public void Update()
        {
            DeltaX = 0f;
            PressedThisFrame = Input.GetMouseButtonDown(0);

            if (PressedThisFrame)
            {
                _dragging = true;
                _lastX = Input.mousePosition.x;
            }
            else if (_dragging && Input.GetMouseButton(0))
            {
                float x = Input.mousePosition.x;
                DeltaX = (x - _lastX) / Screen.width * GameConstants.RoadWidth * Sensitivity;
                _lastX = x;
            }
            else
            {
                _dragging = false;
            }

            // Keyboard fallback for editor testing.
            DeltaX += Input.GetAxisRaw("Horizontal") * Time.unscaledDeltaTime * 7f;
        }
    }
}
