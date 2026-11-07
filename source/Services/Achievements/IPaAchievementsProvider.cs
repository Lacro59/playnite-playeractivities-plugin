using System;
using System.Collections.Generic;

namespace PlayerActivities.Services.Achievements
{
    /// <summary>
    /// Reads achievement unlocks / progression from an external provider (SuccessStory or PlayniteAchievements).
    /// </summary>
    public interface IPaAchievementsProvider
    {
        /// <summary>
        /// Gets the provider kind.
        /// </summary>
        AchievementsProviderKind Kind { get; }

        /// <summary>
        /// Gets a value indicating whether the provider plugin is installed and usable.
        /// </summary>
        bool IsAvailable { get; }

        /// <summary>
        /// Loads achievements for a Playnite game. Returns an empty snapshot when missing or on error.
        /// </summary>
        /// <param name="gameId">Playnite game Guid.</param>
        PaGameAchievementsData GetGameAchievements(Guid gameId);

        /// <summary>
        /// Enumerates Playnite game ids that have achievement data for a full scan.
        /// </summary>
        IEnumerable<Guid> EnumerateGameIds();
    }
}
