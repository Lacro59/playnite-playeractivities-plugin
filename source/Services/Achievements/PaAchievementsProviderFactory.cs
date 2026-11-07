namespace PlayerActivities.Services.Achievements
{
    /// <summary>
    /// Creates the achievements provider adapter for the selected kind (StartPage-style routing).
    /// </summary>
    public static class PaAchievementsProviderFactory
    {
        /// <summary>
        /// Returns an adapter for <paramref name="kind"/>. Defaults to SuccessStory when unknown.
        /// </summary>
        /// <param name="kind">Selected provider.</param>
        public static IPaAchievementsProvider Create(AchievementsProviderKind kind)
        {
            switch (kind)
            {
                case AchievementsProviderKind.PlayniteAchievements:
                    return new PlayniteAchievementsDataProvider();
                case AchievementsProviderKind.SuccessStory:
                default:
                    return new SuccessStoryAchievementsProvider();
            }
        }
    }
}
