using NueGames.NueDeck.Scripts.Enums;

namespace NueGames.NueDeck.Scripts.Card.CardActions
{
    public class PurifyAction : CardActionBase
    {
        public override CardActionType ActionType => CardActionType.Purify;

        public override void DoAction(CardActionParameters actionParameters)
        {
            var target = actionParameters.TargetCharacter
                ? actionParameters.TargetCharacter
                : actionParameters.SelfCharacter;
            if (!target || target.CharacterStats == null) return;

            target.CharacterStats.ClearStatuses(StatusPolarity.Harmful);
        }
    }
}
