using UnityEngine;
using NitroRhythm.Player;

namespace NitroRhythm.Core
{
    /// <summary>
    /// Bouncing pad that sends a kart flying upward when touched.
    /// </summary>
    public class BouncingPadComponent : MonoBehaviour
    {
        public float BounceForce { get; set; } = 12f;
        public int BounceCount { get; set; } = 0;

        [Header("Configuration")]
        [SerializeField] private float _bounceCooldown = 0.5f;

        private float _lastBounceTime;

        private void OnTriggerEnter(Collider other)
        {
            if (Time.time - _lastBounceTime < _bounceCooldown) return;

            if (other.TryGetComponent<Rigidbody>(out Rigidbody rb))
            {
                rb.linearVelocity = Vector3.up * BounceForce;

                if (other.TryGetComponent(out PlayerKartController kart))
                {
                    kart.TakeDamage(BounceCount);
                }

                _lastBounceTime = Time.time;

                Debug.Log("Kart bounced on pad! Bounce force: " + BounceForce);
            }
        }
    }
}
