using System;
using TMPro;
using NueGames.NueDeck.Scripts.Enums;
using NueGames.NueDeck.Scripts.Managers;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace NueGames.NueDeck.Scripts.UI
{
    public class EncounterButton : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private Button button;
        [SerializeField] private Image completedImage;
        [SerializeField] private bool isFinal;

        private Action _onSelected;
        
        protected GameManager GameManager => GameManager.Instance;
        
        private void Awake()
        {
            if (completedImage)
                completedImage.gameObject.SetActive(false);
        }

        public void Configure(string nodeName, EncounterMapNodeType nodeType, bool isFinal, Action onSelected)
        {
            if (!button)
            {
                Debug.LogError("EncounterButton is missing its Button reference.", this);
                return;
            }

            var label = GetComponentInChildren<TMP_Text>(true);
            if (!label)
            {
                Debug.LogError("EncounterButton is missing a TextMeshPro label.", this);
                return;
            }

            this.isFinal = isFinal;
            label.text = isFinal ? $"{nodeName} [Stage Exit]" : nodeName;
            var colors = button.colors;
            colors.normalColor = GetNodeColor(nodeType);
            colors.highlightedColor = Color.Lerp(colors.normalColor, Color.white, 0.2f);
            colors.pressedColor = Color.Lerp(colors.normalColor, Color.black, 0.2f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(colors.normalColor.r, colors.normalColor.g, colors.normalColor.b, 0.55f);
            button.colors = colors;
            if (button.targetGraphic)
                button.targetGraphic.color = Color.white;

            _onSelected = onSelected;
            for (var i = 0; i < button.onClick.GetPersistentEventCount(); i++)
                button.onClick.SetPersistentListenerState(i, UnityEventCallState.Off);

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnButtonClicked);
        }

        private static Color GetNodeColor(EncounterMapNodeType nodeType)
        {
            switch (nodeType)
            {
                case EncounterMapNodeType.Encounter: return new Color(0.3f, 0.45f, 0.68f);
                case EncounterMapNodeType.Boss: return new Color(0.72f, 0.24f, 0.22f);
                case EncounterMapNodeType.Shop: return new Color(0.22f, 0.55f, 0.34f);
                case EncounterMapNodeType.Campfire: return new Color(0.72f, 0.36f, 0.15f);
                case EncounterMapNodeType.Treasure: return new Color(0.72f, 0.56f, 0.16f);
                case EncounterMapNodeType.Event: return new Color(0.5f, 0.36f, 0.68f);
                case EncounterMapNodeType.Unknown: return new Color(0.42f, 0.44f, 0.49f);
                default: return Color.gray;
            }
        }

        private void OnButtonClicked()
        {
            _onSelected?.Invoke();
        }
        
        public void SetStatus(EncounterButtonStatus targetStatus)
        {
            switch (targetStatus)
            {
                case EncounterButtonStatus.Active:
                    button.interactable = true;
                    if (completedImage)
                        completedImage.gameObject.SetActive(false);
                    break;
                case EncounterButtonStatus.Passive:
                    button.interactable = false;
                    if (completedImage)
                        completedImage.gameObject.SetActive(false);
                    break;
                case EncounterButtonStatus.Completed:
                    button.interactable = false;
                    if (completedImage)
                        completedImage.gameObject.SetActive(true);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(targetStatus), targetStatus, null);
            }
        }
    }
}