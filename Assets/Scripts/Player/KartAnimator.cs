using NitroRhythm.Core;
using UnityEngine;

namespace NitroRhythm.Player
{
    /// <summary>
    /// Procedural animation for karts and pilots (the FBX models are static meshes without bones):
    /// body roll into turns, pitch on acceleration/jumps, suspension bob, squash on landing and stretch
    /// on take-off, a wobble when hit, a spin-out during K.O. and a boost shimmer. The pilot leans into
    /// the turn, looks where it steers and breathes when idle.
    ///
    /// The physics root is scaled non-uniformly (collider shape), and rotating a child of a non-uniform
    /// parent shears it. So the visuals are moved under Rig (inverse scale → identity world matrix) →
    /// Pivot (rotation/offset/squash). The models keep their names, so a deep search still finds them.
    /// </summary>
    [DefaultExecutionOrder(50)]
    public class KartAnimator : MonoBehaviour
    {
        [SerializeField] private float _maxRoll = 7f;
        [SerializeField] private float _maxAccelPitch = 4f;
        [SerializeField] private float _maxAirPitch = 12f;

        private Transform _rig;
        private Transform _pivot;
        private Transform _pilot;
        private PilotAnimator _pilotAnim;

        // Wheel bones of the rigged kart FBX (Wheel_FL/FR/RL/RR): spin with speed, front ones steer.
        private Transform[] _wheels = new Transform[0];
        private Quaternion[] _wheelRest = new Quaternion[0];
        private bool[] _wheelFront = new bool[0];
        private float _wheelSpin;
        private const float WheelRadius = 0.65f;   // world units at the normalized kart size
        private Vector3 _pilotBase;

        private Rigidbody _rb;
        private PlayerKartController _kart;
        private KartHealthSpeed _health;

        private float _roll, _pitch, _lastYaw, _lastForwardSpeed;
        private bool _wasGrounded = true;
        private float _verticalSpeed;

        // Squash & stretch spring (1 = rest).
        private float _squash = 1f, _squashVelocity;

        // Hit wobble and K.O. spin.
        private float _wobble;
        private float _spin;
        private bool _wasDown;

        public Transform Pivot => _pivot;

        /// <summary>Moves the kart and pilot models of a kart entity under an animated pivot.</summary>
        public static KartAnimator Install(GameObject entity)
        {
            KartAnimator animator = entity.GetComponent<KartAnimator>();
            if (animator == null) animator = entity.AddComponent<KartAnimator>();
            animator.BuildRig();
            return animator;
        }

        private void BuildRig()
        {
            if (_rig != null) return;

            _rig = new GameObject("AnimRig").transform;
            _rig.SetParent(transform, false);
            _pivot = new GameObject("AnimPivot").transform;
            _pivot.SetParent(_rig, false);
            UpdateRigScale();

            // Collect first, then reparent (changing the hierarchy while iterating skips children).
            System.Collections.Generic.List<ModelNormalizer> models = new System.Collections.Generic.List<ModelNormalizer>();
            foreach (Transform child in transform)
            {
                ModelNormalizer normalizer = child.GetComponent<ModelNormalizer>();
                if (normalizer != null) models.Add(normalizer);
            }

            foreach (ModelNormalizer normalizer in models)
            {
                Transform model = normalizer.transform;
                model.SetParent(_pivot, false);
                // The pivot has world scale 1, so the model simply keeps its normalized size and the
                // squash applied to the pivot shows (instead of being compensated away).
                normalizer.InheritParentScale = true;
                if (normalizer.UseWorldOffset)
                {
                    model.localPosition = normalizer.WorldOffset;
                    _pilot = model;
                    _pilotBase = normalizer.WorldOffset;
                }
                else
                {
                    model.localPosition = Vector3.zero;
                    KartWheelRig.Build(model.gameObject);   // wheel bones for the single-mesh kart
                }
            }
            FindWheelsAndPilot();
        }

        private void FindWheelsAndPilot()
        {
            System.Collections.Generic.List<Transform> wheels = new System.Collections.Generic.List<Transform>();
            foreach (Transform t in _pivot.GetComponentsInChildren<Transform>(true))
            {
                if (t.name.StartsWith("Wheel_") && t.name.Length == 8) wheels.Add(t);
            }
            _wheels = wheels.ToArray();
            _wheelRest = new Quaternion[_wheels.Length];
            _wheelFront = new bool[_wheels.Length];
            for (int i = 0; i < _wheels.Length; i++)
            {
                _wheelRest[i] = _wheels[i].localRotation;
                _wheelFront[i] = _wheels[i].name[6] == 'F';
            }
            if (_pilot != null) _pilotAnim = PilotAnimator.Attach(_pilot, _pilot.name.Replace("_Pilot", ""));
        }

        public int WheelCount => _wheels.Length;
        public PilotAnimator Pilot => _pilotAnim;

        private void UpdateRigScale()
        {
            _rig.localScale = InverseLocal();
        }

        private Vector3 InverseLocal()
        {
            // Undo only this transform's own scale (karts are root objects; a parent, if any, is uniform).
            Vector3 s = transform.localScale;
            return new Vector3(1f / Mathf.Max(0.0001f, s.x), 1f / Mathf.Max(0.0001f, s.y), 1f / Mathf.Max(0.0001f, s.z));
        }

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _kart = GetComponent<PlayerKartController>();
            _health = GetComponent<KartHealthSpeed>();
            _lastYaw = transform.eulerAngles.y;
        }

        private void Start()
        {
            if (_rb == null) _rb = GetComponent<Rigidbody>();
            if (_kart == null) _kart = GetComponent<PlayerKartController>();
            if (_health == null) _health = GetComponent<KartHealthSpeed>();
            KartHealthSpeed.Hit += OnHit;
        }

        private void OnDestroy()
        {
            KartHealthSpeed.Hit -= OnHit;
        }

        private void OnHit(KartHealthSpeed who, float damage)
        {
            if (who != null && who.gameObject == gameObject)
            {
                _wobble = 1f;
                if (_pilotAnim != null) _pilotAnim.PlayHit();
            }
        }

        private void LateUpdate()
        {
            if (_pivot == null) return;
            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            UpdateRigScale();

            Vector3 velocity = _rb != null ? _rb.linearVelocity : Vector3.zero;
            float forwardSpeed = Vector3.Dot(velocity, transform.forward);
            float speed01 = Mathf.Clamp01(Mathf.Abs(forwardSpeed) / 40f);
            bool grounded = _kart != null ? _kart.IsGrounded : Mathf.Abs(velocity.y) < 1.5f;

            // ---- roll into turns (from the yaw rate, so it also works for bots and the villain)
            float yaw = transform.eulerAngles.y;
            float yawRate = Mathf.DeltaAngle(_lastYaw, yaw) / dt;
            _lastYaw = yaw;
            float targetRoll = Mathf.Clamp(-yawRate * 0.06f * (0.3f + speed01), -_maxRoll, _maxRoll);
            _roll = Mathf.Lerp(_roll, targetRoll, 1f - Mathf.Exp(-10f * dt));

            // ---- pitch: squat on acceleration, nose up rising / down falling in the air
            float accel = (forwardSpeed - _lastForwardSpeed) / dt;
            _lastForwardSpeed = forwardSpeed;
            _verticalSpeed = Mathf.Lerp(_verticalSpeed, velocity.y, 1f - Mathf.Exp(-12f * dt));
            float targetPitch = grounded
                ? Mathf.Clamp(-accel * 0.12f, -_maxAccelPitch, _maxAccelPitch)
                : Mathf.Clamp(-_verticalSpeed * 1.1f, -_maxAirPitch, _maxAirPitch);
            if (_kart != null && _kart.IsBoosting) targetPitch -= 2f;
            _pitch = Mathf.Lerp(_pitch, targetPitch, 1f - Mathf.Exp(-8f * dt));

            // ---- take-off stretch and landing squash
            if (grounded && !_wasGrounded)
            {
                float impact = Mathf.Clamp01(-_verticalSpeed / 18f);
                if (impact > 0.1f) { _squash = 1f - 0.22f * impact; _squashVelocity = 0f; }
            }
            else if (!grounded && _wasGrounded && velocity.y > 4f)
            {
                _squash = 1.12f;
                _squashVelocity = 0f;
            }
            _wasGrounded = grounded;
            // Damped spring back to 1.
            float spring = (1f - _squash) * 260f - _squashVelocity * 14f;
            _squashVelocity += spring * dt;
            _squash += _squashVelocity * dt;

            // ---- suspension bob on the ground
            float t = Time.time;
            float bob = grounded ? Mathf.Sin(t * (9f + speed01 * 14f)) * 0.035f * speed01 : 0f;
            if (_kart != null && _kart.IsBoosting) bob += (Mathf.PerlinNoise(t * 40f, 0.5f) - 0.5f) * 0.04f;

            // ---- hit wobble
            _wobble = Mathf.MoveTowards(_wobble, 0f, dt * 2.2f);
            float wobbleRoll = Mathf.Sin(t * 38f) * 9f * _wobble;
            float wobbleYaw = Mathf.Sin(t * 29f + 1f) * 6f * _wobble;

            // ---- K.O.: spin out, then settle while waiting to revive
            bool down = _health != null && _health.IsDown;
            if (down && !_wasDown) _spin = 0f;
            if (down)
            {
                float elapsed = KartHealthSpeed.ReviveDelay - _health.ReviveTimeLeft;
                float spinSpeed = Mathf.Lerp(900f, 0f, Mathf.Clamp01(elapsed / 1.2f));
                _spin += spinSpeed * dt;
            }
            else
            {
                _spin = Mathf.LerpAngle(_spin, 0f, 1f - Mathf.Exp(-6f * dt));
            }
            _wasDown = down;
            float koTilt = down ? 10f : 0f;

            _pivot.localRotation = Quaternion.Euler(_pitch + koTilt, _spin + wobbleYaw, _roll + wobbleRoll);
            _pivot.localPosition = new Vector3(0f, bob, 0f);
            float squash = Mathf.Clamp(_squash, 0.7f, 1.25f);
            float bulge = 1f / Mathf.Sqrt(squash);
            _pivot.localScale = new Vector3(bulge, squash, bulge);

            // ---- wheels: roll with the ground speed; the front pair steers into the turn
            if (_wheels.Length > 0)
            {
                _wheelSpin = Mathf.Repeat(_wheelSpin + forwardSpeed / WheelRadius * Mathf.Rad2Deg * dt, 360f);
                float steerDeg = Mathf.Clamp(-_roll * 3f, -24f, 24f);
                for (int i = 0; i < _wheels.Length; i++)
                {
                    Transform w = _wheels[i];
                    Transform parent = w.parent;
                    Vector3 axle = parent.InverseTransformDirection(transform.right).normalized;
                    Vector3 up = parent.InverseTransformDirection(transform.up).normalized;
                    Quaternion spin = Quaternion.AngleAxis(_wheelSpin, axle);
                    Quaternion steering = _wheelFront[i] ? Quaternion.AngleAxis(steerDeg, up) : Quaternion.identity;
                    w.localRotation = steering * spin * _wheelRest[i];
                }
            }

            // ---- pilot: skeletal clips (steer, boost, hit, K.O.) + a light whole-body lean
            if (_pilotAnim != null)
            {
                _pilotAnim.Steer = _roll / Mathf.Max(0.01f, _maxRoll);
                _pilotAnim.Boosting = _kart != null && _kart.IsBoosting;
                _pilotAnim.Down = down;
            }
            if (_pilot != null)
            {
                bool skeletal = _pilotAnim != null && _pilotAnim.HasClips;
                float lean = _roll * (skeletal ? 0.5f : 1.2f);
                float look = skeletal ? 0f : Mathf.Clamp(-_roll * 2.2f, -14f, 14f);
                float slump = down && !skeletal ? 18f : 0f;
                _pilot.localRotation = Quaternion.Euler(slump - _pitch * 0.4f, look, lean);
                float breathe = Mathf.Sin(t * 2.4f) * 0.025f * (1f - speed01);
                _pilot.localPosition = _pilotBase + new Vector3(0f, breathe, 0f);
            }
        }
    }
}
