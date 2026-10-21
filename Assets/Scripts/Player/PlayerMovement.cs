using UnityEngine;
using UnityEngine.InputSystem;

namespace GameplayPrototype
{
    /// <summary>
    /// 8-direction top-down movement for Stage 1 (WASD / Arrow keys).
    /// Supports brief velocity overrides for dash skills.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerMovement : MonoBehaviour
    {
        [SerializeField] float moveSpeed = 4.5f;
        [SerializeField] bool normalizeDiagonal = true;

        Rigidbody2D _rb;
        Vector2 _input;
        Vector2 _externalInput;
        bool _useExternalInput;
        float _overrideTimer;
        Vector2 _overrideVelocity;
        bool _movementLocked;

        public Vector2 MoveInput => _input;
        public bool IsMoving => _input.sqrMagnitude > 0.0001f;
        public float MoveSpeed => moveSpeed;
        /// <summary>Class stat (R2C ClassCombat, Sandbox). 1 = scene value, unchanged everywhere else.</summary>
        public float SpeedMultiplier { get; set; } = 1f;
        public float HasteMultiplier { get; set; } = 1f;

        public void SetExternalInput(Vector2 input, bool enabled = true)
        {
            _externalInput = input;
            _useExternalInput = enabled;
        }

        public void ClearExternalInput()
        {
            _useExternalInput = false;
            _externalInput = Vector2.zero;
        }

        public void OverrideVelocity(Vector2 velocity, float duration)
        {
            _overrideVelocity = velocity;
            _overrideTimer = Mathf.Max(0f, duration);
        }

        public void SetMovementLocked(bool locked)
        {
            _movementLocked = locked;
            if (locked && _overrideTimer <= 0f)
                _rb.linearVelocity = Vector2.zero;
        }

        void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _rb.gravityScale = 0f;
            _rb.freezeRotation = true;
            _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            _rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        }

        void Update()
        {
            if (_movementLocked && _overrideTimer <= 0f)
            {
                _input = Vector2.zero;
                return;
            }

            _input = _useExternalInput ? _externalInput : ReadMoveInput();
            if (normalizeDiagonal && _input.sqrMagnitude > 1f)
                _input.Normalize();
        }

        void FixedUpdate()
        {
            if (_overrideTimer > 0f)
            {
                _overrideTimer -= Time.fixedDeltaTime;
                _rb.linearVelocity = _overrideVelocity;
                return;
            }

            if (_movementLocked)
            {
                _rb.linearVelocity = Vector2.zero;
                return;
            }

            _rb.linearVelocity = _input * moveSpeed * SpeedMultiplier * HasteMultiplier;
        }

        static Vector2 ReadMoveInput()
        {
            Vector2 input = Vector2.zero;

            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) input.x -= 1f;
                if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) input.x += 1f;
                if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) input.y += 1f;
                if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) input.y -= 1f;
            }

            return input;
        }
    }
}
