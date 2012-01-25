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
    public partial class HelpSavePopup : UserControl
    {
        public HelpSavePopup()
        {
            InitializeComponent();
            this.Loaded += new RoutedEventHandler(HelpSavePopup_Loaded);
        }

        void HelpSavePopup_Loaded(object sender, RoutedEventArgs e)
        {
            this.LayoutRoot.Background.Opacity = 0.8;
        }

        private void HelpSave_Tap(object sender, GestureEventArgs e)
        {
            Popup p = this.Parent as Popup;
            if (p != null)
            {
                p.IsOpen = false;
            }
        }

        private void Lameo_Click(object sender, RoutedEventArgs e)
        {
            HelpSave_Tap(sender, null);
        }
    }
}
