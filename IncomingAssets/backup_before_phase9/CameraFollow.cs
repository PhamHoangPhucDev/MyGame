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
        [SerializeField] Vector3 offset = new Vector3(0f, 0.5f, -10f);
        [SerializeField] bool lockZ = true;
        [SerializeField] bool useBounds = true;
        [SerializeField] Vector2 boundsMin = new Vector2(-14f, -7f);
        [SerializeField] Vector2 boundsMax = new Vector2(14f, 11f);

        Vector3 _velocity;
        Vector3 _basePos;
        bool _initialized;
        float _shakeAmp;
        float _shakeDuration;
        float _shakeElapsed;
        float _shakeSeed;
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

            Vector3 shakeOffset = Vector3.zero;
            if (_shakeElapsed < _shakeDuration)
            {
                _shakeElapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(_shakeElapsed / Mathf.Max(0.0001f, _shakeDuration));
                float falloff = 1f - t;
                float n1 = Mathf.PerlinNoise(_shakeSeed, Time.unscaledTime * 28f) * 2f - 1f;
                float n2 = Mathf.PerlinNoise(_shakeSeed + 17f, Time.unscaledTime * 28f) * 2f - 1f;
                shakeOffset = new Vector3(n1, n2, 0f) * (_shakeAmp * falloff);
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
    }
}
