using UnityEngine;
using NitroRhythm.Combat;
using NitroRhythm.Player;

namespace NitroRhythm.Core
{
    /// <summary>
    /// Respawns any entity — player karts or the AI villain — that falls below
    /// Y = -10 at the current level checkpoint while preserving forward speed.
    /// </summary>
    public class RespawnOnFall : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private float _fallThreshold = -10f;
        [SerializeField] private float _respawnCooldown = 0.5f;
        [SerializeField] private Vector3 _respawnOffset = new Vector3(0f, 1.2f, 0f);

        [Header("Fallback Spawn (used when no LevelManager is present)")]
        [SerializeField] private Vector3 _fallbackSpawn = new Vector3(0f, 1.2f, 0f);

        private float _lastRespawnTime;

        private void Update()
        {
            CheckForFalls();
        }

        private void CheckForFalls()
        {
            if (Time.time - _lastRespawnTime < _respawnCooldown) return;

            bool respawnedAny = false;

            // Player karts (P1, P2 and player-controlled P3 villain).
            PlayerKartController[] players = FindObjectsOfType<PlayerKartController>();
            foreach (PlayerKartController player in players)
            {
                if (player.transform.position.y < _fallThreshold)
                {
                    RespawnAtCheckpoint(player.transform, player.GetComponent<Rigidbody>(), player.name);
                    respawnedAny = true;
                }
            }

            // AI villain (player-controlled villains are already handled above as karts).
            VillainBoss villain = FindObjectOfType<VillainBoss>();
            if (villain != null && villain.GetComponent<PlayerKartController>() == null && villain.transform.position.y < _fallThreshold)
            {
                villain.RespawnAtCheckpoint();
                respawnedAny = true;
            }

            if (respawnedAny)
            {
                _lastRespawnTime = Time.time;
            }
        }

        private void RespawnAtCheckpoint(Transform entity, Rigidbody rb, string entityName)
        {
            Vector3 respawnPosition = _fallbackSpawn;

            LevelManager levelManager = FindObjectOfType<LevelManager>();
            if (levelManager != null)
            {
                respawnPosition = levelManager.GetPlayerSpawnPoint() + _respawnOffset;
            }

            // Preserve forward momentum so respawning does not break the speedrun flow.
            Vector3 keptMomentum = rb != null ? rb.linearVelocity : Vector3.zero;
            keptMomentum.y = 0f;

            entity.position = respawnPosition;
            entity.rotation = Quaternion.Euler(0f, 90f, 0f); // face down the +X track

            if (rb != null)
            {
                rb.linearVelocity = new Vector3(keptMomentum.x, 0f, keptMomentum.z);
                rb.angularVelocity = Vector3.zero;
            }

            Debug.Log($"[RespawnOnFall] Respawning {entityName} at {respawnPosition} (keeping {keptMomentum.magnitude:F1} u/s momentum)");
        }
    }
}
