using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;

namespace IfFoundLockScreen
{
    public partial class Editor : PhoneApplicationPage
    {
        public Editor()
        {
            InitializeComponent();
            this.DataContext = this;
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            RewardModel vm = ((App)(Application.Current)).ViewModel as RewardModel;
            this.DataContext = vm;

            //Work around Silverlight Toolkit Bug for ToggleSwitch
            this.RoomForMediaSwitch.IsChecked = vm.MakeRoomforMedia;
        }

        private void ApplicationBarOKIconButton_Click(object sender, EventArgs e)
        {
            RewardModel vm = ((App)(Application.Current)).ViewModel as RewardModel;
            //Work around Silverlight Toolkit Bug for ToggleSwitch
            vm.MakeRoomforMedia = this.RoomForMediaSwitch.IsChecked.HasValue ? (bool)this.RoomForMediaSwitch.IsChecked : false;

            ((App)(Application.Current)).SaveData();
            this.NavigationService.GoBack();
        }
    }
}