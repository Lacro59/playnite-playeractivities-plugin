using PlayerActivities.Models;
using PlayerActivities.Services;
using System;
using System.Collections;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using CommonPluginsShared.Controls;

namespace PlayerActivities.Controls
{
    /// <summary>
    /// Horizontal icon strip for AchievementsUnlocked activities, fed from ExtensionsData JSON (no SuccessStory host).
    /// Filters unlocked achievements to the activity day (unlike PluginCompact which shows all unlocked).
    /// </summary>
    public class PaAchievementStrip : PaImageStripBase
    {
        private const double IconSize = 48d;
        private const int MaxIcons = 12;

        /// <inheritdoc />
        protected override double ItemHeight => IconSize;

        /// <inheritdoc />
        protected override double? ItemWidth => IconSize;

        /// <inheritdoc />
        protected override int MaxItems => MaxIcons;

        /// <inheritdoc />
        protected override double ItemRightMargin => 6d;

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
        protected override FrameworkElementFactory CreateItemFactory()
        {
            FrameworkElementFactory borderFactory = new FrameworkElementFactory(typeof(Border));
            borderFactory.SetValue(FrameworkElement.WidthProperty, IconSize);
            borderFactory.SetValue(FrameworkElement.HeightProperty, IconSize);
            borderFactory.SetValue(FrameworkElement.MarginProperty, new Thickness(0, 0, ItemRightMargin, 0));
            borderFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(2));
            borderFactory.SetValue(Border.SnapsToDevicePixelsProperty, true);
            borderFactory.SetResourceReference(Border.BorderBrushProperty, "NormalBorderBrush");
            borderFactory.SetResourceReference(Border.BorderThicknessProperty, "ControlBorderThickness");
            borderFactory.SetResourceReference(Border.BackgroundProperty, "PopupBackgroundBrush");

            double innerSize = IconSize - 4d;
            FrameworkElementFactory imageFactory = new FrameworkElementFactory(typeof(ImageAsync));
            imageFactory.SetValue(ImageAsync.DecodePixelHeightProperty, innerSize);
            imageFactory.SetValue(FrameworkElement.HeightProperty, innerSize);
            imageFactory.SetValue(FrameworkElement.WidthProperty, innerSize);
            imageFactory.SetValue(Image.StretchProperty, Stretch.Uniform);
            imageFactory.SetValue(FrameworkElement.MarginProperty, new Thickness(2));
            imageFactory.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            imageFactory.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
            ConfigureItemImage(imageFactory);

            borderFactory.AppendChild(imageFactory);
            return borderFactory;
        }

        /// <inheritdoc />
        protected override void ConfigureItemImage(FrameworkElementFactory imageFactory)
        {
            imageFactory.SetBinding(ImageAsync.SourceProperty, new Binding(nameof(PaAchievementDisplay.IconUrl)));
            imageFactory.SetBinding(FrameworkElement.ToolTipProperty, new Binding(nameof(PaAchievementDisplay.ToolTipText)));
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
