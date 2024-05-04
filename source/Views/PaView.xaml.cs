using CommonPlayniteShared.Commands;
using CommonPluginsShared;
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
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Threading;

namespace PlayerActivities.Views
{
    /// <summary>
    /// Logique d'interaction pour PaView.xaml
    /// </summary>
    public partial class PaView : UserControl
    {
        internal static ILogger Logger => LogManager.GetLogger();

        private PlayerActivities Plugin { get; }
        private PlayerActivitiesDatabase PluginDatabase => PlayerActivities.PluginDatabase;

        internal static PaViewData ControlDataContext { get; set; } = new PaViewData();

        private List<string> SearchSources { get; set; } = new List<string>();


        private bool TimeLineFilter(object item)
        {
            if (item == null)
            {
                return false;
            }

            ActivityListGrouped el = item as ActivityListGrouped;

            bool txtFilter = el.GameContext.Name.Contains(TextboxSearch.Text, StringComparison.InvariantCultureIgnoreCase);

            bool sourceFilter = true;
            if (SearchSources.Count > 0)
            {
                sourceFilter = SearchSources.Where(x => PlayniteTools.GetSourceName(el.GameContext).IsEqual(x)).Count() > 0;
            }

            return txtFilter && sourceFilter;
        }


        private bool IsDataFinished = false;
        private bool IsFriendsFinished = false;


        public PaView(PlayerActivities plugin)
        {
            Plugin = plugin;

            InitializeComponent();
            DataContext = ControlDataContext;

            PART_DataLoad.Visibility = Visibility.Visible;
            PART_Data.Visibility = Visibility.Hidden;


            GetData();
            GetFriends();


            PluginDatabase.Database.ItemUpdated += Database_ItemUpdated;

            PluginDatabase.Database.Select(x => PlayniteTools.GetSourceName(x.Game)).Distinct().ForEach(x =>
            {
                string icon = TransformIcon.Get(x) + " ";
                ControlDataContext.FilterSourceItems.Add(new ListSource { SourceName = ((icon.Length == 2) ? icon : string.Empty) + x, SourceNameShort = x, IsCheck = false });
            });


            if (!PluginDatabase.PluginSettings.Settings.EnableGogFriends && !PluginDatabase.PluginSettings.Settings.EnableOriginFriends && !PluginDatabase.PluginSettings.Settings.EnableSteamFriends)
            {
                PART_BtFriends.IsEnabled = false;
            }
        }

        private void Database_ItemUpdated(object sender, ItemUpdatedEventArgs<PlayerActivitiesData> e)
        {
            GetData();
        }


        #region Data
        private void GetData()
        {
            _ = Task.Run(() =>
            {
                ControlDataContext.ItemsSource = PluginDatabase.GetActivitiesData();

                IsDataFinished = true;
                IsFinish();
            });
        }

        private void GetFriends()
        {
            _ = Task.Run(() =>
            {
                _ = SpinWait.SpinUntil(() => PluginDatabase.FriendsDataIsDownloaded, -1);
                ControlDataContext.FriendsSource = PluginDatabase.GetFriends(Plugin).ToObservable();
                ControlDataContext.LastFriendsRefresh = PluginDatabase.PluginSettings.Settings.LastFriendsRefresh;
                IsFriendsFinished = true;
                IsFinish();
            });
        }

        private void IsFinish()
        {
            if (IsDataFinished && IsFriendsFinished)
            {
                _ = this.Dispatcher?.BeginInvoke(DispatcherPriority.Loaded, new ThreadStart(delegate
                {
                    PART_DataLoad.Visibility = Visibility.Hidden;
                    PART_Data.Visibility = Visibility.Visible;
                }));

                _ = Task.Run(() =>
                {
                    Thread.Sleep(3000);

                    this.Dispatcher?.BeginInvoke(DispatcherPriority.Loaded, new ThreadStart(delegate
                    {
                        CollectionView view = (CollectionView)CollectionViewSource.GetDefaultView(PART_LbTimeLine.ItemsSource);
                        view.Filter = TimeLineFilter;
                    }));
                });
            }
        }
        #endregion


        #region Filter
        private void TextboxSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            CollectionViewSource.GetDefaultView(PART_LbTimeLine.ItemsSource).Refresh();
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
            FilterSource.Text = string.Empty;

            if ((bool)sender.IsChecked)
            {
                SearchSources.Add((string)sender.Tag);
            }
            else
            {
                SearchSources.Remove((string)sender.Tag);
            }

            if (SearchSources.Count != 0)
            {
                FilterSource.Text = string.Join(", ", SearchSources);
            }

            CollectionViewSource.GetDefaultView(PART_LbTimeLine.ItemsSource).Refresh();
        }
        #endregion


        private void Button_RefreshFriendsData(object sender, RoutedEventArgs e)
        {
            _ = Task.Run(() => {
                PluginDatabase.RefreshFriendsDataLoader(Plugin).GetAwaiter().GetResult();
                GetFriends();
            });
        }


        private void ListViewExtend_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender == null)
            {
                return;
            }

            ListViewExtend lv = sender as ListViewExtend;
            PlayerFriends pf = lv.SelectedItem as PlayerFriends;

            if (pf == null)
            {
                return;
            }

            PlayerFriends pf_us = ControlDataContext.FriendsSource.Where(x => x.IsUser && x.ClientName.IsEqual(pf.ClientName))?.First() ?? null;

            ObservableCollection<ListFriendsInfo> listFriendsInfos = new ObservableCollection<ListFriendsInfo>();
            pf.Games.ForEach(x =>
            {
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

            ControlDataContext.FriendsDetailsSource = listFriendsInfos;
            PART_LvFriendsDetails.Sorting();
        }
    }

    public class PaViewData : ObservableObject
    {
        private static PlayerActivitiesDatabase PluginDatabase => PlayerActivities.PluginDatabase;

        private ObservableCollection<ActivityListGrouped> itemsSource = new ObservableCollection<ActivityListGrouped>();
        public ObservableCollection<ActivityListGrouped> ItemsSource { get => itemsSource; set => SetValue(ref itemsSource, value); }

        private ObservableCollection<PlayerFriends> friendsSource = new ObservableCollection<PlayerFriends>();
        public ObservableCollection<PlayerFriends> FriendsSource { get => friendsSource; set => SetValue(ref friendsSource, value); }

        private ObservableCollection<ListFriendsInfo> friendsDetailsSource = new ObservableCollection<ListFriendsInfo>();
        public ObservableCollection<ListFriendsInfo> FriendsDetailsSource { get => friendsDetailsSource; set => SetValue(ref friendsDetailsSource, value); }

        private ObservableCollection<ListSource> filterSourceItems = new ObservableCollection<ListSource>();
        public ObservableCollection<ListSource> FilterSourceItems { get => filterSourceItems; set => SetValue(ref filterSourceItems, value); }

        private DateTime lastFriendsRefresh = DateTime.Now;
        public DateTime LastFriendsRefresh { get => lastFriendsRefresh; set => SetValue(ref lastFriendsRefresh, value); }


        #region Menus
        public RelayCommand<Game> StartGameCommand { get; } = new RelayCommand<Game>((game) =>
        {
            if (game == null)
            {
                return;
            }

            API.Instance.StartGame(game.Id);
        });

        public RelayCommand<Game> InstallGameCommand { get; } = new RelayCommand<Game>((game) =>
        {
            if (game == null)
            {
                return;
            }

            API.Instance.InstallGame(game.Id);
        });

        public RelayCommand<Game> ShowGameInLibraryCommand { get; } = new RelayCommand<Game>((game) =>
        {
            if (game == null)
            {
                return;
            }

            API.Instance.MainView.SelectGame(game.Id);
            API.Instance.MainView.SwitchToLibraryView();
        });

        public RelayCommand<Game> RefreshGameDataCommand { get; } = new RelayCommand<Game>((game) =>
        {
            if (game == null)
            {
                return;
            }

            PluginDatabase.InitializePluginData(true, game.Id);
            PaView.ControlDataContext.ItemsSource = PluginDatabase.GetActivitiesData();
        });

        public RelayCommand<Game> ShowGameSuccessStoryCommand { get; } = new RelayCommand<Game>((game) =>
        {
            if (game == null)
            {
                return;
            }

            SuccessStoryPlugin.SuccessStoryView(game);
        });

        public RelayCommand<Game> ShowGameHowLongToBeatCommand { get; } = new RelayCommand<Game>((game) =>
        {
            if (game == null)
            {
                return;
            }

            HowLongToBeatPlugin.HowLongToBeatView(game);
        });

        public RelayCommand<Game> ShowGameScreenshotsVisualizerCommand { get; } = new RelayCommand<Game>((game) =>
        {
            if (game == null)
            {
                return;
            }

            ScreenshotsVisualizerPlugin.ScreenshotsVisualizerView(game);
        });
        #endregion
    }

    public class ListSource : ObservableObject
    {
        public string SourceName { get; set; }
        public string SourceNameShort { get; set; }

        private bool isCheck;
        public bool IsCheck { get => isCheck; set => SetValue(ref isCheck, value); }
    }

    public class ListFriendsInfo : PlayerGames
    {
        public int UsAchievements { get; set; }
        public long UsPlaytime { get; set; }

        public RelayCommand<object> NavigateUrl { get; } = new RelayCommand<object>((url) => GlobalCommands.NavigateUrl(url));
    }
}
