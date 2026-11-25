using NueGames.NueDeck.Scripts.Enums;
using NueGames.NueDeck.Scripts.Managers;
using UnityEngine;

namespace NueGames.NueDeck.Scripts.Card.CardActions
{
    public class ApplyPoisonAction : CardActionBase
    {
        public override CardActionType ActionType => CardActionType.ApplyPoison;

        public override void DoAction(CardActionParameters actionParameters)
        {
            var target = actionParameters.TargetCharacter;
            if (!target || target.CharacterStats.IsDeath) return;

            var stacks = Mathf.RoundToInt(actionParameters.Value);
            if (stacks <= 0) return;

            target.CharacterStats.ApplyStatus(StatusType.Poison, stacks);

            if (FxManager)
                FxManager.PlayFx(target.transform, FxType.Poison);
            if (AudioManager && actionParameters.CardData)
                AudioManager.PlayOneShot(actionParameters.CardData.AudioType);
        }
    }
}
