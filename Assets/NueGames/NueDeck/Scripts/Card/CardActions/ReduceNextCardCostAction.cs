using NueGames.NueDeck.Scripts.Enums;
using UnityEngine;

namespace NueGames.NueDeck.Scripts.Card.CardActions
{
    public class ReduceNextCardCostAction : CardActionBase
    {
        public override CardActionType ActionType => CardActionType.ReduceNextCardCost;

        public static int NextCardCostReduction { get; private set; }

        public override void DoAction(CardActionParameters actionParameters)
        {
            SetReduction(Mathf.RoundToInt(actionParameters.Value));

            if (FxManager && actionParameters.SelfCharacter)
                FxManager.PlayFx(actionParameters.SelfCharacter.transform, FxType.Buff);
            if (AudioManager && actionParameters.CardData)
                AudioManager.PlayOneShot(actionParameters.CardData.AudioType);
        }

        public static int GetCost(int baseCost)
        {
            return Mathf.Max(0, baseCost - NextCardCostReduction);
        }

        public static void SetReduction(int value)
        {
            NextCardCostReduction = Mathf.Max(0, value);
        }

        public static void ConsumeReduction()
        {
            NextCardCostReduction = 0;
        }

        public static void Reset()
        {
            NextCardCostReduction = 0;
        }
    }
}
