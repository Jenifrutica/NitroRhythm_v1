using UnityEngine;

namespace NitroRhythm.Core
{
    /// <summary>
    /// Moving hazard that slides back and forth; either along the track axis (X)
    /// or sweeping across it (Z) to push players off the edge.
    /// </summary>
    public class MovingHazardComponent : MonoBehaviour
    {
        public float MoveSpeed { get; set; } = 3f;
        public float MoveRange { get; set; } = 2f;

        /// <summary>When true the hazard sweeps across the track width (Z) to push players off.</summary>
        public bool SweepAcrossTrack { get; set; }

        private Vector3 _startPosition;
        private float _phase;

        private void Start()
        {
            _startPosition = transform.position;
        }

        private void Update()
        {
            _phase += MoveSpeed * Time.deltaTime;

            Vector3 position = _startPosition;
            if (SweepAcrossTrack)
            {
                position.z = _startPosition.z + Mathf.Sin(_phase) * MoveRange;
            }
            else
            {
                position.x = _startPosition.x + Mathf.Sin(_phase) * MoveRange;
            }
            transform.position = position;
        }
    }
}