using PlayerActivities.Models;
using PlayerActivities.Services;
using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

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


        public PaView()
        {
            InitializeComponent();
            DataContext = ControlDataContext;

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
        }
    }

    public class PaViewData : ObservableObject
    {
        private ObservableCollection<ActivityListGrouped> _ItemsSource;
        public ObservableCollection<ActivityListGrouped> ItemsSource { get => _ItemsSource; set => SetValue(ref _ItemsSource, value); }
    }
}
