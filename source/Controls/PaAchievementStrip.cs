using PlayerActivities.Models;
using PlayerActivities.Services;
using System;
using System.Collections;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using CommonPluginsShared.Controls;

namespace PlayerActivities.Controls
{
    /// <summary>
    /// Horizontal icon strip for AchievementsUnlocked activities, fed from ExtensionsData JSON (no SuccessStory host).
    /// Filters unlocked achievements to the activity day (unlike PluginCompact which shows all unlocked).
    /// </summary>
    public class PaAchievementStrip : PaImageStripBase
    {
        private const double IconSize = 40d;
        private const int MaxIcons = 12;

        /// <inheritdoc />
        protected override double ItemHeight => IconSize;

        /// <inheritdoc />
        protected override double? ItemWidth => IconSize;

        /// <inheritdoc />
        protected override int MaxItems => MaxIcons;

        /// <inheritdoc />
        protected override double ItemRightMargin => 4d;

        /// <inheritdoc />
        protected override string LogPrefix => nameof(PaAchievementStrip);

        /// <summary>
        /// Gets or sets the activity day used to filter unlocks (same binding name as the former SS host).
        /// </summary>
        public DateTime DateUnlocked
        {
            get => (DateTime)GetValue(DateUnlockedProperty);
            set => SetValue(DateUnlockedProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="DateUnlocked"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty DateUnlockedProperty = DependencyProperty.Register(
            nameof(DateUnlocked),
            typeof(DateTime),
            typeof(PaAchievementStrip),
            new FrameworkPropertyMetadata(default(DateTime), OnDayChanged));

        /// <inheritdoc />
        protected override void ConfigureItemImage(FrameworkElementFactory imageFactory)
        {
            imageFactory.SetBinding(ImageAsync.SourceProperty, new Binding(nameof(PaAchievementDisplay.IconUrl)));
            imageFactory.SetBinding(FrameworkElement.ToolTipProperty, new Binding(nameof(PaAchievementDisplay.Name)));
        }

        /// <inheritdoc />
        protected override DateTime GetFilterDay() => DateUnlocked;

        /// <inheritdoc />
        protected override IEnumerable LoadItems(Guid gameId, DateTime day)
        {
            return PaExternalPluginDataReader.Instance
                .GetUnlockedForDay(gameId, day)
                .Where(x => !string.IsNullOrEmpty(x.IconUrl))
                .Take(MaxItems)
                .ToList();
        }

        private static void OnDayChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            PaAchievementStrip strip = d as PaAchievementStrip;
            strip?.ScheduleRefresh();
        }
    }
}
