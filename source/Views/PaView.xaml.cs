using CommonPlayniteShared.Commands;
using CommonPluginsShared;
using CommonPluginsShared.Commands;
using CommonPluginsShared.Controls;
using CommonPluginsShared.Extensions;
using PlayerActivities.Controls;
using PlayerActivities.Models;
using PlayerActivities.Services;
using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Threading;

namespace PlayerActivities.Views
{
    /// <summary>
    /// Logique d'interaction pour PaView.xaml
    /// </summary>
    public partial class PaView : UserControl
    {
        private static ILogger Logger => LogManager.GetLogger();

        private PlayerActivities Plugin { get; }
        private PlayerActivitiesDatabase PluginDatabase => PlayerActivities.PluginDatabase;

        internal static PaViewData ControlDataContext { get; set; } = new PaViewData();

        /// <summary>
        /// Invokes an immediate timeline rebuild when PaView is loaded (e.g. context-menu refresh).
        /// </summary>
        internal static Action<string> RequestTimelineRefresh { get; private set; }

        private HashSet<string> SearchSources { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly DispatcherTimer _searchDebounceTimer;
        private readonly DispatcherTimer _dataRefreshDebounceTimer;
        private string _pendingDataRefreshReason;
        private const int SearchDebounceMs = 250;
        private const int DataRefreshDebounceMs = 300;

        private bool TimeLineFilter(object item)
        {
            ActivityListGrouped el = item as ActivityListGrouped;
            if (el?.GameContext == null)
            {
                return false;
            }

            string search = TextboxSearch?.Text ?? string.Empty;
            bool txtFilter = search.Length == 0
                || (el.GameContext.Name?.IndexOf(search, StringComparison.InvariantCultureIgnoreCase) ?? -1) >= 0;

            bool sourceFilter = SearchSources.Count == 0 || SearchSources.Contains(el.SourceName ?? string.Empty);

            return txtFilter && sourceFilter;
        }

        private bool IsDataFinished = false;
        private bool IsFriendsFinished = false;
        private bool _friendsLoadStarted = false;
        private bool _timelineReadyLogged = false;

        public PaView(PlayerActivities plugin)
        {
            Plugin = plugin;

            InitializeComponent();
            DataContext = ControlDataContext;

            _searchDebounceTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(SearchDebounceMs)
            };
            _searchDebounceTimer.Tick += SearchDebounceTimer_Tick;

            _dataRefreshDebounceTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(DataRefreshDebounceMs)
            };
            _dataRefreshDebounceTimer.Tick += DataRefreshDebounceTimer_Tick;

            ConfigureListViewColumnPersistence();

            PART_DataLoad.Visibility = Visibility.Visible;
            PART_Data.Visibility = Visibility.Hidden;
            PART_DataRerefsh.Visibility = Visibility.Collapsed;

            // Sidebar reuses the same PaView instance: subscribe on Loaded, unsubscribe on Unloaded
            // (ctor-only subscribe breaks after the first Unloaded — see PaViewSidebar Opened cache).
            Loaded += PaView_Loaded;
            Unloaded += PaView_Unloaded;

            PluginDatabase.GetAllCache().Select(x => PlayniteTools.GetSourceName(x.Game)).Distinct().ForEach(x =>
            {
                string icon = TransformIcon.Get(x) + " ";
                ControlDataContext.FilterSourceItems.Add(new ListSource { SourceName = ((icon.Length == 2) ? icon : string.Empty) + x, SourceNameShort = x, IsCheck = false });
            });


            if (!PluginDatabase.PluginSettings.EnableEpicFriends && !PluginDatabase.PluginSettings.EnableGogFriends && !PluginDatabase.PluginSettings.EnableOriginFriends && !PluginDatabase.PluginSettings.EnableSteamFriends)
            {
                PART_BtFriends.IsEnabled = false;
            }

            ControlDataContext.IsFriendsPanelExpanded = PluginDatabase.PluginSettings.IsFriendsPanelExpanded;
            ApplyFriendsPanelLayout();
        }

        private void ApplyFriendsPanelLayout()
        {
            if (PART_ColFeed == null || PART_ColFriendsGap == null || PART_ColFriends == null)
            {
                return;
            }

            bool expanded = ControlDataContext.IsFriendsPanelExpanded;
            if (expanded)
            {
                PART_ColFeed.Width = new GridLength(1.5, GridUnitType.Star);
                PART_ColFriendsGap.Width = new GridLength(20);
                PART_ColFriends.Width = new GridLength(1, GridUnitType.Star);
            }
            else
            {
                // Keep a slim strip so the expand chevron stays reachable next to the friends panel.
                PART_ColFeed.Width = new GridLength(1, GridUnitType.Star);
                PART_ColFriendsGap.Width = new GridLength(6);
                PART_ColFriends.Width = new GridLength(36);
            }

            Visibility bodyVisibility = expanded ? Visibility.Visible : Visibility.Collapsed;
            if (PART_FriendsHeaderTitle != null)
            {
                PART_FriendsHeaderTitle.Visibility = bodyVisibility;
            }

            if (PART_GridFriendsContener != null)
            {
                PART_GridFriendsContener.Visibility = bodyVisibility;
            }

            if (PART_GridFriendsDetailsContener != null)
            {
                PART_GridFriendsDetailsContener.Visibility = bodyVisibility;
            }

            if (PART_FriendsFooter != null)
            {
                PART_FriendsFooter.Visibility = bodyVisibility;
            }
        }

        private void Button_ToggleFriendsPanel_Click(object sender, RoutedEventArgs e)
        {
            bool expanded = !ControlDataContext.IsFriendsPanelExpanded;
            ControlDataContext.IsFriendsPanelExpanded = expanded;
            PluginDatabase.PluginSettings.IsFriendsPanelExpanded = expanded;
            Plugin.SavePluginSettings(PluginDatabase.PluginSettings);
            ApplyFriendsPanelLayout();
            Common.LogDebug($"[PaView] Friends panel expanded={expanded}");
        }

        private void ConfigureListViewColumnPersistence()
        {
            try
            {
                string columnsPath = Path.Combine(PluginDatabase.Paths.PluginUserDataPath, "ListViewColumns.json");
                bool saveOrder = PluginDatabase.PluginSettings.SaveColumnOrder;
                Common.LogDebug($"[PaView] Column persistence enabled={saveOrder}, path={columnsPath}");

                PART_LvFriends.EnableColumnPersistence = saveOrder;
                PART_LvFriends.ColumnConfigurationFilePath = columnsPath;
                PART_LvFriends.ColumnConfigurationScope = ColumnConfigurationScope.Custom;
                PART_LvFriends.ColumnConfigurationKey = "PlayerActivities.PaView.Friends";

                PART_LvFriendsDetails.EnableColumnPersistence = saveOrder;
                PART_LvFriendsDetails.ColumnConfigurationFilePath = columnsPath;
                PART_LvFriendsDetails.ColumnConfigurationScope = ColumnConfigurationScope.Custom;
                PART_LvFriendsDetails.ColumnConfigurationKey = "PlayerActivities.PaView.FriendsDetails";
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, false, PluginDatabase.PluginName);
            }
        }

        private void PaView_Loaded(object sender, RoutedEventArgs e)
        {
            PluginDatabase.DatabaseItemUpdated -= Database_ItemUpdated;
            PluginDatabase.DatabaseItemUpdated += Database_ItemUpdated;
            PluginDatabase.DatabaseItemCollectionChanged -= Database_ItemCollectionChanged;
            PluginDatabase.DatabaseItemCollectionChanged += Database_ItemCollectionChanged;
            RequestTimelineRefresh = GetData;

            Common.LogDebug("[PaView] Loaded — subscribed to database events");

            GetData("Loaded");
            if (!_friendsLoadStarted)
            {
                _friendsLoadStarted = true;
                GetFriends();
            }
        }

        private void PaView_Unloaded(object sender, RoutedEventArgs e)
        {
            _searchDebounceTimer.Stop();
            _dataRefreshDebounceTimer.Stop();
            _pendingDataRefreshReason = null;
            RequestTimelineRefresh = null;
            PluginDatabase.DatabaseItemUpdated -= Database_ItemUpdated;
            PluginDatabase.DatabaseItemCollectionChanged -= Database_ItemCollectionChanged;
            Common.LogDebug("[PaView] Unloaded — unsubscribed from database events");
        }

        private void Database_ItemUpdated(object sender, ItemUpdatedEventArgs<PlayerActivitiesData> e)
        {
            int updatedCount = e?.UpdatedItems?.Count ?? 0;
            ScheduleDataRefresh($"ItemUpdated count={updatedCount}");
        }

        private void Database_ItemCollectionChanged(object sender, ItemCollectionChangedEventArgs<PlayerActivitiesData> e)
        {
            ScheduleDataRefresh("CollectionChanged");
        }

        private void ScheduleDataRefresh(string reason)
        {
            _pendingDataRefreshReason = reason;
            _dataRefreshDebounceTimer.Stop();
            _dataRefreshDebounceTimer.Start();
            Common.LogDebug($"[PaView] ScheduleDataRefresh reason={reason} debounceMs={DataRefreshDebounceMs}");
        }

        private void DataRefreshDebounceTimer_Tick(object sender, EventArgs e)
        {
            _dataRefreshDebounceTimer.Stop();
            string reason = _pendingDataRefreshReason ?? "Debounced";
            _pendingDataRefreshReason = null;
            GetData(reason);
        }

        #region Data

        private void GetData(string reason = "GetData")
        {
            _ = Task.Run(() =>
            {
                try
                {
                    Stopwatch swAgg = Stopwatch.StartNew();
                    ObservableCollection<ActivityListGrouped> data = PluginDatabase.GetActivitiesData();
                    swAgg.Stop();
                    int count = data?.Count ?? 0;
                    Common.LogDebug($"[PaView] GetActivitiesData reason={reason} count={count} elapsedMs={swAgg.ElapsedMilliseconds}");

                    _ = Dispatcher?.BeginInvoke(DispatcherPriority.Loaded, new ThreadStart(delegate
                    {
                        try
                        {
                            Stopwatch swUi = Stopwatch.StartNew();
                            ControlDataContext.ItemsSource = data;

                            if (PART_LbTimeLine?.ItemsSource != null)
                            {
                                CollectionView view = (CollectionView)CollectionViewSource.GetDefaultView(PART_LbTimeLine.ItemsSource);
                                if (view != null)
                                {
                                    view.Filter = TimeLineFilter;
                                }
                            }

                            swUi.Stop();
                            Common.LogDebug($"[PaView] apply timeline UI reason={reason} count={count} elapsedMs={swUi.ElapsedMilliseconds}");

                            IsDataFinished = true;
                            IsFinish();
                        }
                        catch (Exception ex)
                        {
                            Common.LogError(ex, false, "[PaView] GetData UI update failed", false, PluginDatabase.PluginName);
                        }
                    }));
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false, "[PaView] GetData refresh failed", false, PluginDatabase.PluginName);
                }
            });
        }

        private void GetFriends()
        {
            _ = Task.Run(() =>
            {
                _ = SpinWait.SpinUntil(() => PluginDatabase.FriendsDataIsDownloaded, -1);
                FriendsData friendsData = PluginDatabase.GetFriends(Plugin);
                ControlDataContext.FriendsSource = friendsData.PlayerFriends.ToObservable();
                ControlDataContext.LastFriendsRefresh = friendsData.PlayerFriends.Count == 0 ? (DateTime?)null : friendsData.LastUpdate;
                IsFriendsFinished = true;
                IsFinish();
            });
        }

        private void IsFinish()
        {
            if (IsDataFinished && IsFriendsFinished)
            {
                _ = Dispatcher?.BeginInvoke(DispatcherPriority.Loaded, new ThreadStart(delegate
                {
                    PART_DataLoad.Visibility = Visibility.Hidden;
                    PART_Data.Visibility = Visibility.Visible;
                    if (!_timelineReadyLogged)
                    {
                        _timelineReadyLogged = true;
                        int groupCount = ControlDataContext.ItemsSource?.Count ?? 0;
                        Logger.Info($"[PaView] Timeline and friends UI ready — groups={groupCount}");
                    }
                }));

                _ = Task.Run(() =>
                {
                    Thread.Sleep(3000);

                    _ = (Dispatcher?.BeginInvoke(DispatcherPriority.Loaded, new ThreadStart(delegate
                    {
                        try
                        {
                            if (PART_LbTimeLine?.ItemsSource == null)
                            {
                                return;
                            }

                            CollectionView view = (CollectionView)CollectionViewSource.GetDefaultView(PART_LbTimeLine.ItemsSource);
                            if (view != null)
                            {
                                view.Filter = TimeLineFilter;
                            }
                        }
                        catch (Exception ex)
                        {
                            Common.LogError(ex, false, "[PaView] IsFinish filter reapply failed", false, PluginDatabase.PluginName);
                        }
                    })));
                });
            }
        }

        #endregion

        #region Filter

        private void TextboxSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            _searchDebounceTimer.Stop();
            _searchDebounceTimer.Start();
        }

        private void SearchDebounceTimer_Tick(object sender, EventArgs e)
        {
            _searchDebounceTimer.Stop();
            RefreshTimeLineFilter("TextSearch");
        }

        private void ChkSource_Checked(object sender, RoutedEventArgs e)
        {
            FilterCbSource(sender as CheckBox);
        }

        private void ChkSource_Unchecked(object sender, RoutedEventArgs e)
        {
            FilterCbSource(sender as CheckBox);
        }

        private void FilterCbSource(CheckBox sender)
        {
            if (sender?.Tag == null)
            {
                return;
            }

            string source = sender.Tag.ToString();
            if ((bool)sender.IsChecked)
            {
                SearchSources.Add(source);
            }
            else
            {
                SearchSources.Remove(source);
            }

            FilterSource.Text = SearchSources.Count == 0
                ? string.Empty
                : string.Join(", ", SearchSources);

            RefreshTimeLineFilter("SourceFilter");
        }

        private void RefreshTimeLineFilter(string reason)
        {
            if (PART_LbTimeLine?.ItemsSource == null)
            {
                return;
            }

            System.ComponentModel.ICollectionView view = CollectionViewSource.GetDefaultView(PART_LbTimeLine.ItemsSource);
            if (view == null)
            {
                return;
            }

            Stopwatch sw = Stopwatch.StartNew();
            view.Refresh();
            sw.Stop();
            Common.LogDebug($"[PaView] CollectionView.Refresh reason={reason} searchLen={TextboxSearch?.Text?.Length ?? 0} sources={SearchSources.Count} elapsedMs={sw.ElapsedMilliseconds}");
        }

        #endregion

        private void Button_RefreshFriendsData(object sender, RoutedEventArgs e)
        {
            PART_BtFriends.IsEnabled = false;
            PART_DataRerefsh.Visibility = Visibility.Visible;
            ControlDataContext.FriendsDataLoading = PluginDatabase.FriendsDataLoading;

            _ = Task.Run(() => {
                PluginDatabase.RefreshFriends(Plugin);
                GetFriends();

                _ = (Dispatcher?.BeginInvoke(DispatcherPriority.Loaded, new ThreadStart(delegate
                {
                    PART_BtFriends.IsEnabled = true;
                    PART_DataRerefsh.Visibility = Visibility.Collapsed;
                })));
            });
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                if (button.Tag is PlayerFriend item)
                {
                    button.IsEnabled = false;
                    PART_BtFriends.IsEnabled = false;
                    _ = Task.Run(() => {
                        PluginDatabase.RefreshFriends(Plugin, item.ClientName, item);
                        GetFriends();

                        _ = (Dispatcher?.BeginInvoke(DispatcherPriority.Loaded, new ThreadStart(delegate
                        {
                            button.IsEnabled = true;
                            PART_BtFriends.IsEnabled = true;
                        })));
                    });
                }
            }
        }

        private void ListViewExtend_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender == null)
            {
                return;
            }

            ListViewExtend lv = sender as ListViewExtend;
            if (lv == null || !(lv.SelectedItem is PlayerFriend pf))
            {
                return;
            }

            // Me-row for the same store may be missing (data incomplete / filter) — compare playtime as 0.
            PlayerFriend pf_us = ControlDataContext.FriendsSource?
                .FirstOrDefault(x => x.IsUser && x.ClientName.IsEqual(pf.ClientName));

            ObservableCollection<ListFriendsInfo> listFriendsInfos = new ObservableCollection<ListFriendsInfo>();
            if (pf.Games != null)
            {
                pf.Games.ForEach(x =>
                {
                    if (x == null)
                    {
                        return;
                    }

                    listFriendsInfos.Add(new ListFriendsInfo
                    {
                        Id = x.Id,
                        Name = x.Name,
                        Achievements = x.Achievements,
                        IsCommun = pf.IsUser ? true : x.IsCommun,
                        Link = x.Link,
                        Playtime = x.Playtime,
                        UsAchievements = pf_us?.Games?.Find(y => x.Id == y.Id)?.Achievements ?? 0,
                        UsPlaytime = pf_us?.Games?.Find(y => x.Id == y.Id)?.Playtime ?? 0
                    });
                });
            }

            ControlDataContext.FriendsDetailsSource = listFriendsInfos;
            PART_LvFriendsDetails?.Sorting();
        }
    }


    public class PaViewData : ObservableObject
    {
        private static PlayerActivitiesDatabase PluginDatabase => PlayerActivities.PluginDatabase;

        private ObservableCollection<ActivityListGrouped> _itemsSource = new ObservableCollection<ActivityListGrouped>();
        public ObservableCollection<ActivityListGrouped> ItemsSource { get => _itemsSource; set => SetValue(ref _itemsSource, value); }

        private ObservableCollection<PlayerFriend> _friendsSource = new ObservableCollection<PlayerFriend>();
        public ObservableCollection<PlayerFriend> FriendsSource { get => _friendsSource; set => SetValue(ref _friendsSource, value); }

        private ObservableCollection<ListFriendsInfo> _friendsDetailsSource = new ObservableCollection<ListFriendsInfo>();
        public ObservableCollection<ListFriendsInfo> FriendsDetailsSource { get => _friendsDetailsSource; set => SetValue(ref _friendsDetailsSource, value); }

        private ObservableCollection<ListSource> _filterSourceItems = new ObservableCollection<ListSource>();
        public ObservableCollection<ListSource> FilterSourceItems { get => _filterSourceItems; set => SetValue(ref _filterSourceItems, value); }

        private FriendsDataLoading _friendsDataLoading = new FriendsDataLoading();
        public FriendsDataLoading FriendsDataLoading { get => _friendsDataLoading; set => SetValue(ref _friendsDataLoading, value); }

        private DateTime? _lastFriendsRefresh;
        public DateTime? LastFriendsRefresh { get => _lastFriendsRefresh; set => SetValue(ref _lastFriendsRefresh, value); }

        private bool _isFriendsPanelExpanded = true;
        /// <summary>
        /// Whether the friends column is visible in PaView (bound by the toggle button style).
        /// </summary>
        public bool IsFriendsPanelExpanded { get => _isFriendsPanelExpanded; set => SetValue(ref _isFriendsPanelExpanded, value); }


        #region Menus

        public RelayCommand<Game> StartGameCommand { get; } = new RelayCommand<Game>((game) 
            => API.Instance.StartGame(game.Id));

        public RelayCommand<Game> InstallGameCommand { get; } = new RelayCommand<Game>((game) 
            => API.Instance.InstallGame(game.Id));

        public RelayCommand<Game> ShowGameInLibraryCommand { get; } = new RelayCommand<Game>((game)
            => CommandsNavigation.GoToGame.Execute(game.Id));

        public RelayCommand<Game> RefreshGameDataCommand { get; } = new RelayCommand<Game>((game) =>
        {
            if (game == null)
            {
                return;
            }

            _ = Task.Run(() =>
            {
                try
                {
                    PluginDatabase.InitializePluginData(true, game.Id);
                    Action<string> refresh = PaView.RequestTimelineRefresh;
                    if (refresh != null)
                    {
                        Application.Current?.Dispatcher?.BeginInvoke(DispatcherPriority.Normal, new Action(() => refresh("RefreshGameData")));
                    }
                    else
                    {
                        ObservableCollection<ActivityListGrouped> data = PluginDatabase.GetActivitiesData();
                        Application.Current?.Dispatcher?.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
                        {
                            PaView.ControlDataContext.ItemsSource = data;
                        }));
                    }
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false, "[PaView] RefreshGameDataCommand failed", false, PluginDatabase.PluginName);
                }
            });
        });

        public RelayCommand<Game> ShowGameSuccessStoryCommand { get; } = new RelayCommand<Game>((game) 
            => SuccessStoryPlugin.SuccessStoryView(game));

        public RelayCommand<Game> ShowGameHowLongToBeatCommand { get; } = new RelayCommand<Game>((game) 
            => HowLongToBeatPlugin.HowLongToBeatView(game));

        public RelayCommand<Game> ShowGameScreenshotsVisualizerCommand { get; } = new RelayCommand<Game>((game) 
            => ScreenshotsVisualizerPlugin.ScreenshotsVisualizerView(game));
        
        #endregion
    }


    public class ListSource : ObservableObject
    {
        public string SourceName { get; set; }
        public string SourceNameShort { get; set; }

        private bool isCheck;
        public bool IsCheck { get => isCheck; set => SetValue(ref isCheck, value); }
    }


    public class ListFriendsInfo : PlayerGame
    {
        public int UsAchievements { get; set; }
        public long UsPlaytime { get; set; }

        public RelayCommand<object> NavigateUrl { get; } = new RelayCommand<object>((url) => GlobalCommands.NavigateUrl(url));
    }
}