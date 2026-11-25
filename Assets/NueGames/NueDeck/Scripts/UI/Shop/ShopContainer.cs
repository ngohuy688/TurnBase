using System;
using NueGames.NueDeck.Scripts.UI.Reward;
using UnityEngine;

namespace NueGames.NueDeck.Scripts.UI.Shop
{
    public class ShopContainer : MonoBehaviour
    {
        private RewardContainer _rewardContainer;
        private Func<bool> _purchase;
        private bool _isPurchased;

        public int GoldCost { get; private set; }

        public void Configure(
            RewardContainer rewardContainer,
            Sprite sprite,
            string description,
            int goldCost,
            Func<bool> purchase)
        {
            if (!rewardContainer || !rewardContainer.RewardButton)
            {
                Debug.LogError("Shop offer requires a RewardContainer with a button.", this);
                return;
            }

            _rewardContainer = rewardContainer;
            _purchase = purchase;
            GoldCost = goldCost;
            _rewardContainer.SetShopOfferLayout();
            _rewardContainer.BuildReward(sprite, $"{description}\nCost: {goldCost} gold");
            _rewardContainer.RewardButton.onClick.AddListener(OnPurchaseClicked);
        }

        public void SetCanPurchase(bool canPurchase)
        {
            if (_rewardContainer && _rewardContainer.RewardButton)
                _rewardContainer.RewardButton.interactable = canPurchase && !_isPurchased;
        }

        private void OnPurchaseClicked()
        {
            if (_purchase != null && _purchase())
            {
                _isPurchased = true;
                SetCanPurchase(false);
            }
        }
    }
}
