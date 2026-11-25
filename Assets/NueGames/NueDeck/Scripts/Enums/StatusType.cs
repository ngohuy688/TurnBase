namespace NueGames.NueDeck.Scripts.Enums
{
    [System.Flags]
    public enum StatusType
    {
        None = 0,
        Block = 1 << 0,
        Poison = 1 << 1,
        Strength = 1 << 2,
        Dexterity = 1 << 3,
        Stun = 1 << 4,
        Weak = 1 << 5,
        Vulnerable = 1 << 6,
        StrengthDown = 1 << 7,
        DamageUp = 1 << 8,
        DamageDown = 1 << 9,
        Thorns = 1 << 10
    }
}
