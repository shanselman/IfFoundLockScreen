using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using Input = System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using System.ComponentModel;
using Microsoft.Phone.Tasks;
using System.IO.IsolatedStorage;
using System.Windows.Resources;
using System.Windows.Media.Imaging;
using System.IO;
using Microsoft.Xna.Framework.Media;
using System.Windows.Controls.Primitives;

namespace IfFoundLockScreen
{
    public partial class MainPage : PhoneApplicationPage
    {
        PhotoChooserTask photoChooserTask;

        // Constructor
        public MainPage()
        {
            InitializeComponent();

            UpdateDateTime();

            System.Windows.Threading.DispatcherTimer dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
            dispatcherTimer.Interval = new TimeSpan(0, 1, 0);
            dispatcherTimer.Tick += (sender, e) =>
            {
                UpdateDateTime();
            };
            dispatcherTimer.Start();

            photoChooserTask = new PhotoChooserTask();
            photoChooserTask.Completed += new EventHandler<PhotoResult>(photoChooserTask_Completed);

            this.Loaded += new RoutedEventHandler(MainPage_Loaded);
        }

        void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            LittleWatson.CheckForPreviousException();

            if (((App)App.Current).ViewModel.SeenHelpOnce == false)
            {
                ApplicationBarHelpIconButton_Click(this, null);
                ((App)App.Current).ViewModel.SeenHelpOnce = true;
            }

            BitmapImage bmp = ((App)App.Current).LoadCustomBackground();
            if (bmp != null)
            {
                this.CustomBackground.Source = bmp;
            }
            else //use the standard image
            {
                BitmapImage bi = new BitmapImage();
                bi.CreateOptions = BitmapCreateOptions.None;
                bi.UriSource = new Uri("Images\\initialwallpaper.jpg", UriKind.Relative);
                this.CustomBackground.Source = bi;
            }
        }


        void photoChooserTask_Completed(object sender, PhotoResult e)
        {
            if (e.TaskResult == TaskResult.OK)
            {
                System.Windows.Media.Imaging.BitmapImage bmp = new System.Windows.Media.Imaging.BitmapImage();
                bmp.SetSource(e.ChosenPhoto);
                this.CustomBackground.Source = bmp;

                ((App)App.Current).SaveCustomBackground(bmp);
            }
        }

        private void UpdateDateTime()
        {
            Time.Text = DateTime.Now.ToString("h:mm");
            DayOfWeek.Text = DateTime.Now.ToString("dddd");
            MonthDay.Text = DateTime.Now.ToString("MMMM dd");
        }

        private void ApplicationBarHelpIconButton_Click(object sender, EventArgs e)
        {
            this.ApplicationBar.IsVisible = false;
            var p = new Popup();
            p.Child = new HelpPopup();
            p.VerticalOffset = 200;
            p.HorizontalOffset = 0;
            p.IsOpen = true;
            p.Closed += (sender1, e1) =>
            {
                this.ApplicationBar.IsVisible = true;
            };
        }

        private void LockTextPanel_Tap(object sender, Input.GestureEventArgs e)
        {
            this.NavigationService.Navigate(new Uri("/Editor.xaml", UriKind.Relative));
        }

        private void ApplicationBarEditIconButton_Click(object sender, EventArgs e)
        {
            this.NavigationService.Navigate(new Uri("/Editor.xaml", UriKind.Relative));
        }

        private void ApplicationBarPhotoIconButton_Click(object sender, EventArgs e)
        {
            try
            {
                photoChooserTask.PixelHeight = (int)System.Windows.Application.Current.Host.Content.ActualHeight;
                photoChooserTask.PixelWidth = (int)System.Windows.Application.Current.Host.Content.ActualWidth;
                photoChooserTask.Show();
            }
            catch (System.InvalidOperationException ex)
            {
                MessageBox.Show("An error occurred.");
            }
        }

        private void ApplicationBarSaveIconButton_Click(object sender, EventArgs e)
        {
            //TODO: Popup an explanation about what will happen, and encourage them to go to the media library

            // Create a popup. 
            var p = new Popup();
            p.Child = new HelpSavePopup();
            p.VerticalOffset = 250;
            p.HorizontalOffset = 0;

            // Open the popup. 
            CollapseTime(true);

            p.IsOpen = true;
            p.Closed += (sender1, e1) =>
            {
                try
                {
                    var bitmap = new System.Windows.Media.Imaging.WriteableBitmap(this.LayoutRoot, null);
                    using (var stream = new System.IO.MemoryStream())
                    {
                        System.Windows.Media.Imaging.Extensions.SaveJpeg(bitmap, stream,
                                        bitmap.PixelWidth, bitmap.PixelHeight, 0, 100);
                        stream.Position = 0;
                        var mediaLib = new Microsoft.Xna.Framework.Media.MediaLibrary();
                        var datetime = System.DateTime.Now;
                        var filename =
                            System.String.Format("LockScreen-{0}-{1}-{2}-{3}-{4}",
                                datetime.Year % 100, datetime.Month, datetime.Day,
                                datetime.Hour, datetime.Minute);
                        mediaLib.SavePicture(filename, stream);
                    }

                }
                finally
                {
                    CollapseTime(false);
                }
            };
        }

        private void CollapseTime(bool p)
        {
            if (p)
            {
                this.DimBorder.Visibility = System.Windows.Visibility.Collapsed;
                this.ApplicationBar.IsVisible = false;
                this.TimePanel.Visibility = System.Windows.Visibility.Collapsed;
            }
            else
            {
                this.DimBorder.Visibility = System.Windows.Visibility.Visible;
                this.ApplicationBar.IsVisible = true;
                this.TimePanel.Visibility = System.Windows.Visibility.Visible;
            }
        }

        private void ApplicationBarAboutMenuItem_Click(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/YourLastAboutDialog;component/AboutPage.xaml", UriKind.Relative));
        }
    }
}