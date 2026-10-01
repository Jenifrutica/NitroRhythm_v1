using UnityEngine;

namespace NitroRhythm.Combat
{
    /// <summary>
    /// Obstacle launcher that identifies nearby targets,
    /// tracks them and fires projectiles with parabolic trajectory.
    /// </summary>
    public class ObstacleLauncher : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private float _lifetimeRange = 25f;
        [SerializeField] private float _trackingSpeed = 5f;
        [SerializeField] private float _fireCycle = 1.5f;
        [SerializeField] private GameObject _badRewardPrefab;

        public GameObject ProjectilePrefab { get => _badRewardPrefab; set => _badRewardPrefab = value; }

        private float _lastFireTime;
        private ITargetSelectable[] _activeTargets;

        private void Awake()
        {
            Collider[] hitColliders = Physics.OverlapSphere(transform.position, _lifetimeRange);
            _activeTargets = new ITargetSelectable[hitColliders.Length];

            int count = 0;
            foreach (Collider col in hitColliders)
            {
                if (col.TryGetComponent<ITargetSelectable>(out ITargetSelectable target))
                {
                    if (target.IsValidTarget)
                    {
                        _activeTargets[count] = target;
                        count++;
                    }
                }
            }

            if (count < _activeTargets.Length)
            {
                ITargetSelectable[] arrayToResize = new ITargetSelectable[count];
                System.Array.Copy(_activeTargets, arrayToResize, count);
                _activeTargets = arrayToResize;
            }
        }

        private void Update()
        {
            if (Time.time - _lastFireTime > _fireCycle)
            {
                AttemptFire();
            }
        }

        private void AttemptFire()
        {
            Transform target = GetClosestTarget();

            if (target != null)
            {
                OrientTowardsTarget(target);
                LaunchProjectile(target);
                Debug.Log($"[{name}] Firing grenade at {target.name}");
            }
            else
            {
                Debug.LogWarning($"[{name}] No available targets to shoot at");
            }

            _lastFireTime = Time.time;
        }

        private Transform GetClosestTarget()
        {
            Transform closestTarget = null;
            float minDistance = Mathf.Infinity;

            foreach (ITargetSelectable target in _activeTargets)
            {
                if (!target.IsValidTarget) continue;

                Transform targetTransform = target.Transform;
                float distance = Vector3.Distance(transform.position, targetTransform.position);

                if (distance < minDistance && distance <= _lifetimeRange)
                {
                    minDistance = distance;
                    closestTarget = targetTransform;
                }
            }

            return closestTarget;
        }

        private void OrientTowardsTarget(Transform target)
        {
            Vector3 directionToTarget = (target.position - transform.position).normalized;
            Quaternion targetRotation = Quaternion.LookRotation(directionToTarget, Vector3.up);

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                Time.deltaTime * _trackingSpeed
            );
        }

        private void LaunchProjectile(Transform target)
        {
            if (_badRewardPrefab == null)
            {
                Debug.LogError($"[{name}] The BadReward prefab is not assigned");
                return;
            }

            GameObject projectileInstance = Instantiate(_badRewardPrefab, transform.position, transform.rotation);
            BadReward component = projectileInstance.GetComponent<BadReward>();

            if (component != null)
            {
                component.SetTargetPosition(target.position);
            }
            else
            {
                Debug.LogError($"[{name}] The BadReward prefab does not have BadReward component");
            }
        }

        public void MarkTargetAsActive(ITargetSelectable target, bool isActive)
        {
            if (target == null) return;

            if (isActive && !System.Array.Exists(_activeTargets, t => t == target))
            {
                System.Array.Resize(ref _activeTargets, _activeTargets.Length + 1);
                _activeTargets[^1] = target;
            }
            else if (!isActive)
            {
                _activeTargets = System.Array.FindAll(_activeTargets, t => t != target);
            }
        }
    }
}