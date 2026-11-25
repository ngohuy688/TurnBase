using NueGames.NueDeck.Scripts.Enums;
using NueGames.NueDeck.Scripts.Managers;
using UnityEngine;

namespace NueGames.NueDeck.Scripts.Card.CardActions
{
    public class DecreaseDamageAction : CardActionBase
    {
        public override CardActionType ActionType => CardActionType.DecreaseDamage;

        public override void DoAction(CardActionParameters actionParameters)
        {
            var target = actionParameters.TargetCharacter
                ? actionParameters.TargetCharacter
                : actionParameters.SelfCharacter;
            if (!target || target.CharacterStats.IsDeath) return;

            target.CharacterStats.ApplyStatus(StatusType.DamageDown, Mathf.RoundToInt(actionParameters.Value));

            if (FxManager != null)
                FxManager.PlayFx(target.transform, FxType.Buff);

            if (AudioManager != null && actionParameters.CardData != null)
                AudioManager.PlayOneShot(actionParameters.CardData.AudioType);
        }
    }
}
