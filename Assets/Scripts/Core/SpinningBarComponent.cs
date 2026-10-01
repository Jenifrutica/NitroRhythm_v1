using UnityEngine;

namespace NitroRhythm.Core
{
    /// <summary>
    /// Rotating bar obstacle that spins around its vertical axis.
    /// </summary>
    public class SpinningBarComponent : MonoBehaviour
    {
        public float SpinSpeed { get; set; } = 90f;

        private void Update()
        {
            transform.Rotate(Vector3.up, SpinSpeed * Time.deltaTime, Space.Self);
        }
    }
}
