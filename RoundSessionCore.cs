using System;
using System.Collections.Generic;


namespace RemoteTriviaCore
{
    public class TeamState
    {
        public int TeamId { get; }
        public bool IsPressed { get; private set; }
        public DateTime? PressTime { get; private set; }

        public TeamState(int teamId)
        {
            TeamId = teamId;
            IsPressed = false;
        }

        public void RegisterPress()
        {
            if (!IsPressed)
            {
                IsPressed = true;
                PressTime = DateTime.UtcNow;
            }
        }

        public void Reset()
        {
            IsPressed = false;
            PressTime = null;
        }
    }

    public class RoundSessionCore
    {
        private readonly Dictionary<int, TeamState> _teams;
        private readonly Dictionary<int, int> _scores;
        private bool _active;

        public RoundSessionCore(int teamCount)
        {
            _teams = new Dictionary<int, TeamState>();
            _scores = new Dictionary<int, int>();
            for (int i = 1; i <= teamCount; i++)
            {
                _teams[i] = new TeamState(i);
                _scores[i] = 0;
            }
            _active = true;
        }

        public void RegisterTeam(int teamId)
        {
            if (!_active || !_teams.ContainsKey(teamId)) return;

            var team = _teams[teamId];
            if (!team.IsPressed)
            {
                team.RegisterPress();

                if (GetFirstTeamId() == teamId)
                {
                    _active = false;
                }
            }
        }

        public int? GetFirstTeamId()
        {
            DateTime? earliest = null;
            int? result = null;

            foreach (var kvp in _teams)
            {
                if (kvp.Value.IsPressed)
                {
                    if (earliest == null || kvp.Value.PressTime < earliest)
                    {
                        earliest = kvp.Value.PressTime;
                        result = kvp.Key;
                    }
                }
            }

            return result;
        }

        public void ResetRound()
        {
            foreach (var team in _teams.Values)
            {
                team.Reset();
            }
            _active = true;
        }

        public IReadOnlyDictionary<int, TeamState> GetTeamStates() => _teams;

        /// <summary>Додає одному балу команді з даним ID.</summary>
        public void AwardPoint(int teamId)
        {
            if (_scores.ContainsKey(teamId))
                _scores[teamId]++;
        }

        /// <summary>Повертає поточний рахунок команди (0, якщо такої нема).</summary>
        public int GetTeamScore(int teamId)
        {
            return _scores.TryGetValue(teamId, out var s) ? s : 0;
        }
    }
}
