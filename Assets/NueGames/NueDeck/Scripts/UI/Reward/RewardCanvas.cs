using System;
using System.Collections.Generic;
using NueGames.NueDeck.Scripts.Card;
using NueGames.NueDeck.Scripts.Data.Collection;
using NueGames.NueDeck.Scripts.Data.Containers;
using NueGames.NueDeck.Scripts.Enums;
using NueGames.NueDeck.Scripts.NueExtentions;
using NueGames.NueDeck.Scripts.Relic;
using UnityEngine;

namespace NueGames.NueDeck.Scripts.UI.Reward
{
    public class RewardCanvas : CanvasBase
    {
        [Header("References")]
        [SerializeField] private RewardContainerData rewardContainerData;
        [SerializeField] private Transform rewardRoot;
        [SerializeField] private RewardContainer rewardContainerPrefab;
        [SerializeField] private Transform rewardPanelRoot;
        [Header("Choice")]
        [SerializeField] private Transform choice2DCardSpawnRoot;
        [SerializeField] private ChoiceCard choiceCardUIPrefab;
        [SerializeField] private ChoicePanel choicePanel;

        private readonly List<RewardContainer> _currentRewardsList = new List<RewardContainer>();
        private readonly List<ChoiceCard> _spawnedChoiceList = new List<ChoiceCard>();
        private readonly List<CardData> _cardRewardList = new List<CardData>();
        private readonly List<RewardContainer> _relicChoiceList = new List<RewardContainer>();
        public ChoicePanel ChoicePanel => choicePanel;

        public void PrepareCanvas()
        {
            if (rewardPanelRoot) rewardPanelRoot.gameObject.SetActive(true);
        }

        public void BuildReward(RewardType rewardType)
        {
            if (!rewardContainerData || !rewardContainerPrefab || !rewardRoot)
            {
                Debug.LogError("RewardCanvas requires reward data, a prefab, and a root.", this);
                return;
            }
            if (rewardType == RewardType.Relic)
            {
                BuildRelicReward();
                return;
            }

            var clone = Instantiate(rewardContainerPrefab, rewardRoot);
            _currentRewardsList.Add(clone);
            switch (rewardType)
            {
                case RewardType.Gold:
                    var amount = rewardContainerData.GetRandomGoldReward(out var goldData);
                    clone.BuildReward(goldData.RewardSprite, goldData.RewardDescription);
                    clone.RewardButton.onClick.AddListener(() => GetGoldReward(clone, amount));
                    break;
                case RewardType.Card:
                    var cards = rewardContainerData.GetRandomCardRewardList(out var cardData);
                    _cardRewardList.Clear();
                    _cardRewardList.AddRange(cards);
                    clone.BuildReward(cardData.RewardSprite, cardData.RewardDescription);
                    clone.RewardButton.onClick.AddListener(() => GetCardReward(clone, 3));
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(rewardType), rewardType, null);
            }
        }

        public override void ResetCanvas()
        {
            ResetRewards();
            ResetChoice();
        }

        private void ResetRewards()
        {
            foreach (var reward in _currentRewardsList) if (reward) Destroy(reward.gameObject);
            _currentRewardsList.Clear();
            _relicChoiceList.Clear();
        }

        private void ResetChoice()
        {
            foreach (var choice in _spawnedChoiceList) if (choice) Destroy(choice.gameObject);
            _spawnedChoiceList.Clear();
            if (choicePanel) choicePanel.DisablePanel();
        }

        private void GetGoldReward(RewardContainer rewardContainer, int amount)
        {
            NodeRewardService.GainGold(amount);
            _currentRewardsList.Remove(rewardContainer);
            if (rewardContainer) Destroy(rewardContainer.gameObject);
        }

        private void BuildRelicReward()
        {
            var relicChoices = rewardContainerData.GetRandomRelicRewardList(out _);
            var ownedRelics = GameManager.PersistentGameplayData.CurrentRelicsList;
            relicChoices.RemoveAll(relic => !relic || (relic.IsUnique && ownedRelics.Exists(owned => owned && owned.RelicId == relic.RelicId)));
            if (relicChoices.Count == 0)
            {
                Debug.LogWarning("No unowned relics are available for this reward.", rewardContainerData);
                return;
            }
            var relic = relicChoices.RandomItem();
            var clone = Instantiate(rewardContainerPrefab, rewardRoot);
            _currentRewardsList.Add(clone);
            _relicChoiceList.Add(clone);
            clone.BuildReward(relic.RelicSprite, $"{relic.RelicName}\n{relic.RelicDescription}");
            clone.RewardButton.onClick.AddListener(() => GetRelicReward(relic));
        }

        private void GetRelicReward(RelicData relicData)
        {
            if (!relicData || !RelicEffectProcessor.GrantRelic(relicData)) return;
            foreach (var choice in _relicChoiceList)
            {
                _currentRewardsList.Remove(choice);
                if (choice) Destroy(choice.gameObject);
            }
            _relicChoiceList.Clear();
        }

        private void GetCardReward(RewardContainer rewardContainer, int amount = 3)
        {
            if (!choicePanel || !choiceCardUIPrefab || !choice2DCardSpawnRoot || _cardRewardList.Count == 0)
            {
                Debug.LogError("RewardCanvas is missing card choice references or card rewards.", this);
                return;
            }
            choicePanel.gameObject.SetActive(true);
            var choiceCount = Mathf.Min(amount, _cardRewardList.Count);
            for (var i = 0; i < choiceCount; i++)
            {
                var choice = Instantiate(choiceCardUIPrefab, choice2DCardSpawnRoot);
                var card = _cardRewardList.RandomItem();
                choice.BuildReward(card);
                choice.OnCardChose += ResetChoice;
                _cardRewardList.Remove(card);
                _spawnedChoiceList.Add(choice);
                _currentRewardsList.Remove(rewardContainer);
            }
            if (rewardContainer) Destroy(rewardContainer.gameObject);
        }
    }
}
