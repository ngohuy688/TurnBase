using System;
using System.Collections.Generic;
using System.Linq;
using NueGames.NueDeck.Scripts.Enums;
using NueGames.NueDeck.Scripts.NueExtentions;
using UnityEngine;

namespace NueGames.NueDeck.Scripts.Characters
{
    public class StatusStats
    {
        public StatusType StatusType { get; set; }
        public int StatusValue { get; set; }
        public bool DecreaseOverTurn { get; set; } // If true, decrease when the status owner's turn starts
        public bool IsPermanent { get; set; } // If true, status can not be cleared during combat
        public bool IsActive { get; set; }
        public bool CanNegativeStack { get; set; }
        public bool ClearAtNextTurn { get; set; }
        public bool SkipNextDecay { get; set; }

        public Action OnTriggerAction;
        public StatusStats(StatusType statusType, int statusValue, bool decreaseOverTurn = false, bool isPermanent = false, bool isActive = false, bool canNegativeStack = false, bool clearAtNextTurn = false)
        {
            StatusType = statusType;
            StatusValue = statusValue;
            DecreaseOverTurn = decreaseOverTurn;
            IsPermanent = isPermanent;
            IsActive = isActive;
            CanNegativeStack = canNegativeStack;
            ClearAtNextTurn = clearAtNextTurn;
        }
    }

    public class CharacterStats
    {
        public int MaxHealth { get; set; }
        public int CurrentHealth { get; set; }
        public bool IsStunned { get; set; }
        public bool IsDeath { get; private set; }
        public int StrengthModifier =>
            (StatusDict[StatusType.Strength].IsActive ? StatusDict[StatusType.Strength].StatusValue : 0) -
            (StatusDict[StatusType.StrengthDown].IsActive
                ? Mathf.Abs(StatusDict[StatusType.StrengthDown].StatusValue)
                : 0) -
            (StatusDict[StatusType.Weak].IsActive ? StatusDict[StatusType.Weak].StatusValue : 0);
        public float DamageModifierPercent =>
            50f * ((StatusDict[StatusType.DamageUp].StatusValue > 0 ? 1 : 0) -
                   (StatusDict[StatusType.DamageDown].StatusValue > 0 ? 1 : 0));

        public Action OnDeath;
        public Action<int, int> OnHealthChanged;
        private readonly Action<StatusType, int> OnStatusChanged;
        private readonly Action<StatusType, int> OnStatusApplied;
        private readonly Action<StatusType> OnStatusCleared;
        public Action OnHealAction;
        public Action OnTakeDamageAction;
        public Action OnShieldGained;

        public readonly Dictionary<StatusType, StatusStats> StatusDict = new Dictionary<StatusType, StatusStats>();

        #region Setup
        public CharacterStats(int maxHealth, CharacterCanvas characterCanvas)
        {
            MaxHealth = maxHealth;
            CurrentHealth = maxHealth;
            SetAllStatus();

            OnHealthChanged += characterCanvas.UpdateHealthText;
            OnStatusChanged += characterCanvas.UpdateStatusText;
            OnStatusApplied += characterCanvas.ApplyStatus;
            OnStatusCleared += characterCanvas.ClearStatus;
        }

        private void SetAllStatus()
        {
            foreach (StatusType statusType in Enum.GetValues(typeof(StatusType)))
            {
                if (statusType == StatusType.None) continue;
                if ((statusType & (statusType - 1)) != 0) continue; // skip combined flags
                StatusDict.Add(statusType, new StatusStats(statusType, 0));
            }

            StatusDict[StatusType.Poison].DecreaseOverTurn = true;
            StatusDict[StatusType.Poison].OnTriggerAction += DamagePoison;

            StatusDict[StatusType.Block].ClearAtNextTurn = true;

            StatusDict[StatusType.Dexterity].DecreaseOverTurn = true;

            StatusDict[StatusType.Stun].DecreaseOverTurn = true;
            StatusDict[StatusType.Stun].OnTriggerAction += CheckStunStatus;

            StatusDict[StatusType.Vulnerable].DecreaseOverTurn = true;

            StatusDict[StatusType.StrengthDown].DecreaseOverTurn = true;
            StatusDict[StatusType.DamageUp].DecreaseOverTurn = true;
            StatusDict[StatusType.DamageDown].DecreaseOverTurn = true;
            StatusDict[StatusType.Thorns].ClearAtNextTurn = true;
        }
        #endregion

        #region Public Methods
        public void ApplyStatus(StatusType targetStatus, int value)
        {
            if (targetStatus == StatusType.StrengthDown)
                value = Mathf.Abs(value);

            if (StatusDict[targetStatus].IsActive)
            {
                StatusDict[targetStatus].StatusValue += value;
                OnStatusChanged?.Invoke(targetStatus, StatusDict[targetStatus].StatusValue);
            }
            else
            {
                StatusDict[targetStatus].StatusValue = value;
                StatusDict[targetStatus].IsActive = true;
                StatusDict[targetStatus].SkipNextDecay = targetStatus == StatusType.StrengthDown;
                OnStatusApplied?.Invoke(targetStatus, StatusDict[targetStatus].StatusValue);
            }
        }

        public float ModifyOutgoingDamage(float value)
        {
            return Mathf.Max(0f, value * (100f + DamageModifierPercent) / 100f);
        }

        public float ModifyBlockValue(float value)
        {
            var multiplier = StatusDict[StatusType.Dexterity].StatusValue > 0 ? 1.5f : 1f;
            return Mathf.Max(0f, value * multiplier);
        }

        public void TriggerAllStatus()
        {
            foreach (StatusType statusType in Enum.GetValues(typeof(StatusType)))
            {
                if (statusType == StatusType.None) continue;
                if ((statusType & (statusType - 1)) != 0) continue;
                if (!StatusDict.ContainsKey(statusType)) continue;
                TriggerStatus(statusType);
            }
        }

        public void SetCurrentHealth(int targetCurrentHealth)
        {
            CurrentHealth = targetCurrentHealth <= 0 ? 1 : targetCurrentHealth;
            OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);
        }

        public void Heal(int value)
        {
            var previousHealth = CurrentHealth;
            CurrentHealth += value;
            if (CurrentHealth > MaxHealth) CurrentHealth = MaxHealth;

            if (CurrentHealth > previousHealth)
                OnHealAction?.Invoke();

            OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);
        }

        public void Damage(int value, bool canPierceArmor = false, CharacterBase attacker = null)
        {
            if (IsDeath) return;
            OnTakeDamageAction?.Invoke();
            var remainingDamage = value;

            if (!canPierceArmor)
            {
                if (StatusDict[StatusType.Block].IsActive)
                {
                    ApplyStatus(StatusType.Block, -value);

                    remainingDamage = 0;
                    if (StatusDict[StatusType.Block].StatusValue <= 0)
                    {
                        remainingDamage = StatusDict[StatusType.Block].StatusValue * -1;
                        ClearStatus(StatusType.Block);
                    }
                }
            }

            CurrentHealth -= remainingDamage;

            if (CurrentHealth <= 0)
            {
                CurrentHealth = 0;
                OnDeath?.Invoke();
                IsDeath = true;
            }
            OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);

            var reflectedDamage = StatusDict[StatusType.Thorns].StatusValue;
            if (value > 0 && attacker && !attacker.CharacterStats.IsDeath && reflectedDamage > 0)
                attacker.CharacterStats.Damage(reflectedDamage);
        }

        public void IncreaseMaxHealth(int value)
        {
            MaxHealth += value;
            CurrentHealth += value;
            OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);
        }

        public void ClearAllStatus()
        {
            var keys = StatusDict.Keys.ToList();
            foreach (var status in keys)
                ClearStatus(status);
        }

        public void ClearStatuses(StatusPolarity polarity)
        {
            var statuses = StatusDict.Keys
                .Where(status => status.GetPolarity() == polarity && StatusDict[status].IsActive)
                .ToList();

            foreach (var status in statuses)
                ClearStatus(status);
        }

        public void ClearStatus(StatusType targetStatus)
        {
            StatusDict[targetStatus].IsActive = false;
            StatusDict[targetStatus].StatusValue = 0;
            StatusDict[targetStatus].SkipNextDecay = false;
            if (targetStatus == StatusType.Stun)
                IsStunned = false;
            OnStatusCleared?.Invoke(targetStatus);
        }
        #endregion

        #region Private Methods
        private void TriggerStatus(StatusType targetStatus)
        {
            var status = StatusDict[targetStatus];
            if (!status.IsActive)
            {
                status.StatusValue = 0;
                status.SkipNextDecay = false;
                if (targetStatus == StatusType.Stun)
                    IsStunned = false;
                return;
            }

            if (status.StatusValue <= 0 && !status.CanNegativeStack)
            {
                if (!status.IsPermanent)
                    ClearStatus(targetStatus);
                if (targetStatus == StatusType.Stun)
                    IsStunned = false;
                return;
            }

            var shouldSkipTurn = targetStatus == StatusType.Stun && status.StatusValue > 0;
            status.OnTriggerAction?.Invoke();

            //One turn only statuses
            if (status.ClearAtNextTurn)
            {
                ClearStatus(targetStatus);
                OnStatusChanged?.Invoke(targetStatus, status.StatusValue);
                return;
            }

            //Check status
            if (status.StatusValue <= 0)
            {
                if (status.CanNegativeStack)
                {
                    if (status.StatusValue == 0 && !status.IsPermanent)
                        ClearStatus(targetStatus);
                }
                else
                {
                    if (!status.IsPermanent)
                        ClearStatus(targetStatus);
                }
            }

            if (status.DecreaseOverTurn)
            {
                if (status.SkipNextDecay)
                    status.SkipNextDecay = false;
                else
                    status.StatusValue--;
            }

            if (status.StatusValue == 0)
                if (!status.IsPermanent)
                    ClearStatus(targetStatus);

            if (targetStatus == StatusType.Stun)
                IsStunned = shouldSkipTurn;

            OnStatusChanged?.Invoke(targetStatus, status.StatusValue);
        }

        private void DamagePoison()
        {
            if (StatusDict[StatusType.Poison].StatusValue <= 0) return;
            Damage(StatusDict[StatusType.Poison].StatusValue, true);
        }

        public void CheckStunStatus()
        {
            if (StatusDict[StatusType.Stun].StatusValue <= 0)
            {
                IsStunned = false;
                return;
            }

            IsStunned = true;
        }
        #endregion
    }
}
