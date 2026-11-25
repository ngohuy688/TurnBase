using NueGames.NueDeck.Scripts.Characters;
using NueGames.NueDeck.Scripts.Enums;
using NueGames.NueDeck.Scripts.Managers;
using NueGames.NueDeck.Scripts.Relic;
using UnityEngine;

namespace NueGames.NueDeck.Scripts.EnemyBehaviour.EnemyActions
{
    public class EnemyAttackAction : EnemyActionBase
    {
        public override EnemyActionType ActionType => EnemyActionType.Attack;

        public override void DoAction(EnemyActionParameters actionParameters)
        {
            if (!actionParameters.TargetCharacter) return;

            var value = Mathf.Max(0, Mathf.RoundToInt(actionParameters.Value +
                actionParameters.SelfCharacter.CharacterStats.StrengthModifier));

            if (actionParameters.TargetCharacter.CharacterStats.StatusDict[StatusType.Vulnerable].StatusValue > 0)
                value = Mathf.RoundToInt(value * 1.5f);

            value = Mathf.RoundToInt(actionParameters.SelfCharacter.CharacterStats.ModifyOutgoingDamage(value));

            actionParameters.TargetCharacter.CharacterStats.Damage(value, false, actionParameters.SelfCharacter);
            if (actionParameters.TargetCharacter is AllyBase)
            {
                var damageParameters = new RelicEffectParameters
                {
                    Value = value,
                    SourceCharacter = actionParameters.SelfCharacter,
                    TargetCharacter = actionParameters.TargetCharacter
                };
                CombatManager.Instance?.TriggerFirstDamageTaken(damageParameters);
                RelicEffectProcessor.Trigger(RelicCondition.OnDamageTaken, damageParameters);
            }

            if (FxManager != null)
            {
                FxManager.PlayFx(actionParameters.TargetCharacter.transform, FxType.Attack);
                FxManager.SpawnFloatingText(actionParameters.TargetCharacter.TextSpawnRoot, value.ToString());
            }

            if (AudioManager != null)
                AudioManager.PlayOneShot(AudioActionType.Attack);
        }
    }
}
