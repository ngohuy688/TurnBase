using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NueGames.NueDeck.Scripts.UI.Reward
{
    public class RewardContainer : MonoBehaviour
    {
        [SerializeField] private Button rewardButton;
        [SerializeField] private Image rewardImage;
        [SerializeField] private TextMeshProUGUI rewardText;

        public Button RewardButton => rewardButton;

        public void BuildReward(Sprite rewardSprite,string rewardDescription)
        {
            rewardImage.sprite = rewardSprite;
            rewardText.text = rewardDescription;
        }

        public void SetShopOfferLayout()
        {
            var textRect = rewardText.rectTransform;
            textRect.anchorMin = new Vector2(0.22f, 0f);
            textRect.anchorMax = new Vector2(0.98f, 1f);
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            rewardText.alignment = TextAlignmentOptions.MidlineLeft;
            rewardText.enableWordWrapping = true;
            rewardText.enableAutoSizing = true;
            rewardText.fontSizeMin = 14f;
            rewardText.fontSizeMax = 20f;
            rewardText.overflowMode = TextOverflowModes.Ellipsis;
            rewardImage.preserveAspect = true;
        }
        
    }
}