using System;

namespace RemoteTriviaCore
{
    /// <summary>
    /// Клас, який представляє стан команди у процесі гри.
    /// </summary>
    public class TeamStateUnit
    {
        private readonly Func<DateTime> _utcNowProvider;
        /// <summary>
        /// Ідентифікатор команди.
        /// </summary>
        public int TeamId { get; }

        /// <summary>
        /// Прапорець, що вказує, чи команда активна для відповіді.
        /// </summary>
        public bool IsActive { get; private set; }

        /// <summary>
        /// Час останньої активації команди.
        /// </summary>
        public DateTime? ActivationTime { get; private set; }

        /// <summary>
        /// Створення нового стану команди.
        /// </summary>
        /// <param name="teamId">Унікальний номер команди.</param>
        public TeamStateUnit(int teamId, Func<DateTime>? utcNow = null)
        {
            TeamId = teamId;
            IsActive = false;
            _utcNowProvider = utcNow ?? (() => DateTime.UtcNow);
        }

        /// <summary>
        /// Активувати команду, зафіксувавши час.
        /// </summary>
        public void Activate()
        {
            if (!IsActive)
            {
                IsActive = true;
                ActivationTime = DateTime.UtcNow;
            }
        }

        /// <summary>
        /// Скинути стан команди до початкового.
        /// </summary>
        public void Deactivate()
        {
            IsActive = false;
            ActivationTime = null;
        }

        public void RegisterPress()
        {
            if (!IsActive)
            {
                IsActive = true;
                ActivationTime = _utcNowProvider();
            }
        }
    }
}
