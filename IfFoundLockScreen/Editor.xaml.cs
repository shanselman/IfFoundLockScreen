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
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
//            this.DataContext = App.Current.ViewModel;
            this.DataContext = ((App)(Application.Current)).ViewModel;
        }

        private void ApplicationBarOKIconButton_Click(object sender, EventArgs e)
        {
            this.NavigationService.GoBack();
        }
    }
}