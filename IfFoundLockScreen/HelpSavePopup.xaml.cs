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
        }

        private void HelpSave_Tap(object sender, GestureEventArgs e)
        {
            this.Visibility = System.Windows.Visibility.Collapsed;
            Popup p = this.Parent as Popup;
            if (p != null)
                p.IsOpen = false;
        }
    }
}
