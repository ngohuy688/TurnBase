using System.Collections.Generic;
using NueGames.NueDeck.Scripts.Data.Collection;
using UnityEngine;

namespace NueGames.NueDeck.Scripts.Data.Containers
{
    public enum NodeRewardEffectType
    {
        GainGold,
        HealAllAllies,
        DamageAllAllies,
        AddCard,
        AddRelic
    }

    [System.Serializable]
    public class NodeRewardEffect
    {
        [SerializeField] private NodeRewardEffectType effectType;
        [SerializeField] private int amount;
        [SerializeField] private CardData cardData;
        [SerializeField] private RelicData relicData;

        public NodeRewardEffectType EffectType => effectType;
        public int Amount => amount;
        public CardData CardData => cardData;
        public RelicData RelicData => relicData;
    }

    [System.Serializable]
    public class EventChoiceData
    {
        [SerializeField] private string title;
        [TextArea] [SerializeField] private string description;
        [SerializeField] private List<NodeRewardEffect> effects;
        [SerializeField] private EventData nextEventData;

        public string Title => title;
        public string Description => description;
        public List<NodeRewardEffect> Effects => effects;
        public EventData NextEventData => nextEventData;
    }

    [CreateAssetMenu(fileName = "Node Event", menuName = "NueDeck/Map/Node Event", order = 5)]
    [System.Serializable]
    public class EventData : ScriptableObject
    {
        [SerializeField] private string eventTitle;
        [TextArea] [SerializeField] private string description;
        [SerializeField] private List<EventChoiceData> choices;

        public string EventTitle => eventTitle;
        public string Description => description;
        public List<EventChoiceData> Choices => choices;
    }
}
