using NitroRhythm.Player;
using UnityEngine;

namespace NitroRhythm.Controls
{
    /// <summary>
    /// Keyboard input provider for the three local bindings described in the GDD:
    /// P1 (WASD + Space), P2 (Arrows + Right Shift / Keypad 0) and
    /// P3 villain (IJKL + U to jump + Enter to throw).
    /// Uses the legacy input backend, which the project exposes alongside the
    /// new Input System (Both), keeping WebGL keyboard support simple.
    /// </summary>
    public class KeyboardInputProvider : IInputProvider
    {
        private readonly PlayerKartController.InputScheme _scheme;

        private bool _jumpLatch;
        private bool _fireLatch;

        public KeyboardInputProvider(PlayerKartController.InputScheme scheme)
        {
            _scheme = scheme;
        }

        public float Throttle { get; private set; }
        public float Steer { get; private set; }

        public bool ConsumeJump()
        {
            bool value = _jumpLatch;
            _jumpLatch = false;
            return value;
        }

        public bool ConsumeFire()
        {
            bool value = _fireLatch;
            _fireLatch = false;
            return value;
        }

        /// <summary>Called once per frame before the kart reads the axes.</summary>
        public void Poll()
        {
            switch (_scheme)
            {
                case PlayerKartController.InputScheme.PlayerOne:
                    Throttle = (Input.GetKey(KeyCode.W) ? 1f : 0f) - (Input.GetKey(KeyCode.S) ? 1f : 0f);
                    Steer = (Input.GetKey(KeyCode.D) ? 1f : 0f) - (Input.GetKey(KeyCode.A) ? 1f : 0f);
                    if (Input.GetKeyDown(KeyCode.Space)) _jumpLatch = true;
                    if (Input.GetKeyDown(KeyCode.LeftControl)) _fireLatch = true;
                    break;

                case PlayerKartController.InputScheme.PlayerTwo:
                    Throttle = (Input.GetKey(KeyCode.UpArrow) ? 1f : 0f) - (Input.GetKey(KeyCode.DownArrow) ? 1f : 0f);
                    Steer = (Input.GetKey(KeyCode.RightArrow) ? 1f : 0f) - (Input.GetKey(KeyCode.LeftArrow) ? 1f : 0f);
                    if (Input.GetKeyDown(KeyCode.RightShift) || Input.GetKeyDown(KeyCode.Keypad0)) _jumpLatch = true;
                    if (Input.GetKeyDown(KeyCode.Keypad1)) _fireLatch = true;
                    break;

                case PlayerKartController.InputScheme.PlayerThree:
                    Throttle = (Input.GetKey(KeyCode.I) ? 1f : 0f) - (Input.GetKey(KeyCode.K) ? 1f : 0f);
                    Steer = (Input.GetKey(KeyCode.L) ? 1f : 0f) - (Input.GetKey(KeyCode.J) ? 1f : 0f);
                    // U (not Right Shift / Numpad 0): those already belong to player 2.
                    if (Input.GetKeyDown(KeyCode.U)) _jumpLatch = true;
                    if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)) _fireLatch = true;
                    break;
            }
        }
    }
}
