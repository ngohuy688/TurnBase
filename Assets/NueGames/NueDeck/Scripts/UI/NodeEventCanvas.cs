using System;
using System.Collections.Generic;
using NueGames.NueDeck.Scripts.Data.Containers;
using NueGames.NueDeck.Scripts.UI.Reward;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NueGames.NueDeck.Scripts.UI
{
    public class NodeEventCanvas : CanvasBase
    {
        [SerializeField] private GameObject eventPanel;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private Transform choicesRoot;
        [SerializeField] private Button choiceButtonPrefab;

        private readonly List<Button> _choiceButtons = new List<Button>();
        private Action _onResolved;
        private bool _resolved;

        public void OpenEvent(EventData eventData, Action onResolved)
        {
            if (!HasValidEventData(eventData) ||
                !eventPanel || !titleText || !descriptionText || !choicesRoot || !choiceButtonPrefab)
            {
                Debug.LogError("NodeEventCanvas is missing event data or required UI references.", this);
                return;
            }

            _resolved = false;
            _onResolved = onResolved;
            if (!DisplayEvent(eventData))
            {
                _onResolved = null;
                return;
            }

            eventPanel.SetActive(true);
            OpenCanvas();
        }

        private bool DisplayEvent(EventData eventData)
        {
            if (!HasValidEventData(eventData))
                return false;

            ClearChoices();
            titleText.text = eventData.EventTitle;
            descriptionText.text = eventData.Description;
            foreach (var choice in eventData.Choices)
            {
                if (choice == null) continue;
                var button = Instantiate(choiceButtonPrefab, choicesRoot, false);
                var label = button.GetComponentInChildren<TMP_Text>(true);
                if (!label)
                {
                    Debug.LogError("Event choice button needs a TextMeshPro label.", button);
                    Destroy(button.gameObject);
                    continue;
                }

                label.text = string.IsNullOrWhiteSpace(choice.Description)
                    ? choice.Title
                    : $"{choice.Title}\n{choice.Description}";
                button.onClick.AddListener(() => SelectChoice(choice));
                _choiceButtons.Add(button);
            }

            if (_choiceButtons.Count == 0)
            {
                Debug.LogError("Event has no valid choices.", eventData);
                return false;
            }

            return true;
        }

        private static bool HasValidEventData(EventData eventData)
        {
            return eventData && eventData.Choices != null &&
                   eventData.Choices.Exists(choice => choice != null);
        }

        public override void ResetCanvas()
        {
            ClearChoices();
            _onResolved = null;
            _resolved = false;
            if (eventPanel) eventPanel.SetActive(false);
        }

        private void SelectChoice(EventChoiceData choice)
        {
            if (_resolved || choice == null)
                return;

            SetChoicesInteractable(false);

            if (choice.Effects != null)
            {
                foreach (var effect in choice.Effects)
                {
                    if (!NodeRewardService.Apply(effect))
                        Debug.LogWarning($"Could not apply event reward '{effect?.EffectType}'.", this);
                }
            }

            if (choice.NextEventData)
            {
                if (!HasValidEventData(choice.NextEventData))
                {
                    Debug.LogError($"Event choice '{choice.Title}' references an event with no valid choices.", choice.NextEventData);
                    SetChoicesInteractable(true);
                    return;
                }

                DisplayEvent(choice.NextEventData);
                return;
            }

            if (!GameManager || !GameManager.CompleteCurrentMapNode())
            {
                SetChoicesInteractable(true);
                return;
            }

            _resolved = true;
            var callback = _onResolved;
            _onResolved = null;
            CloseCanvas();
            callback?.Invoke();
        }

        private void SetChoicesInteractable(bool interactable)
        {
            foreach (var button in _choiceButtons)
                if (button) button.interactable = interactable;
        }

        private void ClearChoices()
        {
            foreach (var button in _choiceButtons)
                if (button) Destroy(button.gameObject);
            _choiceButtons.Clear();
        }
    }
}
