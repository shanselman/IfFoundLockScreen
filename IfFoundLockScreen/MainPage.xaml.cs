using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using Windows.Phone.System.UserProfile;
using Windows.Storage;
using Windows.Storage.Streams;
using Input = System.Windows.Input;
using Microsoft.Phone.Controls;
using System.ComponentModel;
using Microsoft.Phone.Tasks;
using System.Windows.Media.Imaging;
using System.IO;
using System.Windows.Controls.Primitives;
using Microsoft.Phone.Shell;
using Coding4Fun.Phone.Controls;
using System.Windows.Data;
using System.Globalization;
using QEDCode;

namespace IfFoundLockScreen
{
    public partial class MainPage : PhoneApplicationPage
    {
        PhotoChooserTask photoChooserTask;
        AwaitableCriticalSection acs = new AwaitableCriticalSection();

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

            if (ViewModel.SeenHelpOnce == false)
            {
                ApplicationBarHelpIconButton_Click(this, null);
                ViewModel.SeenHelpOnce = true;
                ((App)App.Current).SaveData();
            }

            UpdateInvertStatus();

            BitmapImage bmp = ((App)App.Current).LoadCustomBackground();
            if (bmp != null)
            {
                this.CustomBackground.Source = bmp;
            }
            else //use the standard image
            {
                BitmapImage bi = new BitmapImage();
                bi.CreateOptions = BitmapCreateOptions.BackgroundCreation; //TODO: confirm this is cool
                bi.UriSource = new Uri("Images\\initialwallpaper.jpg", UriKind.Relative);
                this.CustomBackground.Source = bi;
            }
        }

        public RewardModel ViewModel
        {
            get { return ((App)App.Current).ViewModel as RewardModel; }
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            //MessageBox.Show("NavMode:" + e.NavigationMode.ToString());
            //MessageBox.Show("Initiator:" + e.IsNavigationInitiator);
            
            // Get a dictionary of query string keys and values.
            IDictionary<string, string> queryStrings = this.NavigationContext.QueryString;

            // Ensure that there is at least one key in the query string, and check whether the "token" key is present.
            // Make sure we don't go here when we get here via a BACK from Crop.
            if (queryStrings.ContainsKey("token") 
                && e.NavigationMode != System.Windows.Navigation.NavigationMode.Back)
            {
                this.NavigationService.Navigate(new Uri(string.Format("/Crop.xaml?token={0}",queryStrings["token"]), UriKind.Relative));
            }

            UpdateMockup(false);
           
            base.OnNavigatedTo(e);
        }

        protected override void OnBackKeyPress(CancelEventArgs e)
        {
            if (theHelpPopup != null && theHelpPopup.IsOpen == true)
            {
                theHelpPopup.IsOpen = false;
                e.Cancel = true;
            }

            if (theSavePopup != null && theSavePopup.IsOpen == true)
            {
                backButtonPressedFlag = true;
                theSavePopup.IsOpen = false;
                e.Cancel = true;
            }

            base.OnBackKeyPress(e);
        }

        bool backButtonPressedFlag = false;

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

            var custom = System.Threading.Thread.CurrentThread.CurrentUICulture.DateTimeFormat.ShortTimePattern.Replace("t", "");

            Time.Text = DateTime.Now.ToString(custom);

            DayOfWeek.Text = DateTime.Now.ToString("dddd");
            MonthDay.Text = DateTime.Now.ToString("M");
        }

        private Popup theHelpPopup;
        private Popup theSavePopup; 

        private void ApplicationBarHelpIconButton_Click(object sender, EventArgs e)
        {
            PreparePopups(); //lazy setup

            this.ApplicationBar.IsVisible = false;
            theHelpPopup.IsOpen = true;
        }

        private void PreparePopups()
        {
            if (theHelpPopup == null)
            {
                theHelpPopup = new Popup();
                theHelpPopup.Child = new HelpPopup();
                theHelpPopup.VerticalOffset = 200;
                theHelpPopup.HorizontalOffset = 0;
                theHelpPopup.Closed += (sender1, e1) =>
                {
                    this.ApplicationBar.IsVisible = true;
                };
            }
            if (theSavePopup == null)
            {
                theSavePopup = new Popup();
                theSavePopup.Child = new HelpSavePopup();
                theSavePopup.VerticalOffset = 70;
                theSavePopup.HorizontalOffset = 0;
                theSavePopup.Closed += (sender1, e1) =>
                {
                    try
                    {
                        if (!backButtonPressedFlag)
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
                                
                                var toast = new ToastPrompt
                                {
                                    Title = "Saved",
                                    Message = @"Your wallpaper is in ""Saved Pictures"""
                                };
                                toast.Show();   
                            }
                        }
                        backButtonPressedFlag = false;
                    }
                    finally
                    {
                        UpdateMockup(false);

                    }
                };

            }
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
            catch (System.InvalidOperationException)
            {
                MessageBox.Show("An error occurred.");
            }
        }

        private void ApplicationBarSaveIconButton_Click(object sender, EventArgs e)
        {
            //PreparePopups(); //lazy setup
            //// Open the popup. 
            //UpdateMockup(true);
            //theSavePopup.IsOpen = true;
            HideBeforeSnapshot();

            Dispatcher.BeginInvoke(CreateImageToSaveAsWallpaper);
        }

        private void HideBeforeSnapshot()
        {
            MediaPanel.Opacity = 0;
            TimePanel.Opacity = 0;
        }

        private void ShowAfterSnapshot()
        {
            MediaPanel.Opacity = 100;
            TimePanel.Opacity = 100;
        }

        private async void CreateImageToSaveAsWallpaper()
        {
            using (var section = await acs.EnterAsync())
            {
                var bitmap = new System.Windows.Media.Imaging.WriteableBitmap(this.LayoutRoot, null);
                using (var stream = new System.IO.MemoryStream())
                {
                    System.Windows.Media.Imaging.Extensions.SaveJpeg(bitmap, stream,
                                    bitmap.PixelWidth, bitmap.PixelHeight, 0, 100);
                    stream.Position = 0;
                    var datetime = System.DateTime.Now;
                    var filename =
                        System.String.Format("LockScreen-{0}-{1}-{2}-{3}-{4}-{5}.jpg",
                            datetime.Year % 100, datetime.Month, datetime.Day,
                            datetime.Hour, datetime.Minute, datetime.Second);

                    var mediaLib = new Microsoft.Xna.Framework.Media.MediaLibrary();
                    mediaLib.SavePicture(filename, stream);
                    stream.Seek(0, SeekOrigin.Begin);
                    ShowAfterSnapshot();

                    var file = await ApplicationData.Current.LocalFolder.CreateFileAsync(filename, CreationCollisionOption.ReplaceExisting);
                    using (var s = await file.OpenAsync(FileAccessMode.ReadWrite))
                    using (var dw = new DataWriter(s))
                    {
                        byte[] imageData = new byte[stream.Length];
                        await stream.ReadAsync(imageData, 0, (int)stream.Length);
                        dw.WriteBytes(imageData);
                        await dw.StoreAsync();
                    }

                    if (!LockScreenManager.IsProvidedByCurrentApplication)
                    {
                        LockScreenRequestResult result = await LockScreenManager.RequestAccessAsync();
                        if (result == LockScreenRequestResult.Granted)
                        {
                            SetAsWallpaper(filename);
                        }
                    }
                    else
                    {
                        SetAsWallpaper(filename);
                    }

                    var toast = new ToastPrompt
                    {
                        Title = "Saved",
                        Message = @"Saved! Lock your phone to see!"
                    };
                    toast.Show();
                }
            }
        }


        private void SetAsWallpaper(string filename)
        {
            string realPath = "ms-appdata:///local/" + filename;
            //string realPath = "file://" + filename.Replace(@"\", "/");
            //string realPath = "ms-appdata:///" +
            //                  Windows.ApplicationModel.Package.Current.InstalledLocation.Path
            //                  + "/"
            //                  + filename;
            ;
            Debug.WriteLine(realPath);
            //Debug.WriteLine(ApplicationData.Current.LocalFolder.Path);

            LockScreen.SetImageUri(new Uri(realPath, UriKind.Absolute));
        }

        private void UpdateMockup(bool p)
        {
            if (p)
            {
                this.DimBorder.Visibility = System.Windows.Visibility.Collapsed;
                this.MediaPanelInternal.Visibility = System.Windows.Visibility.Collapsed;
                this.ApplicationBar.IsVisible = false;
                this.TimePanel.Visibility = System.Windows.Visibility.Collapsed;
            }
            else
            {
                this.DimBorder.Visibility = System.Windows.Visibility.Visible;
                this.MediaPanelInternal.Visibility = System.Windows.Visibility.Visible;
                this.ApplicationBar.IsVisible = true;
                this.TimePanel.Visibility = System.Windows.Visibility.Visible;
            }

            //TODO: Understand why this didn't just work with a convertor?
            if (ViewModel.MakeRoomforMedia == false)
            {
                this.MediaPanel.Visibility = System.Windows.Visibility.Collapsed;
            }
            else
            {
                this.MediaPanel.Visibility = System.Windows.Visibility.Visible;
            }
        }

        private bool LightTheme
        {
            get {
                Visibility v = (Visibility)Resources["PhoneLightThemeVisibility"];
                return v == System.Windows.Visibility.Visible;
            }        
        }

        private void ApplicationBarThemeIconButton_Click(object sender, EventArgs e)
        {
            ViewModel.InvertedText = !ViewModel.InvertedText;
            UpdateInvertStatus();
        }

        private void UpdateInvertStatus()
        {
            ApplicationBarIconButton foo = ApplicationBar.Buttons[3] as ApplicationBarIconButton;
            if (ViewModel.InvertedText)
            {
                foo.IconUri = new Uri("Images\\appbar.theme.inverse.png", UriKind.Relative);
            }
            else
            {
                foo.IconUri = new Uri("Images\\appbar.theme.normal.png", UriKind.Relative);
            }

            Style foundLineStyle;
            if (ViewModel.InvertedText)
            {
                foundLineStyle = (Style)Application.Current.Resources["BolderPhoneTextTitleContrast"];
            }
            else
            {
                foundLineStyle = (Style)Application.Current.Resources["BolderPhoneTextTitle"];
            }
            FoundLine1.Style = foundLineStyle;
            FoundLine2.Style = foundLineStyle;
            FoundLine3.Style = foundLineStyle;
            FoundLine4.Style = foundLineStyle;

        }

        private void ApplicationBarAboutMenuItem_Click(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/YourLastAboutDialog;component/AboutPage.xaml", UriKind.Relative));
        }
    }

    public sealed class BooleanToVisibilityConverter : IValueConverter
    {
        public bool IsReversed { get; set; }
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var val = System.Convert.ToBoolean(value, CultureInfo.InvariantCulture);
            if (this.IsReversed)    
            {
                val = !val;
            }
            if (val)
            {
                return Visibility.Visible;
            }
            return Visibility.Collapsed;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}