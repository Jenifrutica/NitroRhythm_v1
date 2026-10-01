using UnityEngine;
using NitroRhythm.Combat;
using NitroRhythm.Controls;

namespace NitroRhythm.Player
{
    /// <summary>
    /// Snappy arcade kart controller for the NitroRhythm Speedrun Obby.
    /// High-velocity movement (moveSpeed 35 / acceleration 60 / turnSpeed 160),
    /// Space-jump with a sphere ground check, custom gravity, continuous
    /// collision detection and speed-boost / debuff hooks.
    /// </summary>
    public class PlayerKartController : MonoBehaviour, IDamageable, ITargetSelectable
    {
        /// <summary>Which binding set the kart uses (P1: WASD/Space, P2: Arrows/Right Shift, P3: IJKL/Enter, Bot: AI-driven).</summary>
        public enum InputScheme
        {
            PlayerOne,
            PlayerTwo,
            PlayerThree,
            Bot
        }

        [Header("Arcade Kart Tuning")]
        [SerializeField] private InputScheme _inputScheme = InputScheme.PlayerOne;
        [SerializeField] private float _moveSpeed = 35f;    // high-velocity speedrun feel
        [SerializeField] private float _turnSpeed = 160f;   // degrees per second
        [SerializeField] private float _acceleration = 60f; // snappy responsiveness
        [SerializeField] private float _gravity = -25f;     // custom gravity for snappy falls
        [SerializeField] private float _groundDrag = 4f;
        [SerializeField] private float _airDrag = 0.5f;

        [Header("Jump")]
        [SerializeField] private float _jumpForce = 12f;    // upward velocity applied on Space

        [Header("Ground Check")]
        [SerializeField] private float _groundCheckRadius = 0.4f;
        [SerializeField] private float _groundCheckDistance = 0.85f;
        [SerializeField] private LayerMask _groundMask = ~0;

        [Header("Kart Scale")]
        [SerializeField] private Vector3 _kartScale = new Vector3(2.2f, 1.2f, 3.8f);

        private Rigidbody _rb;
        private bool _isGrounded;
        private float _boostTimer;
        private float _boostMultiplier = 1f;

        private IInputProvider _input;
        private KeyboardInputProvider _keyboard;
        private ScriptedInputProvider _scripted;
        private InputScheme _providerScheme;

        /// <summary>Temporary speed modifier (debuffs &lt; 1, boosts &gt; 1). Applied to target speed.</summary>
        public float SpeedMultiplier { get; set; } = 1f;

        /// <summary>True when the kart is resting on a solid surface (track/platform).</summary>
        public bool IsGrounded => _isGrounded;

        /// <summary>Which input scheme this kart uses (P1: WASD/Space, P2: Arrows/Right Shift).</summary>
        public InputScheme Scheme => _inputScheme;

        /// <summary>Assigns the input scheme at runtime (used by split-screen setup).</summary>
        public void SetInputScheme(InputScheme scheme)
        {
            _inputScheme = scheme;
            _input = null; // rebuilt lazily for the new scheme
        }

        /// <summary>Injects a custom input source (bot AI, replay, network).</summary>
        public void SetInputProvider(IInputProvider provider)
        {
            _input = provider;
        }

        /// <summary>External drive input for bot-controlled karts (InputScheme.Bot).</summary>
        public void SetDriveInput(float vertical, float horizontal)
        {
            EnsureInputProvider();
            _scripted?.SetDrive(vertical, horizontal);
        }

        /// <summary>Queues a jump for bot-controlled karts.</summary>
        public void RequestJump()
        {
            EnsureInputProvider();
            _scripted?.RequestJump();
        }

        /// <summary>Queues a fire action (villain shooting).</summary>
        public void RequestFire()
        {
            EnsureInputProvider();
            _scripted?.RequestFire();
        }

        /// <summary>Returns true once per fire press for this kart's input source.</summary>
        public bool ConsumeFireInput()
        {
            EnsureInputProvider();
            return _input != null && _input.ConsumeFire();
        }

        private void EnsureInputProvider()
        {
            if (_inputScheme == InputScheme.Bot)
            {
                if (_scripted == null) _scripted = new ScriptedInputProvider();
                _input = _scripted;
                return;
            }

            if (_input == null || _providerScheme != _inputScheme)
            {
                _keyboard = new KeyboardInputProvider(_inputScheme);
                _input = _keyboard;
                _providerScheme = _inputScheme;
            }
        }

        /// <summary>The kart's current forward speed in units/sec.</summary>
        public float CurrentSpeed { get; private set; }

        private void Awake()
        {
            // Standard vehicle proportions for the speedrun karts.
            transform.localScale = _kartScale;

            _rb = GetComponent<Rigidbody>();
            if (_rb == null)
            {
                _rb = gameObject.AddComponent<Rigidbody>();
            }

            _rb.mass = 50f;
            _rb.useGravity = false; // custom gravity for snappy arcade falls
            _rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            _rb.interpolation = RigidbodyInterpolation.Interpolate;
            _rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        }

        private void Update()
        {
            EnsureInputProvider();

            // Keyboard edge triggers are polled once per frame; physics reads
            // the latched values in FixedUpdate through the provider.
            if (_keyboard != null && _inputScheme != InputScheme.Bot)
            {
                _keyboard.Poll();
            }

            if (_boostTimer > 0f)
            {
                _boostTimer -= Time.deltaTime;
                if (_boostTimer < 0f)
                {
                    _boostTimer = 0f;
                }
            }
        }

        private void FixedUpdate()
        {
            UpdateGrounded();
            ApplyGravity();
            ApplySteering();
            ApplyDrive();
            ApplyJump();
        }

        private void UpdateGrounded()
        {
            // Sphere cast from just above the kart center down to the wheels, so
            // the ground check stays stable while driving over small pad edges.
            Vector3 origin = transform.position + Vector3.up * 0.1f;
            _isGrounded = Physics.SphereCast(
                origin, _groundCheckRadius, Vector3.down,
                out RaycastHit _, _groundCheckDistance, _groundMask,
                QueryTriggerInteraction.Ignore);
        }

        private void ApplyGravity()
        {
            _rb.AddForce(Vector3.up * _gravity, ForceMode.Acceleration);
        }

        private void ApplySteering()
        {
            float throttle = _input != null ? _input.Throttle : 0f;
            float steer = _input != null ? _input.Steer : 0f;

            float speedFactor = Mathf.Clamp01(Mathf.Abs(throttle));
            transform.Rotate(Vector3.up, steer * _turnSpeed * speedFactor * Time.fixedDeltaTime, Space.World);
        }

        private void ApplyDrive()
        {
            float throttle = _input != null ? _input.Throttle : 0f;

            float targetSpeed = _moveSpeed * SpeedMultiplier;
            if (_boostTimer > 0f)
            {
                targetSpeed *= _boostMultiplier;
            }

            if (Mathf.Abs(throttle) > 0.01f)
            {
                Vector3 forward = transform.forward;
                Vector3 flatVelocity = new Vector3(_rb.linearVelocity.x, 0f, _rb.linearVelocity.z);
                Vector3 intendedVelocity = forward * (targetSpeed * Mathf.Sign(throttle));
                Vector3 newFlat = Vector3.MoveTowards(flatVelocity, intendedVelocity, _acceleration * Time.fixedDeltaTime);
                _rb.linearVelocity = new Vector3(newFlat.x, _rb.linearVelocity.y, newFlat.z);
            }

            // Drag: strong on the ground, light in the air for jumps.
            _rb.linearDamping = _isGrounded ? _groundDrag : _airDrag;

            CurrentSpeed = Vector3.Dot(_rb.linearVelocity, transform.forward);
        }

        private void ApplyJump()
        {
            if (_input == null || !_input.ConsumeJump()) return;

            if (_isGrounded)
            {
                // Direct velocity set for a snappy, predictable arcade jump.
                _rb.linearVelocity = new Vector3(_rb.linearVelocity.x, _jumpForce, _rb.linearVelocity.z);
            }
        }

        /// <summary>
        /// Applies a temporary speed boost as a multiplier of the base speed
        /// (used by track speed pads and on-beat rhythm boosts).
        /// </summary>
        public void ApplySpeedBoost(float multiplier, float duration)
        {
            _boostMultiplier = Mathf.Max(multiplier, _boostMultiplier);
            _boostTimer = Mathf.Max(duration, _boostTimer);
        }

        /// <summary>
        /// Applies the character's 1..10 stats to the arcade tuning curves.
        /// </summary>
        public void ApplyCharacterStats(int speed, int grip, int power)
        {
            float s = Mathf.Clamp01(speed / 10f);
            float g = Mathf.Clamp01(grip / 10f);
            float p = Mathf.Clamp01(power / 10f);

            _moveSpeed = Mathf.Lerp(30f, 44f, s);
            _turnSpeed = Mathf.Lerp(130f, 200f, g);
            _jumpForce = Mathf.Lerp(11f, 15f, p * 0.6f + g * 0.4f);
        }

        public void TakeDamage(float amount)
        {
            KartHealthSpeed health = GetComponent<KartHealthSpeed>();
            if (health != null)
            {
                health.ApplyVillainHit(amount);
                return;
            }

            Debug.Log($"[{name}] Took {amount} damage!");
        }

        public Transform Transform => transform;

        public bool IsValidTarget => _rb != null && _rb.gameObject.activeSelf && _rb.linearVelocity.magnitude > 0.5f;
    }
}
