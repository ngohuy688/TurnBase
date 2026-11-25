using NueGames.NueDeck.Scripts.Enums;

namespace NueGames.NueDeck.Scripts.NueExtentions
{
    public static class StatusTypeExtentions
    {
        public static StatusPolarity GetPolarity(this StatusType statusType)
        {
            switch (statusType)
            {
                case StatusType.Block:
                case StatusType.Strength:
                case StatusType.Dexterity:
                case StatusType.DamageUp:
                case StatusType.Thorns:
                    return StatusPolarity.Beneficial;

                case StatusType.Poison:
                case StatusType.Stun:
                case StatusType.Weak:
                case StatusType.Vulnerable:
                case StatusType.StrengthDown:
                case StatusType.DamageDown:
                    return StatusPolarity.Harmful;

                case StatusType.None:
                default:
                    return StatusPolarity.None;
            }
        }

        public static bool IsBeneficial(this StatusType statusType) => statusType.GetPolarity() == StatusPolarity.Beneficial;

        public static bool IsHarmful(this StatusType statusType) => statusType.GetPolarity() == StatusPolarity.Harmful;
    }
}
