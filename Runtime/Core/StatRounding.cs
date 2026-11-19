namespace StatForge
{
    /// <summary>How a stat's final value is rounded (applied before clamping).</summary>
    public enum StatRounding
    {
        /// <summary>Keep the fractional value (e.g. crit chance 0.155).</summary>
        None = 0,

        /// <summary>Round down toward negative infinity (e.g. 12.9 -> 12).</summary>
        Floor = 1,

        /// <summary>Round up toward positive infinity (e.g. 12.1 -> 13).</summary>
        Ceil = 2,

        /// <summary>Round to nearest, .5 away from zero (2.5 -> 3, -2.5 -> -3).</summary>
        Round = 3,
    }
}