using NueGames.NueDeck.Scripts.Enums;
using NueGames.NueDeck.Scripts.Card.CardActions;

namespace NueGames.NueDeck.Scripts.Relic.RelicEffect
{
    public class ReduceCardCostEffect : RelicEffectBase
    {
        public override RelicEffectType EffectType => RelicEffectType.ReduceCardCost;

        public static int NextCardCostReduction => ReduceNextCardCostAction.NextCardCostReduction;

        public override void ApplyEffect(RelicEffectParameters parameters)
        {
            ReduceNextCardCostAction.SetReduction(EffectValue(parameters));
        }

        public static void ConsumeReduction()
        {
            ReduceNextCardCostAction.ConsumeReduction();
        }
    }
}
