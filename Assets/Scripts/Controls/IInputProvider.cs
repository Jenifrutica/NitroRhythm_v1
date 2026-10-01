namespace NitroRhythm.Controls
{
    /// <summary>
    /// Abstraction over one player's controls. Keeps the input source decoupled
    /// from the kart physics, so keyboard, bot AI or a replay can all drive the
    /// same <c>PlayerKartController</c>.
    /// </summary>
    public interface IInputProvider
    {
        /// <summary>Forward/backward axis in the range -1..1.</summary>
        float Throttle { get; }

        /// <summary>Left/right steering axis in the range -1..1.</summary>
        float Steer { get; }

        /// <summary>Returns true exactly once per jump press.</summary>
        bool ConsumeJump();

        /// <summary>Returns true exactly once per fire press.</summary>
        bool ConsumeFire();
    }
}
