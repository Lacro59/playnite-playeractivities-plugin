using Playnite.SDK.Data;
using System;

namespace PlayerActivities.Models.SuccessStory
{
    public class Achievements
    {
        private DateTime? _dateUnlocked;

        /// <summary>
        /// Gets or sets the date and time when the achievement was unlocked.
        /// Converts to local time when reading, and ensures UTC storage when setting.
        /// If the provided value is DateTime.MinValue, it is treated as null.
        /// </summary>
        public DateTime? DateUnlocked
        {
            get
            {
                if (_dateUnlocked.HasValue && _dateUnlocked.Value == DateTime.MinValue)
                {
                    return null;
                }
                return _dateUnlocked?.ToLocalTime();
            }
            set
            {
                if (!value.HasValue || value.Value == default)
                {
                    _dateUnlocked = null;
                }
                else
                {
                    _dateUnlocked = value.Value.ToUniversalTime();
                }
            }
        }

        /// <summary>
        /// Gets or sets the local date and time when the achievement was unlocked, or null if not unlocked.
        /// </summary>
        [DontSerialize]
        public DateTime? DateWhenUnlocked
        {
            get => DateUnlocked == null || DateUnlocked.Value.Year == 0001 || DateUnlocked.Value.Year == 1982
                    ? null
                    : (DateTime?)((DateTime)DateUnlocked).ToLocalTime();
            set => DateUnlocked = value;
        }

        /// <summary>
        /// Gets a value indicating whether the achievement is unlocked.
        /// </summary>
        [DontSerialize]
        public bool IsUnlock => DateWhenUnlocked != null || DateUnlocked.ToString().Contains("1982");
    }
}