using NueGames.NueDeck.Scripts.Enums;
using UnityEngine;

namespace NueGames.NueDeck.Scripts.Relic.RelicEffect
{
    public class ThornsEffect : RelicEffectBase
    {
        public override RelicEffectType EffectType => RelicEffectType.Thorns;

        public override void ApplyEffect(RelicEffectParameters parameters)
        {
            var reflectedDamage = EffectValue(parameters);
            if (reflectedDamage <= 0) return;

            var attacker = parameters.SourceCharacter;
            if (!attacker || attacker.CharacterStats.IsDeath) return;

            attacker.CharacterStats.Damage(reflectedDamage);

            if (FxManager)
            {
                FxManager.PlayFx(attacker.transform, FxType.Attack);
                FxManager.SpawnFloatingText(attacker.TextSpawnRoot, reflectedDamage.ToString());
            }
        }
    }
}
