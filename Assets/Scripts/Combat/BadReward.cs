using NitroRhythm.Core;
using NitroRhythm.Player;
using UnityEngine;

namespace NitroRhythm.Combat
{
    /// <summary>
    /// "PremioMalo" / BadReward projectile.
    /// Flight is fully kinematic (deterministic Vector3.Lerp + Mathf.Sin arc),
    /// then physics and the collider are re-enabled when it lands, turning it
    /// into a static hazard until its deferred destruction (6 s).
    /// A proximity check keeps the attack readable and reliable in playtests.
    /// </summary>
    public class BadReward : MonoBehaviour
    {
        [Header("Damage")]
        [SerializeField] private float _damage = 15f;
        [SerializeField] private float _speed = 45f;
        [SerializeField] private float _arcHeight = 6f;
        [SerializeField] private float _lifetime = 6f;
        [SerializeField] private float _hitRadius = 1.4f;

        /// <summary>Firing entity; the projectile ignores its own colliders.</summary>
        public GameObject Owner;

        private Vector3 _start;
        private Vector3 _target;
        private float _flightTime;
        private float _elapsed;
        private bool _flying;
        private bool _configured;

        private Rigidbody _rb;
        private Collider _collider;

        public float Damage { get => _damage; set => _damage = value; }

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _collider = GetComponent<Collider>();
        }

        private void Start()
        {
            // Auto-destroy safety net even if never configured.
            Destroy(gameObject, _lifetime);
        }

        /// <summary>Configures the parabolic flight from a launch point to a target point.</summary>
        public void Launch(Vector3 start, Vector3 target, float arcHeight)
        {
            _start = start;
            _target = target;
            _arcHeight = arcHeight;
            _flightTime = Mathf.Max(0.15f, Vector3.Distance(start, target) / Mathf.Max(1f, _speed));
            _elapsed = 0f;
            _flying = true;
            _configured = true;

            transform.position = start;

            if (_rb != null)
            {
                _rb.isKinematic = true;
                _rb.useGravity = false;
            }
            if (_collider != null)
            {
                _collider.enabled = false;
            }
        }

        /// <summary>Legacy entry point used by ObstacleLauncher.</summary>
        public void SetTargetPosition(Vector3 target)
        {
            Launch(transform.position, target, _arcHeight);
        }

        private void Update()
        {
            if (!_flying || !_configured) return;

            _elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(_elapsed / _flightTime);

            Vector3 position = Vector3.Lerp(_start, _target, t);
            position.y += Mathf.Sin(t * Mathf.PI) * _arcHeight;
            transform.position = position;

            if (CheckProximityHit()) return;

            if (t >= 1f)
            {
                Land();
            }
        }

        private bool CheckProximityHit()
        {
            if (LevelManager.Instance == null) return false;

            foreach (PlayerKartController player in LevelManager.Instance.Players)
            {
                if (player == null) continue;
                if (Owner != null && player.gameObject == Owner) continue;

                if (Vector3.Distance(transform.position, player.transform.position) <= _hitRadius)
                {
                    player.TakeDamage(_damage);
                    Destroy(gameObject);
                    return true;
                }
            }

            return false;
        }

        private void Land()
        {
            _flying = false;
            if (_collider != null) _collider.enabled = true;
            if (_rb != null)
            {
                _rb.isKinematic = false;
                _rb.useGravity = true;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (Owner != null && (other.transform == Owner.transform || other.transform.IsChildOf(Owner.transform))) return;

            if (other.TryGetComponent(out IDamageable damageable))
            {
                damageable.TakeDamage(_damage);
                Destroy(gameObject);
            }
        }
    }
}
