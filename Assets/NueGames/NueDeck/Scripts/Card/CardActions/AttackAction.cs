using NueGames.NueDeck.Scripts.Characters;
using NueGames.NueDeck.Scripts.Enums;
using NueGames.NueDeck.Scripts.Managers;
using NueGames.NueDeck.Scripts.Relic;
using UnityEngine;

namespace NueGames.NueDeck.Scripts.Card.CardActions
{
    public class AttackAction : CardActionBase
    {
        public override CardActionType ActionType => CardActionType.Attack;

        public override void DoAction(CardActionParameters actionParameters)
        {
            if (!actionParameters.TargetCharacter) return;

            var targetCharacter = actionParameters.TargetCharacter;
            var selfCharacter = actionParameters.SelfCharacter;
            var value = Mathf.Max(0, actionParameters.Value + selfCharacter.CharacterStats.StrengthModifier);

            if (targetCharacter.CharacterStats.StatusDict[StatusType.Vulnerable].StatusValue > 0)
                value = Mathf.RoundToInt(value * 1.5f);

            value = selfCharacter.CharacterStats.ModifyOutgoingDamage(value);

            if (selfCharacter is AllyBase)
            {
                var damageParameters = new RelicEffectParameters
                {
                    Value = value,
                    SourceCharacter = selfCharacter,
                    TargetCharacter = targetCharacter,
                    CardData = actionParameters.CardData
                };
                CombatManager.Instance?.TriggerFirstDamageDealt(damageParameters);
                RelicEffectProcessor.Trigger(RelicCondition.OnDamageDealt, damageParameters);
                value = damageParameters.Value;
            }

            targetCharacter.CharacterStats.Damage(Mathf.RoundToInt(value), false, selfCharacter);
            if (targetCharacter is AllyBase)
            {
                var damageParameters = new RelicEffectParameters
                {
                    Value = value,
                    SourceCharacter = selfCharacter,
                    TargetCharacter = targetCharacter,
                    CardData = actionParameters.CardData
                };
                CombatManager.Instance?.TriggerFirstDamageTaken(damageParameters);
                RelicEffectProcessor.Trigger(RelicCondition.OnDamageTaken, damageParameters);
            }

            if (FxManager != null)
            {
                FxManager.PlayFx(targetCharacter.transform, FxType.Attack);
                FxManager.SpawnFloatingText(targetCharacter.TextSpawnRoot, value.ToString());
            }

            if (AudioManager != null)
                AudioManager.PlayOneShot(actionParameters.CardData.AudioType);
        }
    }
}
