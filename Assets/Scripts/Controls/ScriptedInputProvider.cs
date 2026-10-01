namespace NitroRhythm.Controls
{
    /// <summary>
    /// Programmatic input source used by the bot AI and by scripted events.
    /// Values are pushed by the owner each frame.
    /// </summary>
    public class ScriptedInputProvider : IInputProvider
    {
        public float Throttle { get; set; }
        public float Steer { get; set; }

        private bool _jumpLatch;
        private bool _fireLatch;

        public void SetDrive(float throttle, float steer)
        {
            Throttle = throttle;
            Steer = steer;
        }

        public void RequestJump() => _jumpLatch = true;
        public void RequestFire() => _fireLatch = true;

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
    }
}
