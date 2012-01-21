using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.ComponentModel;
using System.Windows.Media.Imaging;
using System.Xml.Serialization;

namespace IfFoundLockScreen
{
    public class RewardModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public string Line1 { get; set; }
        public string Line2 { get; set; }
        public string Line3 { get; set; }
        public bool SeenHelpOnce { get; set; }

        public void Update()
        {
            //TODO: Get from XML file?
            Line1 = "Reward if found";
            Line2 = "email@yourdomain.com";
            Line3 = "+1.503.555.1212";
        }
    }
}
