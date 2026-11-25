using System;
using System.Collections;
using System.Collections.Generic;
using NueGames.NueDeck.Scripts.Characters;
using NueGames.NueDeck.Scripts.Card.CardActions;
using NueGames.NueDeck.Scripts.Characters.Enemies;
using NueGames.NueDeck.Scripts.Data.Containers;
using NueGames.NueDeck.Scripts.Enums;
using NueGames.NueDeck.Scripts.Relic;
using NueGames.NueDeck.Scripts.UI;
using NueGames.NueDeck.Scripts.Utils.Background;
using UnityEngine;

namespace NueGames.NueDeck.Scripts.Managers
{
    public class CombatManager : MonoBehaviour
    {
        private CombatManager(){}
        public static CombatManager Instance { get; private set; }

        [Header("References")] 
        [SerializeField] private BackgroundContainer backgroundContainer;
        [SerializeField] private List<Transform> enemyPosList;
        [SerializeField] private List<Transform> allyPosList;
 
        
        #region Cache
        public List<EnemyBase> CurrentEnemiesList { get; private set; } = new List<EnemyBase>();
        public List<AllyBase> CurrentAlliesList { get; private set; }= new List<AllyBase>();

        public Action OnAllyTurnStarted;
        public Action OnEnemyTurnStarted;
        public List<Transform> EnemyPosList => enemyPosList;

        public List<Transform> AllyPosList => allyPosList;

        public AllyBase CurrentMainAlly => CurrentAlliesList.Count>0 ? CurrentAlliesList[0] : null;

        public EnemyEncounter CurrentEncounter { get; private set; }
        
        public CombatStateType CurrentCombatStateType
        {
            get => _currentCombatStateType;
            private set
            {
                ExecuteCombatState(value);
                _currentCombatStateType = value;
            }
        }
        
        private CombatStateType _currentCombatStateType;
        private bool _hasPlayedFirstCardThisTurn;
        private bool _hasDealtDamageThisTurn;
        private bool _hasTakenDamageThisTurn;
        protected FxManager FxManager => FxManager.Instance;
        protected AudioManager AudioManager => AudioManager.Instance;
        protected GameManager GameManager => GameManager.Instance;
        protected UIManager UIManager => UIManager.Instance;

        protected CollectionManager CollectionManager => CollectionManager.Instance;

        #endregion
        
        
        #region Setup
        private void Awake()
        {
            if (Instance)
            {
                Destroy(gameObject);
                return;
            } 
            else
            {
                Instance = this;
                CurrentCombatStateType = CombatStateType.PrepareCombat;
            }
        }

        private void Start()
        {
            StartCombat();
        }

        public void StartCombat()
        {
            ReduceNextCardCostAction.Reset();
            if (!BuildEnemies())
            {
                CurrentCombatStateType = CombatStateType.EndCombat;
                return;
            }

            if (!BuildAllies())
            {
                CurrentCombatStateType = CombatStateType.EndCombat;
                return;
            }

            backgroundContainer.OpenSelectedBackground();
          
            CollectionManager.SetGameDeck();
            RelicEffectProcessor.Trigger(RelicCondition.OnBattleStart);
           
            UIManager.CombatCanvas.gameObject.SetActive(true);
            UIManager.InformationCanvas.gameObject.SetActive(true);
            CurrentCombatStateType = CombatStateType.AllyTurn;
            if (UIManager && UIManager.CombatCanvas)
                UIManager.CombatCanvas.SetPileTexts();
        }

        public void TriggerFirstCardPlayed(RelicEffectParameters parameters)
        {
            if (_hasPlayedFirstCardThisTurn)
                return;

            _hasPlayedFirstCardThisTurn = true;
            RelicEffectProcessor.Trigger(RelicCondition.OnFirstCardPlayed, parameters);
        }

        public void TriggerFirstDamageDealt(RelicEffectParameters parameters)
        {
            if (_hasDealtDamageThisTurn)
                return;

            _hasDealtDamageThisTurn = true;
            RelicEffectProcessor.Trigger(RelicCondition.OnFirstDamageDealt, parameters);
        }

        public void TriggerFirstDamageTaken(RelicEffectParameters parameters)
        {
            if (_hasTakenDamageThisTurn)
                return;

            _hasTakenDamageThisTurn = true;
            RelicEffectProcessor.Trigger(RelicCondition.OnFirstDamageTaken, parameters);
        }
        
        private void ExecuteCombatState(CombatStateType targetStateType)
        {
            switch (targetStateType)
            {
                case CombatStateType.PrepareCombat:
                    break;
                case CombatStateType.AllyTurn:
                    _hasPlayedFirstCardThisTurn = false;
                    _hasDealtDamageThisTurn = false;
                    _hasTakenDamageThisTurn = false;
                    OnAllyTurnStarted?.Invoke();
                    
                    if (CurrentMainAlly.CharacterStats.IsStunned)
                    {
                        EndTurn();
                        return;
                    }

                    RelicEffectProcessor.Trigger(RelicCondition.OnTurnStart);
                   
                    CollectionManager.DrawCards(GameManager.PersistentGameplayData.DrawCount);
                    
                    GameManager.PersistentGameplayData.CanSelectCards = true;
                    GameManager.PersistentGameplayData.CurrentMana = GameManager.PersistentGameplayData.MaxMana + GameManager.PersistentGameplayData.ManaCache;
                    ResetManaCache();
                    if (UIManager && UIManager.CombatCanvas)
                        UIManager.CombatCanvas.SetPileTexts();
                    break;
                case CombatStateType.EnemyTurn:
                    _hasDealtDamageThisTurn = false;
                    _hasTakenDamageThisTurn = false;
                    OnEnemyTurnStarted?.Invoke();
                    
                    CollectionManager.DiscardHand();
                    
                    StartCoroutine(nameof(EnemyTurnRoutine));
                    
                    GameManager.PersistentGameplayData.CanSelectCards = false;
                    break;
                case CombatStateType.EndCombat:
                    ReduceNextCardCostAction.Reset();
                    GameManager.PersistentGameplayData.CanSelectCards = false;
                    
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(targetStateType), targetStateType, null);
            }
        }
        #endregion

        #region Public Methods
        public void EndTurn()
        {
            if (CurrentCombatStateType == CombatStateType.AllyTurn)
                RelicEffectProcessor.Trigger(RelicCondition.OnTurnEnd);
            CurrentCombatStateType = CombatStateType.EnemyTurn;
        }
        public void OnAllyDeath(AllyBase targetAlly)
        {
            var targetAllyData = GameManager.PersistentGameplayData.AllyList.Find(x =>
                x.AllyCharacterData.CharacterID == targetAlly.AllyCharacterData.CharacterID);
            if (GameManager.PersistentGameplayData.AllyList.Count>1)
                GameManager.PersistentGameplayData.AllyList.Remove(targetAllyData);
            CurrentAlliesList.Remove(targetAlly);
            UIManager.InformationCanvas.ResetCanvas();
            if (CurrentAlliesList.Count<=0)
                LoseCombat();
        }
        public void OnEnemyDeath(EnemyBase targetEnemy)
        {
            CurrentEnemiesList.Remove(targetEnemy);
            if (CurrentEnemiesList.Count<=0)
                WinCombat();
        }
        public void DeactivateCardHighlights()
        {
            foreach (var currentEnemy in CurrentEnemiesList)
                currentEnemy.EnemyCanvas.SetHighlight(false);

            foreach (var currentAlly in CurrentAlliesList)
                currentAlly.AllyCanvas.SetHighlight(false);
        }
        public void IncreaseMana(int target)
        {
            GameManager.PersistentGameplayData.ManaCache += target;
            GameManager.PersistentGameplayData.CurrentMana += target;
            if (UIManager && UIManager.CombatCanvas)
                UIManager.CombatCanvas.SetPileTexts();
        }
        public void IncreaseMaxMana(int target)
        {
            GameManager.PersistentGameplayData.MaxMana += target;
            GameManager.PersistentGameplayData.CurrentMana += target;
            if (UIManager && UIManager.CombatCanvas)
                UIManager.CombatCanvas.SetPileTexts();
        }
        public void ResetManaCache()
        {
            GameManager.PersistentGameplayData.ManaCache = 0;
        }
        public void HighlightCardTarget(TargetType targetTypeTargetType)
        {
            switch (targetTypeTargetType)
            {
                case TargetType.Enemy:
                    foreach (var currentEnemy in CurrentEnemiesList)
                        currentEnemy.EnemyCanvas.SetHighlight(true);
                    break;
                case TargetType.Ally:
                    foreach (var currentAlly in CurrentAlliesList)
                        currentAlly.AllyCanvas.SetHighlight(true);
                    break;
                case TargetType.AllEnemies:
                    foreach (var currentEnemy in CurrentEnemiesList)
                        currentEnemy.EnemyCanvas.SetHighlight(true);
                    break;
                case TargetType.AllAllies:
                    foreach (var currentAlly in CurrentAlliesList)
                        currentAlly.AllyCanvas.SetHighlight(true);
                    break;
                case TargetType.RandomEnemy:
                    foreach (var currentEnemy in CurrentEnemiesList)
                        currentEnemy.EnemyCanvas.SetHighlight(true);
                    break;
                case TargetType.RandomAlly:
                    foreach (var currentAlly in CurrentAlliesList)
                        currentAlly.AllyCanvas.SetHighlight(true);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(targetTypeTargetType), targetTypeTargetType, null);
            }
        }
        #endregion
        
        #region Private Methods
        private bool BuildEnemies()
        {
            if (EnemyPosList == null || EnemyPosList.Count == 0)
            {
                Debug.LogError("CombatManager requires at least one enemy spawn position.", this);
                return false;
            }

            if (!GameManager.EncounterData.TryGetMapNode(
                    GameManager.PersistentGameplayData.CurrentStageId,
                    GameManager.PersistentGameplayData.CurrentMapNodeId,
                    out var mapNode))
                return false;

            CurrentEncounter = GameManager.EncounterData.GetEnemyEncounter(
                GameManager.PersistentGameplayData.CurrentStageId,
                mapNode);
            if (CurrentEncounter == null || CurrentEncounter.EnemyList == null || CurrentEncounter.EnemyList.Count == 0)
            {
                Debug.LogError(
                    $"Map node '{mapNode.NodeId}' has no enemies configured.",
                    GameManager.EncounterData);
                return false;
            }
            
            var enemyList = CurrentEncounter.EnemyList;
            for (var i = 0; i < enemyList.Count; i++)
            {
                var spawnPosition = i < EnemyPosList.Count ? EnemyPosList[i] : EnemyPosList[0];
                var clone = Instantiate(enemyList[i].EnemyPrefab, spawnPosition);
                clone.BuildCharacter();
                CurrentEnemiesList.Add(clone);
            }

            return true;
        }
        private bool BuildAllies()
        {
            var allies = GameManager.PersistentGameplayData.AllyList;
            if (allies == null || allies.Count == 0 || AllyPosList == null || AllyPosList.Count == 0)
            {
                Debug.LogError("CombatManager requires at least one ally and one ally spawn position.", this);
                return false;
            }

            for (var i = 0; i < allies.Count; i++)
            {
                var spawnPosition = i < AllyPosList.Count ? AllyPosList[i] : AllyPosList[0];
                var clone = Instantiate(allies[i], spawnPosition);
                clone.BuildCharacter();
                CurrentAlliesList.Add(clone);
            }

            return true;
        }
        private void LoseCombat()
        {
            if (CurrentCombatStateType == CombatStateType.EndCombat) return;
            
            RelicEffectProcessor.Trigger(RelicCondition.OnBattleEnd);
            CurrentCombatStateType = CombatStateType.EndCombat;
            
            CollectionManager.DiscardHand();
            CollectionManager.DiscardPile.Clear();
            CollectionManager.DrawPile.Clear();
            CollectionManager.HandPile.Clear();
            CollectionManager.HandController.hand.Clear();
            UIManager.CombatCanvas.gameObject.SetActive(true);
            UIManager.CombatCanvas.CombatLosePanel.SetActive(true);
        }
        private void WinCombat()
        {
            if (CurrentCombatStateType == CombatStateType.EndCombat) return;
          
            RelicEffectProcessor.Trigger(RelicCondition.OnBattleEnd);
            CurrentCombatStateType = CombatStateType.EndCombat;
           
            foreach (var allyBase in CurrentAlliesList)
            {
                GameManager.PersistentGameplayData.SetAllyHealthData(allyBase.AllyCharacterData.CharacterID,
                    allyBase.CharacterStats.CurrentHealth, allyBase.CharacterStats.MaxHealth);
            }
            
            CollectionManager.ClearPiles();

            if (!GameManager.CompleteCurrentMapNode())
                return;
            
            if (GameManager.PersistentGameplayData.IsFinalEncounter)
            {
                var hasNextStage = GameManager.EncounterData.TryGetNextStageId(
                    GameManager.PersistentGameplayData.CurrentStageId, out _);
                if (hasNextStage)
                {
                    foreach (var allyBase in CurrentAlliesList)
                        allyBase.CharacterStats.ClearAllStatus();

                    GameManager.PendingStageAdvanceAfterRewards = true;
                    UIManager.CombatCanvas.gameObject.SetActive(false);
                    UIManager.SetCanvas(UIManager.RewardCanvas, true, true);
                    UIManager.RewardCanvas.PrepareCanvas();
                    UIManager.RewardCanvas.BuildReward(RewardType.Gold);
                    UIManager.RewardCanvas.BuildReward(RewardType.Card);
                    UIManager.RewardCanvas.BuildReward(RewardType.Relic);
                    return;
                }

                var winPanel = UIManager.CombatCanvas.CombatWinPanel.GetComponent<CombatWinPanel>();
                if (winPanel != null)
                    winPanel.SetNextStageAvailable(hasNextStage);
                else
                    Debug.LogError("CombatWinPanel component is missing from the win panel.", UIManager.CombatCanvas);

                UIManager.CombatCanvas.CombatWinPanel.SetActive(true);
            }
            else
            {
                CurrentMainAlly.CharacterStats.ClearAllStatus();
                GameManager.PersistentGameplayData.CurrentEncounterId++;
                UIManager.CombatCanvas.gameObject.SetActive(false);
                UIManager.RewardCanvas.gameObject.SetActive(true);
                UIManager.RewardCanvas.PrepareCanvas();
                UIManager.RewardCanvas.BuildReward(RewardType.Gold);
                UIManager.RewardCanvas.BuildReward(RewardType.Card);
            }
           
        }
        #endregion
        
        #region Routines
        private IEnumerator EnemyTurnRoutine()
        {
            var waitDelay = new WaitForSeconds(0.1f);

            foreach (var currentEnemy in CurrentEnemiesList)
            {
                yield return currentEnemy.StartCoroutine(nameof(EnemyExample.ActionRoutine));
                yield return waitDelay;
            }

            if (CurrentCombatStateType != CombatStateType.EndCombat)
                CurrentCombatStateType = CombatStateType.AllyTurn;
        }
        #endregion
    }
}