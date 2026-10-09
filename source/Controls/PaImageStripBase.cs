using CommonPluginsShared;
using CommonPluginsShared.Controls;
using Playnite.SDK.Models;
using System;
using System.Collections;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace PlayerActivities.Controls
{
    /// <summary>
    /// Shared horizontal <see cref="ImageAsync"/> strip with coalesced refresh, background load, and unload release.
    /// </summary>
    public abstract class PaImageStripBase : ContentControl
    {
        private readonly ItemsControl _itemsControl;
        private bool _refreshScheduled;
        private string _appliedKey = string.Empty;
        private int _refreshGeneration;

        /// <summary>
        /// Gets or sets the game whose ExtensionsData JSON is read.
        /// </summary>
        public Game GameContext
        {
            get => (Game)GetValue(GameContextProperty);
            set => SetValue(GameContextProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="GameContext"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty GameContextProperty = DependencyProperty.Register(
            nameof(GameContext),
            typeof(Game),
            typeof(PaImageStripBase),
            new FrameworkPropertyMetadata(null, OnContextChanged));

        /// <summary>
        /// Gets the strip row height and decode pixel height for each thumbnail.
        /// </summary>
        protected abstract double ItemHeight { get; }

        /// <summary>
        /// Gets an optional fixed item width; <c>null</c> keeps width content-driven.
        /// </summary>
        protected abstract double? ItemWidth { get; }

        /// <summary>
        /// Gets the maximum number of images to show.
        /// </summary>
        protected abstract int MaxItems { get; }

        /// <summary>
        /// Gets the right margin between thumbnails.
        /// </summary>
        protected abstract double ItemRightMargin { get; }

        /// <summary>
        /// Gets the log prefix for strip refresh traces (e.g. <c>PaScreenshotStrip</c>).
        /// </summary>
        protected abstract string LogPrefix { get; }

        /// <summary>
        /// Builds the shared scrollable <see cref="ItemsControl"/> host.
        /// </summary>
        protected PaImageStripBase()
        {
            double height = ItemHeight;
            Focusable = false;
            Height = height;

            FrameworkElementFactory panelFactory = new FrameworkElementFactory(typeof(StackPanel));
            panelFactory.SetValue(StackPanel.OrientationProperty, Orientation.Horizontal);

            _itemsControl = new ItemsControl
            {
                Focusable = false,
                ItemsPanel = new ItemsPanelTemplate { VisualTree = panelFactory },
                ItemTemplate = new DataTemplate { VisualTree = CreateItemFactory() }
            };

            Content = new ScrollViewer
            {
                Focusable = false,
                Content = _itemsControl,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Height = height
            };

            Visibility = Visibility.Collapsed;
            Loaded += (s, e) => ScheduleRefresh();
            Unloaded += (s, e) => ReleaseImages();
        }

        /// <summary>
        /// Creates the visual tree factory for one strip item (default: bare <see cref="ImageAsync"/>).
        /// </summary>
        /// <returns>Factory used as the <see cref="DataTemplate"/> visual tree.</returns>
        protected virtual FrameworkElementFactory CreateItemFactory()
        {
            FrameworkElementFactory imageFactory = new FrameworkElementFactory(typeof(ImageAsync));
            double height = ItemHeight;
            imageFactory.SetValue(ImageAsync.DecodePixelHeightProperty, height);
            imageFactory.SetValue(FrameworkElement.HeightProperty, height);
            imageFactory.SetValue(Image.StretchProperty, Stretch.Uniform);
            imageFactory.SetValue(FrameworkElement.MarginProperty, new Thickness(0, 0, ItemRightMargin, 0));
            if (ItemWidth.HasValue)
            {
                imageFactory.SetValue(FrameworkElement.WidthProperty, ItemWidth.Value);
            }

            ConfigureItemImage(imageFactory);
            return imageFactory;
        }

        /// <summary>
        /// Binds image source and tooltip on the item <see cref="ImageAsync"/> factory.
        /// </summary>
        /// <param name="imageFactory">Factory for each strip item.</param>
        protected abstract void ConfigureItemImage(FrameworkElementFactory imageFactory);

        /// <summary>
        /// Returns the activity day used for filtering, or <see cref="DateTime.MinValue"/> when unset.
        /// </summary>
        protected abstract DateTime GetFilterDay();

        /// <summary>
        /// Loads display rows for <paramref name="gameId"/> on <paramref name="day"/> (background thread).
        /// </summary>
        /// <param name="gameId">Playnite game id.</param>
        /// <param name="day">Activity day.</param>
        /// <returns>Items to bind; empty when nothing to show.</returns>
        protected abstract IEnumerable LoadItems(Guid gameId, DateTime day);

        /// <summary>
        /// Queues a coalesced refresh on the UI dispatcher.
        /// </summary>
        protected void ScheduleRefresh()
        {
            if (_refreshScheduled)
            {
                return;
            }

            _refreshScheduled = true;
            _ = Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
            {
                _refreshScheduled = false;
                RefreshItemsCore();
            }));
        }

        private static void OnContextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            PaImageStripBase strip = d as PaImageStripBase;
            strip?.ScheduleRefresh();
        }

        private void ReleaseImages()
        {
            _refreshGeneration++;
            _appliedKey = string.Empty;
            _itemsControl.ItemsSource = null;
            Visibility = Visibility.Collapsed;
        }

        private void RefreshItemsCore()
        {
            Guid gameId = GameContext?.Id ?? Guid.Empty;
            DateTime day = GetFilterDay();
            if (gameId == Guid.Empty || day == default(DateTime))
            {
                ReleaseImages();
                return;
            }

            string key = gameId.ToString() + "|" + day.ToString("yyyy-MM-dd");
            if (string.Equals(key, _appliedKey, StringComparison.Ordinal))
            {
                return;
            }

            int generation = ++_refreshGeneration;

            _ = Task.Run(() =>
            {
                try
                {
                    IEnumerable items = LoadItems(gameId, day);
                    int count = CountItems(items);

                    _ = Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                    {
                        if (generation != _refreshGeneration)
                        {
                            return;
                        }

                        _appliedKey = key;
                        _itemsControl.ItemsSource = items;
                        Visibility = count == 0 ? Visibility.Collapsed : Visibility.Visible;
                        Common.LogDebug($"[{LogPrefix}] gameId={gameId} day={day:yyyy-MM-dd} count={count}");
                    }));
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false, $"[{LogPrefix}] loadFailed gameId={gameId}", false, "PlayerActivities");
                }
            });
        }

        private static int CountItems(IEnumerable items)
        {
            if (items == null)
            {
                return 0;
            }

            ICollection collection = items as ICollection;
            if (collection != null)
            {
                return collection.Count;
            }

            int count = 0;
            foreach (object unused in items)
            {
                count++;
            }

            return count;
        }
    }
}
