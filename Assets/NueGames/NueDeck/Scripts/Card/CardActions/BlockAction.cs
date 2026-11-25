using NueGames.NueDeck.Scripts.Characters;
using NueGames.NueDeck.Scripts.Enums;
using NueGames.NueDeck.Scripts.Managers;
using NueGames.NueDeck.Scripts.Relic;
using UnityEngine;

namespace NueGames.NueDeck.Scripts.Card.CardActions
{
    public class BlockAction : CardActionBase
    {
        public override CardActionType ActionType => CardActionType.Block;
        public override void DoAction(CardActionParameters actionParameters)
        {
            var newTarget = actionParameters.TargetCharacter
                ? actionParameters.TargetCharacter
                : actionParameters.SelfCharacter;
            
            if (!newTarget) return;
            
            var blockValue = actionParameters.SelfCharacter.CharacterStats.ModifyBlockValue(actionParameters.Value);
            newTarget.CharacterStats.ApplyStatus(StatusType.Block, Mathf.RoundToInt(blockValue));
            if (newTarget is AllyBase)
                RelicEffectProcessor.Trigger(RelicCondition.OnBlockGained,
                    new RelicEffectParameters
                    {
                        Value = blockValue,
                        SourceCharacter = actionParameters.SelfCharacter,
                        TargetCharacter = newTarget,
                        CardData = actionParameters.CardData
                    });

            if (FxManager != null) 
                FxManager.PlayFx(newTarget.transform, FxType.Block);
            
            if (AudioManager != null) 
                AudioManager.PlayOneShot(actionParameters.CardData.AudioType);
        }
    }
}