using UnityEngine;
using NitroRhythm.Player;

namespace NitroRhythm.Core
{
    /// <summary>
    /// Speed pad that grants a temporary forward speed boost when driven over.
    /// </summary>
    public class SpeedPadComponent : MonoBehaviour
    {
        public float BoostMultiplier { get; set; } = 1.6f;
        public float BoostDuration { get; set; } = 1.2f;

        private void OnTriggerEnter(Collider other)
        {
            if (other.TryGetComponent(out PlayerKartController kart))
            {
                kart.ApplySpeedBoost(BoostMultiplier, BoostDuration);
            }
        }
    }
}