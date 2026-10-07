using PlayerActivities.Models;
using PlayerActivities.Services;
using System;
using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using CommonPluginsShared.Controls;

namespace PlayerActivities.Controls
{
    /// <summary>
    /// Horizontal thumbnail strip for ScreenshotsTaken activities, fed from ExtensionsData JSON (no plugin host / ffprobe).
    /// </summary>
    public class PaScreenshotStrip : PaImageStripBase
    {
        private const double ThumbnailHeight = 64d;
        private const int MaxThumbnails = 8;

        /// <inheritdoc />
        protected override double ItemHeight => ThumbnailHeight;

        /// <inheritdoc />
        protected override double? ItemWidth => null;

        /// <inheritdoc />
        protected override int MaxItems => MaxThumbnails;

        /// <inheritdoc />
        protected override double ItemRightMargin => 6d;

        /// <inheritdoc />
        protected override string LogPrefix => nameof(PaScreenshotStrip);

        /// <summary>
        /// Gets or sets the activity day used to filter screenshots (same binding name as the former SV host).
        /// </summary>
        public DateTime DateTaked
        {
            get => (DateTime)GetValue(DateTakedProperty);
            set => SetValue(DateTakedProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="DateTaked"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty DateTakedProperty = DependencyProperty.Register(
            nameof(DateTaked),
            typeof(DateTime),
            typeof(PaScreenshotStrip),
            new FrameworkPropertyMetadata(default(DateTime), OnDayChanged));

        /// <inheritdoc />
        protected override void ConfigureItemImage(FrameworkElementFactory imageFactory)
        {
            imageFactory.SetBinding(ImageAsync.SourceProperty, new Binding(nameof(PaScreenshotDisplay.FilePath)));
            imageFactory.SetBinding(FrameworkElement.ToolTipProperty, new Binding(nameof(PaScreenshotDisplay.FilePath)));
        }

        /// <inheritdoc />
        protected override DateTime GetFilterDay() => DateTaked;

        /// <inheritdoc />
        protected override IEnumerable LoadItems(Guid gameId, DateTime day)
        {
            return PaExternalPluginDataReader.Instance.GetExistingScreenshotsForDay(gameId, day, MaxItems);
        }

        private static void OnDayChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            PaScreenshotStrip strip = d as PaScreenshotStrip;
            strip?.ScheduleRefresh();
        }
    }
}
