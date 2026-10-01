using NitroRhythm.Player;
using UnityEngine;

namespace NitroRhythm.Core
{
    /// <summary>
    /// Intermediate checkpoint volume. The first active player to touch it
    /// advances the LevelManager's respawn point (forward only).
    /// </summary>
    public class CheckpointComponent : MonoBehaviour
    {
        private LevelManager _levelManager;

        private void Start()
        {
            Collider collider = GetComponent<Collider>();
            if (collider != null) collider.isTrigger = true;

            _levelManager = FindObjectOfType<LevelManager>();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_levelManager == null)
            {
                _levelManager = FindObjectOfType<LevelManager>();
                if (_levelManager == null) return;
            }

            PlayerKartController kart = other.GetComponentInParent<PlayerKartController>();
            if (kart == null) return;

            _levelManager.ActivateCheckpoint(transform.position);
        }
    }
}
