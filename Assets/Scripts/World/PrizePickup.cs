using NitroRhythm.Audio;
using NitroRhythm.Player;
using UnityEngine;

namespace NitroRhythm.World
{
    /// <summary>Collectible prize on the track: grants a short turbo and vanishes.</summary>
    public class PrizePickup : MonoBehaviour
    {
        public float BoostMultiplier = 1.5f;
        public float BoostDuration = 2f;
        public bool Collected { get; private set; }

        private void OnTriggerEnter(Collider other)
        {
            if (Collected) return;
            PlayerKartController kart = other.GetComponentInParent<PlayerKartController>();
            if (kart == null) return;

            Collected = true;
            kart.ApplySpeedBoost(BoostMultiplier, BoostDuration);
            SfxPlayer.Play(SfxLibrary.Pickup, 0.8f);
            Destroy(gameObject);
        }
    }
}
