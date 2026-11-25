using NueGames.NueDeck.Scripts.Enums;
using NueGames.NueDeck.Scripts.Managers;
using UnityEngine;

namespace NueGames.NueDeck.Scripts.Card.CardActions
{
    public class DecreaseStrengthAction : CardActionBase
    {
        public override CardActionType ActionType => CardActionType.DecreaseStrength;

        public override void DoAction(CardActionParameters actionParameters)
        {
            var target = actionParameters.TargetCharacter
                ? actionParameters.TargetCharacter
                : actionParameters.SelfCharacter;

            if (!target || target.CharacterStats.IsDeath) return;

            var amount = Mathf.Abs(Mathf.RoundToInt(actionParameters.Value));
            if (amount <= 0) return;

            target.CharacterStats.ApplyStatus(StatusType.StrengthDown, amount);

            if (FxManager != null)
                FxManager.PlayFx(target.transform, FxType.Buff);

            if (AudioManager != null && actionParameters.CardData != null)
                AudioManager.PlayOneShot(actionParameters.CardData.AudioType);
        }
    }
}
