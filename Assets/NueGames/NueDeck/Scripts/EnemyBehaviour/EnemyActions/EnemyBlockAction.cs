using NueGames.NueDeck.Scripts.Enums;
using NueGames.NueDeck.Scripts.Managers;
using UnityEngine;

namespace NueGames.NueDeck.Scripts.EnemyBehaviour.EnemyActions
{
    public class EnemyBlockAction : EnemyActionBase
    {
        public override EnemyActionType ActionType => EnemyActionType.Block;
        
        public override void DoAction(EnemyActionParameters actionParameters)
        {
            
            var newTarget = actionParameters.TargetCharacter
                ? actionParameters.TargetCharacter
                : actionParameters.SelfCharacter;
            
            if (!newTarget) return;
            
            var blockValue = actionParameters.SelfCharacter.CharacterStats.ModifyBlockValue(actionParameters.Value);
            newTarget.CharacterStats.ApplyStatus(StatusType.Block, Mathf.RoundToInt(blockValue));
            
            if (FxManager != null)
                FxManager.PlayFx(newTarget.transform,FxType.Block);

            if (AudioManager != null)
                AudioManager.PlayOneShot(AudioActionType.Block);
        }
    }
}