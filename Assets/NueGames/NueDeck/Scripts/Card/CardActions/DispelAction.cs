using NueGames.NueDeck.Scripts.Enums;

namespace NueGames.NueDeck.Scripts.Card.CardActions
{
    public class DispelAction : CardActionBase
    {
        public override CardActionType ActionType => CardActionType.Dispel;

        public override void DoAction(CardActionParameters actionParameters)
        {
            var target = actionParameters.TargetCharacter;
            if (!target || target.CharacterStats == null) return;

            target.CharacterStats.ClearStatuses(StatusPolarity.Beneficial);
        }
    }
}
