using System;
using System.Collections.Generic;
using UnityEngine;

namespace BossRaid.Combat
{
    public enum StatModType
    {
        Flat,       // 단순 합산 (Base + Value)
        PercentAdd, // 합연산 배율 (Base * (1 + Value))
        PercentMult // 곱연산 배율 (Base * Value)
    }

    [Serializable]
    public class StatModifier
    {
        public float Value;
        public StatModType Type;
        public object Source; // 어떤 장비나 버프에서 왔는지 추적용

        public StatModifier(float value, StatModType type, object source = null)
        {
            Value = value;
            Type = type;
            Source = source;
        }
    }

    /// <summary>
    /// 개별 스탯의 기본값과 모디파이어를 관리하는 클래스
    /// </summary>
    public class ModifiableStat
    {
        public float BaseValue;
        private readonly List<StatModifier> _modifiers = new List<StatModifier>();
        
        public float Value { get; private set; }
        private bool _isDirty = true;

        public ModifiableStat(float baseValue)
        {
            BaseValue = baseValue;
            Value = baseValue;
        }

        public void AddModifier(StatModifier mod)
        {
            _isDirty = true;
            _modifiers.Add(mod);
        }

        public bool RemoveModifier(StatModifier mod)
        {
            if (_modifiers.Remove(mod))
            {
                _isDirty = true;
                return true;
            }
            return false;
        }

        public void RemoveAllModifiersFromSource(object source)
        {
            int removed = _modifiers.RemoveAll(m => m.Source == source);
            if (removed > 0) _isDirty = true;
        }

        public float GetValue()
        {
            if (_isDirty)
            {
                Value = CalculateFinalValue();
                _isDirty = false;
            }
            return Value;
        }

        private float CalculateFinalValue()
        {
            float finalValue = BaseValue;
            float sumPercentAdd = 0;

            // 1. Flat 우선 합산
            for (int i = 0; i < _modifiers.Count; i++)
            {
                var mod = _modifiers[i];
                if (mod.Type == StatModType.Flat)
                    finalValue += mod.Value;
                else if (mod.Type == StatModType.PercentAdd)
                    sumPercentAdd += mod.Value;
            }

            // 2. PercentAdd 적용 (예: +10%와 +20%가 있으면 합쳐서 1.3배)
            finalValue *= (1 + sumPercentAdd);

            // 3. PercentMult 적용 (곱연산 배율)
            for (int i = 0; i < _modifiers.Count; i++)
            {
                var mod = _modifiers[i];
                if (mod.Type == StatModType.PercentMult)
                    finalValue *= mod.Value;
            }

            return (float)Math.Round(finalValue, 4);
        }
    }
}
