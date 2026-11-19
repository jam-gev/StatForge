namespace StatForge
{
    /// <summary>A modifier stored inside a StatNode, tagged with the id its handle points to.</summary>
    public readonly struct StatModifierEntry
    {
        public readonly int EntryId;

        public readonly int ModifierChannel;

        public readonly float Value;

        public StatModifierEntry(int entryId, int modifierChannel, float value)
        {
            EntryId = entryId;
            ModifierChannel = modifierChannel;
            Value = value;
        }

        public StatModifierEntry(int entryId, StatModifier statModifier)
        {
            EntryId = entryId;
            ModifierChannel = statModifier.ModifierChannel;
            Value = statModifier.Value;
        }
    }
}