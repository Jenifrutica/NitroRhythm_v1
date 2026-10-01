using UnityEngine;

namespace NitroRhythm.Combat
{
    public interface ITargetSelectable
    {
        Transform Transform { get; }
        bool IsValidTarget { get; }
    }
}