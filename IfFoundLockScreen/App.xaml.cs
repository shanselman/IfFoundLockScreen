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
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System.IO.IsolatedStorage;
using System.IO;
using System.Xml.Serialization;
using System.Windows.Media.Imaging;
using System.Windows.Resources;

namespace IfFoundLockScreen
{
    public partial class App : Application
    {
        /// <summary>
        /// Provides easy access to the root frame of the Phone Application.
        /// </summary>
        /// <returns>The root frame of the Phone Application.</returns>
        public PhoneApplicationFrame RootFrame { get; private set; }

        public RewardModel ViewModel { get; private set; }


        
        /// <summary>
        /// Constructor for the Application object.
        /// </summary>
        public App()
        {
            // Global handler for uncaught exceptions. 
            UnhandledException += Application_UnhandledException;

            // Standard Silverlight initialization
            InitializeComponent();

            // Phone-specific initialization
            InitializePhoneApplication();

            // Show graphics profiling information while debugging.
            if (System.Diagnostics.Debugger.IsAttached)
            {
                // Display the current frame rate counters.
                Application.Current.Host.Settings.EnableFrameRateCounter = true;

                // Display the metro grid helper.
                MetroGridHelper.IsVisible = true;

                // Show the areas of the app that are being redrawn in each frame.
                //Application.Current.Host.Settings.EnableRedrawRegions = true;

                // Enable non-production analysis visualization mode, 
                // which shows areas of a page that are handed off to GPU with a colored overlay.
                //Application.Current.Host.Settings.EnableCacheVisualization = true;

                // Disable the application idle detection by setting the UserIdleDetectionMode property of the
                // application's PhoneApplicationService object to Disabled.
                // Caution:- Use this under debug mode only. Application that disables user idle detection will continue to run
                // and consume battery power when the user is not using the phone.
                PhoneApplicationService.Current.UserIdleDetectionMode = IdleDetectionMode.Disabled;
            }

            //TODO: Screenshots!
            //System.Windows.ScreenShots.BeginTakingPictures();

        }

        // Code to execute when the application is launching (eg, from Start)
        // This code will not execute when the application is reactivated
        private void Application_Launching(object sender, LaunchingEventArgs e)
        {
            LoadData();

            // if the view model is not loaded, create a new one
            if (ViewModel == null)
            {
                ViewModel = new RewardModel();
                ViewModel.Update();
            }

            // set the frame DataContext
            RootFrame.DataContext = ViewModel;
        }

        public void LoadData()
        {
            // load the view model from isolated storage
            using (var store = IsolatedStorageFile.GetUserStoreForApplication())
            using (var stream = new IsolatedStorageFileStream("data.txt", FileMode.OpenOrCreate, FileAccess.Read, store))
            using (var reader = new StreamReader(stream))
            {
                if (!reader.EndOfStream)
                {
                    var serializer = new XmlSerializer(typeof(RewardModel));
                    ViewModel = (RewardModel)serializer.Deserialize(reader);

                }
            }
        }

        // Code to execute when the application is activated (brought to foreground)
        // This code will not execute when the application is first launched
        private void Application_Activated(object sender, ActivatedEventArgs e)
        {
            //if (PhoneApplicationService.Current.State.ContainsKey(ModelKey))
            //{
            //    ViewModel = PhoneApplicationService.Current.State[ModelKey] as RewardModel;
            //    RootFrame.DataContext = ViewModel;
            //}
            LoadData();
            // set the frame DataContext
            RootFrame.DataContext = ViewModel;


        }

        private readonly string ModelKey = "ViewModel";

        // Code to execute when the application is deactivated (sent to background)
        // This code will not execute when the application is closing
        private void Application_Deactivated(object sender, DeactivatedEventArgs e)
        {
            //PhoneApplicationService.Current.State[ModelKey] = ViewModel;
            SaveData();
        }

        // Code to execute when the application is closing (eg, user hit Back)
        // This code will not execute when the application is deactivated
        private void Application_Closing(object sender, ClosingEventArgs e)
        {
            SaveData();
        }

        public void SaveData()
        {
            // persist the data using isolated storage
            using (var store = IsolatedStorageFile.GetUserStoreForApplication())
            using (var stream = new IsolatedStorageFileStream("data.txt",
                                                            FileMode.Create,
                                                            FileAccess.Write,
                                                            store))
            {
                var serializer = new XmlSerializer(typeof(RewardModel));
                serializer.Serialize(stream, ViewModel);
            }
        }

        // Code to execute if a navigation fails
        private void RootFrame_NavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            if (System.Diagnostics.Debugger.IsAttached)
            {
                // A navigation has failed; break into the debugger
                System.Diagnostics.Debugger.Break();
            }

            LittleWatson.ReportException(e.Exception,"NavigationFailed");
        }

        // Code to execute on Unhandled Exceptions
        private void Application_UnhandledException(object sender, ApplicationUnhandledExceptionEventArgs e)
        {
            if (System.Diagnostics.Debugger.IsAttached)
            {
                // An unhandled exception has occurred; break into the debugger
                System.Diagnostics.Debugger.Break();
            }

            LittleWatson.ReportException(e.ExceptionObject,"UnhandledException");
        }

        public BitmapImage LoadCustomBackground()
        {
            using (IsolatedStorageFile myIsolatedStorage = IsolatedStorageFile.GetUserStoreForApplication())
            {
                if(myIsolatedStorage.FileExists("custombackground.jpg"))
                {
                    using (IsolatedStorageFileStream fileStream = myIsolatedStorage.OpenFile("custombackground.jpg", FileMode.Open, FileAccess.Read))
                    {
                        if (fileStream != null)
                        {
                            BitmapImage bi = new BitmapImage();
                            bi.CreateOptions = BitmapCreateOptions.BackgroundCreation; //TODO confirm this is cool
                            bi.SetSource(fileStream);
                            return bi;
                        }
                    }
                }
            }
            return null;
        }

        public void SaveCustomBackground(WriteableBitmap wb)
        {
            String tempJPEG = "custombackground.jpg";
            // Create virtual store and file stream. Check for duplicate tempJPEG files.
            using (IsolatedStorageFile myIsolatedStorage = IsolatedStorageFile.GetUserStoreForApplication())
            {
                if (myIsolatedStorage.FileExists(tempJPEG))
                {
                    myIsolatedStorage.DeleteFile(tempJPEG);
                }

                using (IsolatedStorageFileStream fileStream = myIsolatedStorage.CreateFile(tempJPEG))
                {

                    // Encode WriteableBitmap object to a JPEG stream.
                    Extensions.SaveJpeg(wb, fileStream, wb.PixelWidth, wb.PixelHeight, 0, 100);
                }
            }
        }

        public void SaveCustomBackground(BitmapImage bmp)
        {
            WriteableBitmap wb = new WriteableBitmap(bmp);
            SaveCustomBackground(wb);
        }




        #region Phone application initialization

        // Avoid double-initialization
        private bool phoneApplicationInitialized = false;

        // Do not add any additional code to this method
        private void InitializePhoneApplication()
        {
            if (phoneApplicationInitialized)
                return;

            // Create the frame but don't set it as RootVisual yet; this allows the splash
            // screen to remain active until the application is ready to render.
            //RootFrame = new PhoneApplicationFrame();
            RootFrame = new TransitionFrame { Background = new SolidColorBrush(Colors.Transparent) };
            RootFrame.Navigated += CompleteInitializePhoneApplication;

            // Handle navigation failures
            RootFrame.NavigationFailed += RootFrame_NavigationFailed;

            // Ensure we don't initialize again
            phoneApplicationInitialized = true;
        }

        // Do not add any additional code to this method
        private void CompleteInitializePhoneApplication(object sender, NavigationEventArgs e)
        {
            // Set the root visual to allow the application to render
            if (RootVisual != RootFrame)
                RootVisual = RootFrame;

            // Remove this handler since it is no longer needed
            RootFrame.Navigated -= CompleteInitializePhoneApplication;
        }

        #endregion
    }
}