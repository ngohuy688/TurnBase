using NueGames.NueDeck.Scripts.Enums;
using UnityEngine;

namespace NueGames.NueDeck.Scripts.Relic.RelicEffect
{
    public class IncreaseDamageEffect : RelicEffectBase
    {
        public override RelicEffectType EffectType => RelicEffectType.IncreaseDamage;

        public override void ApplyEffect(RelicEffectParameters parameters)
        {
            var multiplier = 1f + EffectValue(parameters) / 100f;
            parameters.Value = Mathf.Max(0, parameters.Value * multiplier);
        }
    }
}
