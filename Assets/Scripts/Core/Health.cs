using System;
using UnityEngine;

namespace LastStep.Core
{
    [DisallowMultipleComponent]
    public sealed class Health : MonoBehaviour
    {
        [SerializeField, Min(1)] private int maximum = 100;
        public int Current { get; private set; }
        public int Maximum => maximum;
        public bool IsAlive => Current > 0;
        public float InvulnerableUntil { get; set; }
        public event Action<Health> Changed;
        public event Action<Health> Defeated;
        public event Action<Health> Evaded;

        private void Awake() { ResetHealth(maximum); }

        public void ResetHealth(int value)
        {
            maximum = Mathf.Max(1, value);
            Current = maximum;
            InvulnerableUntil = 0f;
            Changed?.Invoke(this);
        }

        public bool ApplyDamage(int damage)
        {
            if (damage <= 0 || !IsAlive) return false;
            if (Time.time < InvulnerableUntil) { Evaded?.Invoke(this); return false; }
            Current = Mathf.Max(0, Current - damage);
            Changed?.Invoke(this);
            if (Current == 0) Defeated?.Invoke(this);
            return true;
        }
    }
}
