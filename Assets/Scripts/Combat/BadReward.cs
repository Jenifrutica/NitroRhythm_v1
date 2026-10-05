using NitroRhythm.Core;
using NitroRhythm.Player;
using UnityEngine;

namespace NitroRhythm.Combat
{
    /// <summary>
    /// "PremioMalo" / BadReward projectile thrown by the villain.
    /// Flight is kinematic (Lerp + Sin arc) toward a PREDICTED point, with a pulsing red marker
    /// on the ground so the player can read and dodge it. On landing it explodes (area damage);
    /// a mine stays armed on the track for a while instead.
    /// </summary>
    public class BadReward : MonoBehaviour
    {
        [Header("Damage")]
        [SerializeField] private float _damage = 15f;
        [SerializeField] private float _flightTime = 1.1f;
        [SerializeField] private float _arcHeight = 3.5f;
        [SerializeField] private float _impactRadius = 2.4f;
        [SerializeField] private float _flightHitRadius = 1.6f;
        [SerializeField] private float _mineDuration = 6f;

        /// <summary>Firing entity; the projectile ignores it.</summary>
        public GameObject Owner;

        /// <summary>A mine lingers armed after landing instead of exploding at once.</summary>
        public bool IsMine;

        private Vector3 _start;
        private Vector3 _target;
        private float _elapsed;
        private float _armedTime;
        private bool _flying;
        private bool _landed;
        private GameObject _marker;
        private Material _markerMaterial;
        private Rigidbody _rb;
        private Collider _collider;

        public float Damage { get => _damage; set => _damage = value; }

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _collider = GetComponent<Collider>();
        }

        /// <summary>Configures the parabolic flight from a launch point to a target point.</summary>
        public void Launch(Vector3 start, Vector3 target, float arcHeight, float flightTime = 1.1f)
        {
            _start = start;
            _target = target;
            _arcHeight = arcHeight;
            _flightTime = Mathf.Max(0.3f, flightTime);
            _elapsed = 0f;
            _flying = true;
            _landed = false;

            transform.position = start;

            if (_rb != null)
            {
                _rb.isKinematic = true;
                _rb.useGravity = false;
            }
            if (_collider != null) _collider.enabled = false;

            CreateMarker();
            Destroy(gameObject, _flightTime + _mineDuration + 2f);
        }

        /// <summary>Legacy entry point used by ObstacleLauncher.</summary>
        public void SetTargetPosition(Vector3 target)
        {
            Launch(transform.position, target, _arcHeight, _flightTime);
        }

        private void CreateMarker()
        {
            _marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            _marker.name = "ImpactMarker";
            _marker.transform.position = new Vector3(_target.x, _target.y + 0.06f, _target.z);
            _marker.transform.localScale = new Vector3(_impactRadius * 2f, 0.02f, _impactRadius * 2f);
            Collider c = _marker.GetComponent<Collider>();
            if (c != null) Destroy(c);

            _markerMaterial = VisualEntityFactory.CreateEmissiveMaterial(new Color(1f, 0.15f, 0.1f), 2f);
            _marker.GetComponent<Renderer>().sharedMaterial = _markerMaterial;
        }

        private void Update()
        {
            if (_flying)
            {
                FlightStep();
            }
            else if (_landed && IsMine)
            {
                _armedTime += Time.deltaTime;
                if (_marker != null)
                {
                    float pulse = 0.85f + Mathf.Sin(Time.time * 8f) * 0.15f;
                    _marker.transform.localScale = new Vector3(_impactRadius * 2f * pulse, 0.02f, _impactRadius * 2f * pulse);
                }

                if (DamagePlayersInRadius(_impactRadius * 0.8f) || _armedTime >= _mineDuration)
                {
                    Destroy(gameObject);
                }
            }
        }

        private void FlightStep()
        {
            _elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(_elapsed / _flightTime);

            Vector3 position = Vector3.Lerp(_start, _target, t);
            position.y += Mathf.Sin(t * Mathf.PI) * _arcHeight;
            transform.position = position;
            transform.Rotate(Vector3.up, 360f * Time.deltaTime, Space.World);

            if (_markerMaterial != null)
            {
                float blink = 0.5f + 0.5f * Mathf.Sin(t * 30f);
                _markerMaterial.SetColor("_EmissionColor", new Color(1f, 0.15f, 0.1f) * Mathf.Lerp(0.8f, 3f, blink * t));
            }

            if (t >= 1f) Land();
        }

        private void Land()
        {
            _flying = false;
            _landed = true;
            transform.position = _target + Vector3.up * 0.4f;

            if (IsMine)
            {
                if (_collider != null) _collider.enabled = false;
                return;
            }

            DamagePlayersInRadius(_impactRadius);
            SpawnBlastRing();
            Destroy(gameObject);
        }

        private bool DamagePlayersInRadius(float radius)
        {
            bool hitAny = false;
            LevelManager lm = LevelManager.Instance;
            if (lm == null) return false;

            foreach (PlayerKartController player in lm.Players)
            {
                if (player == null) continue;
                if (Owner != null && player.gameObject == Owner) continue;

                Vector3 d = player.transform.position - transform.position;
                d.y = 0f;
                if (d.magnitude <= radius && Mathf.Abs(player.transform.position.y - transform.position.y) < 3f)
                {
                    player.TakeDamage(_damage);
                    hitAny = true;
                }
            }

            return hitAny;
        }

        private void SpawnBlastRing()
        {
            GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = "BlastRing";
            ring.transform.position = _target + Vector3.up * 0.15f;
            ring.transform.localScale = new Vector3(0.5f, 0.05f, 0.5f);
            Collider c = ring.GetComponent<Collider>();
            if (c != null) Destroy(c);
            ring.GetComponent<Renderer>().sharedMaterial = VisualEntityFactory.CreateEmissiveMaterial(new Color(1f, 0.45f, 0.1f), 3f);
            ring.AddComponent<BlastRingFx>().MaxRadius = _impactRadius * 2.2f;
            NitroRhythm.Audio.SfxPlayer.Play(NitroRhythm.Audio.SfxLibrary.Impact, 0.35f, 1.4f);
        }

        private void OnDestroy()
        {
            if (_marker != null) Destroy(_marker);
        }
    }

    /// <summary>Expanding, fading ring shown where a BadReward exploded.</summary>
    public class BlastRingFx : MonoBehaviour
    {
        public float MaxRadius = 5f;
        private float _t;

        private void Update()
        {
            _t += Time.deltaTime / 0.35f;
            float r = Mathf.Lerp(0.5f, MaxRadius, Mathf.Clamp01(_t));
            transform.localScale = new Vector3(r, 0.05f, r);
            if (_t >= 1f) Destroy(gameObject);
        }
    }
}
