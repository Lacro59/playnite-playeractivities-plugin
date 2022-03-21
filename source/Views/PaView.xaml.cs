using CommonPluginsShared;
using PlayerActivities.Clients;
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

        private PlayerActivitiesDatabase PluginDatabase = PlayerActivities.PluginDatabase;

        private PaViewData ControlDataContext = new PaViewData();


        private bool IsDataFinished = false;
        private bool IsFriendsFinished = false;


        public PaView()
        {
            InitializeComponent();
            DataContext = ControlDataContext;

            PART_DataLoad.Visibility = Visibility.Visible;
            PART_Data.Visibility = Visibility.Hidden;

            GetData();
            GetFriends();
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
                activityLists = activityLists.OrderByDescending(x => x.DateActivity).ToObservable();
                activityLists.Where(x => activityTypes.Any(y => y == x.Type)).ForEach(x =>
                {
                    var finded = activityListsGrouped.Where(z => z.dtString == x.DateActivity.ToString("yyyy-MM-dd") && z.GameContext == x.GameContext)?.FirstOrDefault();
                    if (finded != null)
                    {
                        finded.Activities.Add(new Activity
                        {
                            DateActivity = x.DateActivity,
                            Value = x.Value,
                            Type = x.Type
                        });
                    }
                    else
                    {
                        activityListsGrouped.Add(new ActivityListGrouped
                        {
                            GameContext = x.GameContext,
                            dtString = x.DateActivity.ToString("yyyy-MM-dd"),
                            Activities = new List<Activity> {
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
                List<PlayerFriends> playerFriends = new List<PlayerFriends>();

                GogFriends gogFriends = new GogFriends();
                List<PlayerFriends> gogs = gogFriends.GetFriends();

                SteamFriends steamFriends = new SteamFriends();
                List<PlayerFriends> steams = steamFriends.GetFriends();


                playerFriends = playerFriends.Concat(gogs).Concat(steams).ToList();
                ControlDataContext.FriendsSource = playerFriends.ToObservable();


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
    }

    public class PaViewData : ObservableObject
    {
        private ObservableCollection<ActivityListGrouped> _ItemsSource;
        public ObservableCollection<ActivityListGrouped> ItemsSource { get => _ItemsSource; set => SetValue(ref _ItemsSource, value); }

        private ObservableCollection<PlayerFriends> _FriendsSource;
        public ObservableCollection<PlayerFriends> FriendsSource { get => _FriendsSource; set => SetValue(ref _FriendsSource, value); }
    }
}
