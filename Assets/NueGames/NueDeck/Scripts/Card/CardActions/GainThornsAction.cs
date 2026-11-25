using NueGames.NueDeck.Scripts.Enums;
using NueGames.NueDeck.Scripts.Managers;
using UnityEngine;

namespace NueGames.NueDeck.Scripts.Card.CardActions
{
    public class GainThornsAction : CardActionBase
    {
        public override CardActionType ActionType => CardActionType.GainThorns;

        public override void DoAction(CardActionParameters actionParameters)
        {
            var target = actionParameters.TargetCharacter
                ? actionParameters.TargetCharacter
                : actionParameters.SelfCharacter;
            if (!target || target.CharacterStats.IsDeath) return;

            var stacks = Mathf.RoundToInt(actionParameters.Value);
            if (stacks <= 0) return;

            target.CharacterStats.ApplyStatus(StatusType.Thorns, stacks);

            if (FxManager)
                FxManager.PlayFx(target.transform, FxType.Buff);
            if (AudioManager && actionParameters.CardData)
                AudioManager.PlayOneShot(actionParameters.CardData.AudioType);
        }
    }
}
