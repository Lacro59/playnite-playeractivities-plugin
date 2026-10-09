using System;

namespace PlayerActivities.Models
{
    /// <summary>
    /// Lightweight achievement row for PaView timeline strips (not persisted in PA DB).
    /// </summary>
    public sealed class PaAchievementDisplay
    {
        /// <summary>
        /// Gets or sets the achievement display name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the achievement description from SuccessStory JSON when present.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the unlocked icon URL or local path.
        /// </summary>
        public string IconUrl { get; set; }

        /// <summary>
        /// Gets or sets the local unlock date/time used for day filtering.
        /// </summary>
        public DateTime DateUnlocked { get; set; }

        /// <summary>
        /// Gets a multi-line tooltip (name + description when available).
        /// </summary>
        public string ToolTipText
        {
            get
            {
                string name = Name ?? string.Empty;
                if (string.IsNullOrWhiteSpace(Description))
                {
                    return name;
                }

                if (string.IsNullOrWhiteSpace(name))
                {
                    return Description.Trim();
                }

                return name + Environment.NewLine + Description.Trim();
            }
        }
    }
}
