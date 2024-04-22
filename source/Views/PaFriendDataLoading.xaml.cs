using PlayerActivities.Models;
using PlayerActivities.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace PlayerActivities.Views
{
    /// <summary>
    /// Logique d'interaction pour PaFriendDataLoading.xaml
    /// </summary>
    public partial class PaFriendDataLoading : UserControl
    {
        private static PlayerActivitiesDatabase PluginDatabase = PlayerActivities.PluginDatabase;
        private FriendsDataLoading ControlDataContext = PluginDatabase.FriendsDataLoading;


        public PaFriendDataLoading()
        {
            InitializeComponent();
            DataContext = ControlDataContext;
        }

        private void PART_BtCancel_Click(object sender, RoutedEventArgs e)
        {
            PluginDatabase.FriendsDataIsCanceled = true;
            PART_BtCancel.IsEnabled = false;
        }
    }
}
