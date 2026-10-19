using UnityEngine;

namespace GameplayPrototype
{
    /// <summary>
    /// Smooth follow with shake applied AFTER SmoothDamp into a separate base position.
    /// Optional soft world bounds keep landscape framing on the camp map.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField] Transform target;
        [SerializeField] float smoothTime = 0.1f;
        // Landscape 16:9 — slight downward bias keeps HUD clear of the hero silhouette.
        [SerializeField] Vector3 offset = new Vector3(0f, -0.45f, -10f);
        [SerializeField] bool lockZ = true;
        [SerializeField] bool useBounds = true;
        [SerializeField] Vector2 boundsMin = new Vector2(-14f, -12f);
        [SerializeField] Vector2 boundsMax = new Vector2(14f, 14f);

        Vector3 _velocity;
        Vector3 _basePos;
        bool _initialized;
        float _shakeAmp;
        float _shakeDuration;
        float _shakeElapsed;
        float _shakeSeed;
        int _shakeFrame = -1;
        Camera _cam;

        public Vector3 BasePosition => _basePos;
        public Vector3 FollowOffset => offset;

        public void SetTarget(Transform followTarget)
        {
            target = followTarget;
            _initialized = false;
        }

        public void SetBounds(Vector2 min, Vector2 max)
        {
            useBounds = true;
            boundsMin = min;
            boundsMax = max;
        }

        public void Shake(float amplitude, float duration)
        {
            if (amplitude <= 0f || duration <= 0f) return;
            if (Time.frameCount == _shakeFrame)
            {
                _shakeAmp = Mathf.Max(_shakeAmp, amplitude);
                _shakeDuration = Mathf.Max(_shakeDuration, duration);
                return;
            }
            _shakeFrame = Time.frameCount;
            if (_shakeElapsed < _shakeDuration && _shakeAmp > amplitude)
            {
                _shakeDuration = Mathf.Max(_shakeDuration - _shakeElapsed, duration) + _shakeElapsed;
                return;
            }
            _shakeAmp = amplitude;
            _shakeDuration = duration;
            _shakeElapsed = 0f;
            _shakeSeed = Random.value * 100f;
        }

        void Awake()
        {
            _cam = GetComponent<Camera>();
        }

        void LateUpdate()
        {
            if (target == null)
                return;

            Vector3 desired = target.position + offset;
            if (lockZ)
                desired.z = offset.z;

            if (!_initialized)
            {
                _basePos = desired;
                _velocity = Vector3.zero;
                _initialized = true;
            }

            _basePos = Vector3.SmoothDamp(_basePos, desired, ref _velocity, smoothTime);
            if (useBounds)
                ClampToBounds(ref _basePos);
            // Authored bounds can leave the hero under the HUD or off-screen on wide maps.
            KeepTargetOnScreen(ref _basePos);

            Vector3 shakeOffset = Vector3.zero;
            if (_shakeElapsed < _shakeDuration)
            {
                _shakeElapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(_shakeElapsed / Mathf.Max(0.0001f, _shakeDuration));
                float falloff = 1f - t;
                float n1 = Mathf.PerlinNoise(_shakeSeed, Time.unscaledTime * 28f) * 2f - 1f;
                float n2 = Mathf.PerlinNoise(_shakeSeed + 17f, Time.unscaledTime * 28f) * 2f - 1f;
                shakeOffset = new Vector3(n1, n2, 0f) * (_shakeAmp * falloff);
                shakeOffset.x = Mathf.Round(shakeOffset.x * 16f) / 16f;
                shakeOffset.y = Mathf.Round(shakeOffset.y * 16f) / 16f;
            }

            Vector3 finalPos = _basePos + shakeOffset;
            if (lockZ)
                finalPos.z = offset.z;
            transform.position = finalPos;
        }

        void ClampToBounds(ref Vector3 pos)
        {
            if (_cam == null) _cam = GetComponent<Camera>();
            float halfH = _cam != null ? _cam.orthographicSize : 8f;
            float halfW = halfH * (_cam != null ? _cam.aspect : 16f / 9f);
            float minX = boundsMin.x + halfW;
            float maxX = boundsMax.x - halfW;
            float minY = boundsMin.y + halfH;
            float maxY = boundsMax.y - halfH;
            if (minX > maxX) pos.x = (boundsMin.x + boundsMax.x) * 0.5f;
            else pos.x = Mathf.Clamp(pos.x, minX, maxX);
            if (minY > maxY) pos.y = (boundsMin.y + boundsMax.y) * 0.5f;
            else pos.y = Mathf.Clamp(pos.y, minY, maxY);
        }

        /// <summary>
        /// Pull the camera so the follow target stays inside a safe frame
        /// (above the bottom HUD, inside the side edges). Overrides clamp when needed.
        /// </summary>
        void KeepTargetOnScreen(ref Vector3 pos)
        {
            if (target == null) return;
            if (_cam == null) _cam = GetComponent<Camera>();
            float halfH = _cam != null ? _cam.orthographicSize : 8f;
            float halfW = halfH * (_cam != null ? _cam.aspect : 16f / 9f);
            const float marginX = 3f;
            const float marginTop = 2.2f;
            const float marginBottom = 4f; // keep the hero above the landscape hotbar

            float heroX = target.position.x;
            float heroY = target.position.y;
            float minCamX = heroX - halfW + marginX;
            float maxCamX = heroX + halfW - marginX;
            // Camera must sit low enough that the body clears the bottom HUD,
            // and high enough that the body clears the top of the frame.
            float minCamY = heroY - halfH + marginTop;
            float maxCamY = heroY + halfH - marginBottom;
            if (pos.x < minCamX) pos.x = minCamX;
            if (pos.x > maxCamX) pos.x = maxCamX;
            if (pos.y < minCamY) pos.y = minCamY;
            if (pos.y > maxCamY) pos.y = maxCamY;
        }
    }
}
