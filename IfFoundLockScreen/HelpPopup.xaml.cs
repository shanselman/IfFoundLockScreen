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
using System.Windows.Controls.Primitives;

namespace IfFoundLockScreen
{
    public partial class HelpPopup : UserControl
    {
        public HelpPopup()
        {
            InitializeComponent();
            this.Loaded += new RoutedEventHandler(HelpPopup_Loaded);
        }

        void HelpPopup_Loaded(object sender, RoutedEventArgs e)
        {
            this.LayoutRoot.Background.Opacity = 0.8;            
        }
        
        private void DayOfWeek_Tap(object sender, GestureEventArgs e)
        {
            Popup p = this.Parent as Popup;
            if (p != null)
                p.IsOpen = false;
        }

        private void Lameo_Click(object sender, RoutedEventArgs e)
        {
            DayOfWeek_Tap(sender, null);
        }
    }
}
