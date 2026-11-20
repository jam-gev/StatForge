using System;
using Unity.Collections.LowLevel.Unsafe;

namespace StatForge
{
    /// <summary>
    /// Converts a project-defined channel enum to/from the int the core stores, without boxing.
    /// One place for the "enum must be int-backed" rule: the static constructor runs once per TStatModifierChannel.
    /// </summary>
    internal static class StatChannelCast<TStatModifierChannel> where TStatModifierChannel : unmanaged, Enum
    {
        static StatChannelCast()
        {
            if (UnsafeUtility.SizeOf<TStatModifierChannel>() != sizeof(int))
            {
                throw new InvalidOperationException($"Channel enum {typeof(TStatModifierChannel).Name} must be int-backed (enum {typeof(TStatModifierChannel).Name} : int)");
            }
        }

        public static int ToInt(TStatModifierChannel channel)
        {
            return UnsafeUtility.As<TStatModifierChannel, int>(ref channel);
        }

        public static TStatModifierChannel FromInt(int raw)
        {
            return UnsafeUtility.As<int, TStatModifierChannel>(ref raw);
        }
    }
}