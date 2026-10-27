using System;
using UnityEngine;

namespace BoomerangGuardian.Core
{
    /// <summary>
    /// Marks a serialized reference that may legitimately stay empty (e.g. an audio clip slot
    /// that falls back to a placeholder). "Validate Wiring" reports every other empty reference.
    /// Usage: [SerializeField, OptionalReference] private Transform spawnRoot;
    /// It has no runtime behaviour: the Editor-only WiringValidator finds it via reflection
    /// (GetCustomAttribute) and skips the field.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class OptionalReferenceAttribute : PropertyAttribute
    {
    }
}
