using UnityEngine;

namespace NitroRhythm.Player
{
    /// <summary>
    /// Simple bot AI that drives a PlayerKartController (scheme Bot):
    /// races forward, steers back toward the track center, and jumps over
    /// gaps / obstacles detected ahead.
    /// </summary>
    public class BotKartAI : MonoBehaviour
    {
        [Header("Driving")]
        [SerializeField] private float _centerSteering = 0.4f;
        [SerializeField] private float _jumpCooldown = 0.25f;
        [SerializeField] private float _obstacleLookAhead = 8f;
        [SerializeField] private float _gapCheckDepth = 3f;
        [SerializeField] private float _gapProbeSeconds = 0.16f;

        private PlayerKartController _kart;
        private float _lastJumpTime;

        private void Awake()
        {
            _kart = GetComponent<PlayerKartController>();
        }

        private void Update()
        {
            if (_kart == null)
            {
                _kart = GetComponent<PlayerKartController>();
                return;
            }

            // Always throttle forward; steer back toward the center of the track.
            float vertical = 1f;
            float horizontal = -Mathf.Clamp(transform.position.z * _centerSteering, -1f, 1f);

            if (_kart.IsGrounded && Time.time - _lastJumpTime > _jumpCooldown && IsObstacleAhead())
            {
                _kart.RequestJump();
                _lastJumpTime = Time.time;
            }

            _kart.SetDriveInput(vertical, horizontal);
        }

        private bool IsObstacleAhead()
        {
            Vector3 origin = transform.position + Vector3.up * 0.2f;

            // Probe the ground AHEAD of the kart (distance grows with speed, so the
            // jump starts right at the edge instead of after already falling).
            float probeDistance = Mathf.Clamp(Mathf.Abs(_kart.CurrentSpeed) * _gapProbeSeconds, 2.5f, 12f);
            Vector3 edge = origin + transform.forward * probeDistance;
            if (!Physics.Raycast(edge, Vector3.down, _gapCheckDepth, ~0, QueryTriggerInteraction.Ignore))
            {
                return true;
            }

            // A wall / spinning bar / other obstacle ahead — jump over it.
            if (Physics.Raycast(origin, transform.forward, _obstacleLookAhead, ~0, QueryTriggerInteraction.Ignore))
            {
                return true;
            }

            return false;
        }
    }
}
