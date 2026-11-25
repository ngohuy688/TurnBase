using NueGames.NueDeck.Scripts.Characters;
using NueGames.NueDeck.Scripts.Enums;
using NueGames.NueDeck.Scripts.Managers;
using NueGames.NueDeck.Scripts.Relic;
using UnityEngine;

namespace NueGames.NueDeck.Scripts.Card.CardActions
{
    public class LifeStealAction : CardActionBase
    {
        public override CardActionType ActionType => CardActionType.LifeSteal;

        public override void DoAction(CardActionParameters actionParameters)
        {
            if (!actionParameters.TargetCharacter) return;

            var value = Mathf.Max(0, actionParameters.Value + actionParameters.SelfCharacter.CharacterStats.StrengthModifier);
            value = actionParameters.SelfCharacter.CharacterStats.ModifyOutgoingDamage(value);
            if (actionParameters.SelfCharacter is AllyBase)
            {
                var damageParameters = new RelicEffectParameters
                {
                    Value = value,
                    SourceCharacter = actionParameters.SelfCharacter,
                    TargetCharacter = actionParameters.TargetCharacter,
                    CardData = actionParameters.CardData
                };
                CombatManager.Instance?.TriggerFirstDamageDealt(damageParameters);
                RelicEffectProcessor.Trigger(RelicCondition.OnDamageDealt, damageParameters);
                value = damageParameters.Value;
            }

            var roundedValue = Mathf.RoundToInt(value);
            actionParameters.TargetCharacter.CharacterStats.Damage(roundedValue, false, actionParameters.SelfCharacter);
            if (actionParameters.TargetCharacter is AllyBase)
            {
                var damageParameters = new RelicEffectParameters
                {
                    Value = value,
                    SourceCharacter = actionParameters.SelfCharacter,
                    TargetCharacter = actionParameters.TargetCharacter,
                    CardData = actionParameters.CardData
                };
                CombatManager.Instance?.TriggerFirstDamageTaken(damageParameters);
                RelicEffectProcessor.Trigger(RelicCondition.OnDamageTaken, damageParameters);
            }

            var previousHealth = actionParameters.SelfCharacter.CharacterStats.CurrentHealth;
            actionParameters.SelfCharacter.CharacterStats.Heal(roundedValue);
            var restoredHealth = actionParameters.SelfCharacter.CharacterStats.CurrentHealth - previousHealth;
            if (restoredHealth > 0 && actionParameters.SelfCharacter is AllyBase)
                RelicEffectProcessor.Trigger(RelicCondition.OnHealthRestored,
                    new RelicEffectParameters
                    {
                        Value = restoredHealth,
                        SourceCharacter = actionParameters.SelfCharacter,
                        TargetCharacter = actionParameters.SelfCharacter,
                        CardData = actionParameters.CardData
                    });

            if (FxManager != null)
            {
                FxManager.PlayFx(actionParameters.TargetCharacter.transform, FxType.Attack);
                FxManager.PlayFx(actionParameters.SelfCharacter.transform, FxType.Heal);
                FxManager.SpawnFloatingText(actionParameters.TargetCharacter.TextSpawnRoot, value.ToString());
            }

            if (AudioManager != null)
                AudioManager.PlayOneShot(actionParameters.CardData.AudioType);
        }
    }
}
