using CommonPluginsShared;
using CommonPluginsShared.Controls;
using CommonPluginsShared.Extensions;
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
        internal static readonly ILogger logger = LogManager.GetLogger();
        internal static IResourceProvider resources = new ResourceProvider();

        private PlayerActivities Plugin;
        private PlayerActivitiesDatabase PluginDatabase = PlayerActivities.PluginDatabase;

        private PaViewData ControlDataContext = new PaViewData();

        private List<string> SearchSources = new List<string>();


        private bool TimeLineFilter(object item)
        {
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


        public PaView(PlayerActivities Plugin)
        {
            this.Plugin = Plugin;

            InitializeComponent();
            DataContext = ControlDataContext;

            PART_DataLoad.Visibility = Visibility.Visible;
            PART_Data.Visibility = Visibility.Hidden;


            GetData();
            GetFriends();


            PluginDatabase.Database.Select(x => PlayniteTools.GetSourceName(x.Game)).Distinct().ForEach(x =>
            {
                string icon = TransformIcon.Get(x) + " ";
                ControlDataContext.FilterSourceItems.Add(new ListSource { SourceName = ((icon.Length == 2) ? icon : string.Empty) + x, SourceNameShort = x, IsCheck = false });
            });
        }


        #region Data
        private void GetData()
        {
            Task.Run(() =>
            {
                ObservableCollection<ActivityList> activityLists = new ObservableCollection<ActivityList>();
                PluginDatabase.Database.ForEach(x =>
                {
                    x.Items.ForEach(y =>
                    {
                        activityLists.Add(new ActivityList
                        {
                            GameContext = x.Game,
                            DateActivity = y.DateActivity,
                            Type = y.Type,
                            Value = y.Value
                        });
                    });
                });


                // Options
                List<ActivityType> activityTypes = new List<ActivityType> { ActivityType.PlaytimeFirst, ActivityType.PlaytimeGoal };
                if (PluginDatabase.PluginSettings.Settings.EnableHowLongToBeatData)
                {
                    activityTypes.Add(ActivityType.HowLongToBeatCompleted);
                }
                if (PluginDatabase.PluginSettings.Settings.EnableScreenshotsVisualizerData)
                {
                    activityTypes.Add(ActivityType.ScreenshotsTaked);
                }
                if (PluginDatabase.PluginSettings.Settings.EnableSuccessStoryData)
                {
                    activityTypes.Add(ActivityType.AchievementsGoal);
                    activityTypes.Add(ActivityType.AchievementsUnlocked);
                }


                ObservableCollection<ActivityListGrouped> activityListsGrouped = new ObservableCollection<ActivityListGrouped>();
                Game GameContext = null;
                string TimeAgo = string.Empty;

                activityLists = activityLists.OrderByDescending(x => x.DateActivity).ToObservable();
                activityLists.Where(x => activityTypes.Any(y => y == x.Type)).ForEach(x =>
                {
                    if (ReferenceEquals(GameContext, x.GameContext) && TimeAgo.IsEqual(x.TimeAgo))
                    {
                        activityListsGrouped.Last().Activities.Add(new Activity
                        {
                            DateActivity = x.DateActivity,
                            Value = x.Value,
                            Type = x.Type
                        });
                    }
                    else
                    {
                        GameContext = x.GameContext;
                        TimeAgo = x.TimeAgo;
                        activityListsGrouped.Add(new ActivityListGrouped
                        {
                            GameContext = x.GameContext,
                            dtString = x.DateActivity.ToString("yyyy-MM-dd"),
                            TimeAgo = x.TimeAgo,
                            Activities = new List<Activity>
                            {
                                new Activity
                                {
                                    DateActivity = x.DateActivity,
                                    Value = x.Value,
                                    Type = x.Type
                                }
                            }
                        });
                    }
                });

                ControlDataContext.ItemsSource = activityListsGrouped;


                IsDataFinished = true;
                IsFinish();
            });
        }

        private void GetFriends()
        {
            Task.Run(() => 
            {
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
                this.Dispatcher?.BeginInvoke(DispatcherPriority.Loaded, new ThreadStart(delegate
                {
                    PART_DataLoad.Visibility = Visibility.Hidden;
                    PART_Data.Visibility = Visibility.Visible;
                }));

                Task.Run(() =>
                {
                    Thread.Sleep(5000);

                    this.Dispatcher?.BeginInvoke(DispatcherPriority.Loaded, new ThreadStart(delegate
                    {
                        CollectionView view = (CollectionView)CollectionViewSource.GetDefaultView(PART_LbTimeLine.ItemsSource);
                        view.Filter = TimeLineFilter;
                    }));
                });
            }
        }
        #endregion


        private void Hyperlink_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(((Hyperlink)sender).Tag.ToString());
            }
            catch(Exception ex)
            {
                Common.LogError(ex, false);
            }
        }


        #region Filter
        private void TextboxSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            CollectionViewSource.GetDefaultView(PART_LbTimeLine.ItemsSource).Refresh();
        }
        private void chkSource_Checked(object sender, RoutedEventArgs e)
        {
            FilterCbSource(sender as CheckBox);
        }

        private void chkSource_Unchecked(object sender, RoutedEventArgs e)
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
                FilterSource.Text = String.Join(", ", SearchSources);
            }

            CollectionViewSource.GetDefaultView(PART_LbTimeLine.ItemsSource).Refresh();
        }
        #endregion


        private void Button_RefreshFriendsData(object sender, RoutedEventArgs e)
        {
            GlobalProgressOptions globalProgressOptions = new GlobalProgressOptions(
            $"{PluginDatabase.PluginName} - {resources.GetString("LOCCommonProcessing")}",
            false
            );
            globalProgressOptions.IsIndeterminate = true;

            API.Instance.Dialogs.ActivateGlobalProgress((activateGlobalProgress) =>
            {
                PluginDatabase.GetFriends(Plugin, true);
                ControlDataContext.FriendsSource = PluginDatabase.GetFriends(Plugin).ToObservable();
                ControlDataContext.LastFriendsRefresh = PluginDatabase.PluginSettings.Settings.LastFriendsRefresh;
            }, globalProgressOptions);
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
        private ObservableCollection<ActivityListGrouped> _ItemsSource = new ObservableCollection<ActivityListGrouped>();
        public ObservableCollection<ActivityListGrouped> ItemsSource { get => _ItemsSource; set => SetValue(ref _ItemsSource, value); }

        private ObservableCollection<PlayerFriends> _FriendsSource = new ObservableCollection<PlayerFriends>();
        public ObservableCollection<PlayerFriends> FriendsSource { get => _FriendsSource; set => SetValue(ref _FriendsSource, value); }

        private ObservableCollection<ListFriendsInfo> _FriendsDetailsSource = new ObservableCollection<ListFriendsInfo>();
        public ObservableCollection<ListFriendsInfo> FriendsDetailsSource { get => _FriendsDetailsSource; set => SetValue(ref _FriendsDetailsSource, value); }

        private ObservableCollection<ListSource> _FilterSourceItems = new ObservableCollection<ListSource>();
        public ObservableCollection<ListSource> FilterSourceItems { get => _FilterSourceItems; set => SetValue(ref _FilterSourceItems, value); }

        private DateTime _LastFriendsRefresh = DateTime.Now;
        public DateTime LastFriendsRefresh { get => _LastFriendsRefresh; set => SetValue(ref _LastFriendsRefresh, value); }
    }

    public class ListSource : ObservableObject
    {
        public string SourceName { get; set; }
        public string SourceNameShort { get; set; }

        private bool _IsCheck;
        public bool IsCheck { get => _IsCheck; set => SetValue(ref _IsCheck, value); }
    }

    public class ListFriendsInfo : PlayerGames
    {
        public int UsAchievements { get; set; }
        public long UsPlaytime { get; set; }
    }
}
