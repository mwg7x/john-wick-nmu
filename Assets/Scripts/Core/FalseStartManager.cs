using System;
using UnityEngine;

namespace LastStep.Core
{
    [DisallowMultipleComponent]
    public sealed class FalseStartManager : MonoBehaviour
    {
        [SerializeField] private DuelManager duel;
        private int offenses;
        public int Offenses => offenses;
        public bool PlayerDisqualified { get; private set; }
        public bool EnemyDisqualified { get; private set; }
        public event Action<int, bool> FalseStart;

        public bool ReportEarlyTrigger()
        {
            if (duel == null || duel.Settings == null || duel.CurrentState != GameState.Countdown) return false;
            offenses++;
            PlayerDisqualified = true;
            FalseStart?.Invoke(offenses, true);
            duel.TryDeclareDefeat();
            return true;
        }
        public bool ReportEnemyEarlyTrigger()
        {
            if (duel == null || duel.CurrentState != GameState.Countdown) return false;
            EnemyDisqualified = true;
            duel.TryDeclareEnemyFalseStartVictory();
            return true;
        }
    }
}
