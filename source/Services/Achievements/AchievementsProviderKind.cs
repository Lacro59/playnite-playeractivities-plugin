namespace PlayerActivities.Services.Achievements
{
    /// <summary>
    /// Achievements data source available to PlayerActivities (StartPage-style routing).
    /// </summary>
    public enum AchievementsProviderKind
    {
        /// <summary>
        /// SuccessStory JSON under ExtensionsData (default).
        /// </summary>
        SuccessStory = 0,

        /// <summary>
        /// PlayniteAchievements SQLite cache (<c>achievement_cache.db</c>).
        /// </summary>
        PlayniteAchievements = 1
    }
}
