using System;
using Unity.Collections.LowLevel.Unsafe;

namespace StatForge
{
    /// <summary>
    /// Converts a project-defined channel enum to/from the int the core stores, without boxing.
    /// One place for the "enum must be int-backed" rule: the static constructor runs once per TChannel.
    /// </summary>
    internal static class StatChannelCast<TChannel> where TChannel : unmanaged, Enum
    {
        static StatChannelCast()
        {
            if (UnsafeUtility.SizeOf<TChannel>() != sizeof(int))
            {
                throw new InvalidOperationException($"Channel enum {typeof(TChannel).Name} must be int-backed (enum {typeof(TChannel).Name} : int)");
            }
        }

        public static int ToInt(TChannel channel)
        {
            return UnsafeUtility.As<TChannel, int>(ref channel);
        }

        public static TChannel FromInt(int raw)
        {
            return UnsafeUtility.As<int, TChannel>(ref raw);
        }
    }
}