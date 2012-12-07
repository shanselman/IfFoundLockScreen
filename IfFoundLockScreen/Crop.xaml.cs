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
using System.Windows.Media.Imaging;
using System.IO;
using Microsoft.Xna.Framework.Media;
using ExifLib;
using Coding4Fun.Phone.Controls;
using System.Threading;
using System.Windows.Controls.Primitives;

namespace IfFoundLockScreen
{
    public partial class Crop : PhoneApplicationPage
    {
        public Crop()
        {
            InitializeComponent();

            if (MultiRes.Is720p)
            {
                imageControl.Height = 853;
                CropBorder.Width = 338;
                LeftDimBorder.Width = 71;
                RightDimBorder.Width = 71;
                BottomDimBorder.Height = 200;
            }
        }

        private void ImageViewer_ImageOpened(object sender, EventArgs e)
        {
            progressBar.Visibility = Visibility.Collapsed;
        }

        private void ImageViewer_ImageFailed(object sender, EventArgs e)
        {
            MessageBox.Show("Failed to load image");
            if (NavigationService.CanGoBack)
                NavigationService.GoBack();
        }

        private void ApplicationBarIconButton_Click(object sender, EventArgs e)
        {
            WriteableBitmap bmp = new WriteableBitmap(imageControl, null);
            bmp.Invalidate();

            //Actually save it
            ((App)App.Current).SaveCustomBackground(bmp);

            //fade the frame away then fly off the screen!
            Storyboard sb = new Storyboard();
            var duration = new Duration(TimeSpan.FromSeconds(0.750));

            var brushes = new string[] { "LeftDimBorder", "RightDimBorder", "TopDimBorder", "BottomDimBorder" };
            foreach (var brush in brushes)
            {
                var fadeOut = new DoubleAnimation();
                fadeOut.From = 0.6;
                fadeOut.To = 1;
                fadeOut.Duration = duration;

                Storyboard.SetTarget(fadeOut, LayoutRoot.FindName(brush) as DependencyObject);
                Storyboard.SetTargetProperty(fadeOut, new PropertyPath("Opacity"));
                sb.Children.Add(fadeOut);
            }
            sb.Begin();

            sb.Completed += (s, ea) =>
            {

                //When we've darkened the outside, now fly away!

                Storyboard sb1 = new Storyboard();
                sb1.Duration = new Duration(TimeSpan.FromMilliseconds(250));

                var flyAway = new DoubleAnimationUsingKeyFrames();
                flyAway.KeyFrames.Add(new LinearDoubleKeyFrame() { KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(0)), Value = 0 });
                flyAway.KeyFrames.Add(new LinearDoubleKeyFrame() { KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(250)), Value = 800 });

                Storyboard.SetTarget(flyAway, this.LayoutRoot);
                Storyboard.SetTargetProperty(flyAway,
                    new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));
                sb1.Children.Add(flyAway);
                sb1.Begin();
                sb1.Completed += (s2, ea2) =>
                {
                    if (NavigationService.CanGoBack)
                        NavigationService.GoBack();
                };
            };

        }

        protected override void OnBackKeyPress(System.ComponentModel.CancelEventArgs e)
        {
            this.NavigationService.RemoveBackEntry();
            base.OnBackKeyPress(e);
        }

        public class DataContextObject
        {
            public Uri ImageUri { get; set; }
            public Uri ThumbnailUri { get; set; }
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            //MessageBox.Show("NavMode:" + e.NavigationMode.ToString());
            //MessageBox.Show("Initiator:" + e.IsNavigationInitiator);

            //Did we get deep linked to?
            // Get a dictionary of query string keys and values.
            IDictionary<string, string> queryStrings = this.NavigationContext.QueryString;

            if (System.Diagnostics.Debugger.IsAttached)
            {
                imageControl.Image = new BitmapImage(new Uri("Images/BigImage.jpg", UriKind.Relative));

            }

            if (queryStrings.ContainsKey("token"))
            {
                // Retrieve the picture from the media library using the token passed to the application.
                MediaLibrary library = new MediaLibrary();
                Picture picture = library.GetPictureFromToken(queryStrings["token"]);

                // Create a WriteableBitmap object and add it to the Image control Source property.
                Stream s = picture.GetImage();
                JpegInfo info = ExifLib.ExifReader.ReadJpeg(s, "magic.jpg");

                var _width = info.Width;
                var _height = info.Height;
                var _orientation = info.Orientation;
                var _angle = 0;

                switch (info.Orientation)
                {
                    case ExifOrientation.TopLeft:
                    case ExifOrientation.Undefined:
                        _angle = 0;
                        break;
                    case ExifOrientation.TopRight:
                        _angle = 90;
                        break;
                    case ExifOrientation.BottomRight:
                        _angle = 180;
                        break;
                    case ExifOrientation.BottomLeft:
                        _angle = 270;
                        break;
                }

                s.Seek(0, SeekOrigin.Begin);

                //DEBUG
                //MessageBox.Show("Angle: " + _angle);

                //Stream resultStream;
                //if (_angle > 0d)
                //{
                //    resultStream = RotateStream(s, _angle);
                //}
                //else
                //{
                //    resultStream = s;
                //}

                BitmapImage bitmap = new BitmapImage();
                bitmap.CreateOptions = BitmapCreateOptions.None;
                bitmap.SetSource(s);

                this.imageControl.RenderTransformOrigin = new Point(0.5, 0.5);
                this.imageControl.Angle = _angle;
                this.imageControl.Image = bitmap;

            }
            base.OnNavigatedTo(e);
        }

        private Stream RotateStream(Stream stream, int angle)
        {
            stream.Position = 0;
            if (angle % 90 != 0 || angle < 0) throw new ArgumentException();
            if (angle % 360 == 0) return stream;

            BitmapImage bitmap = new BitmapImage();
            bitmap.SetSource(stream);
            WriteableBitmap wbSource = new WriteableBitmap(bitmap);

            WriteableBitmap wbTarget = null;
            if (angle % 180 == 0)
            {
                wbTarget = new WriteableBitmap(wbSource.PixelWidth, wbSource.PixelHeight);
            }
            else
            {
                wbTarget = new WriteableBitmap(wbSource.PixelHeight, wbSource.PixelWidth);
            }

            int width = wbSource.PixelWidth;
            int height = wbSource.PixelHeight;
            int targetWidth = wbTarget.PixelWidth;
            int[] sourcePixels = wbSource.Pixels;
            int[] targetPixels = wbTarget.Pixels;

            for (int x = 0; x < wbSource.PixelWidth; x++)
            {
                for (int y = 0; y < wbSource.PixelHeight; y++)
                {
                    switch (angle % 360)
                    {
                        case 90:
                            targetPixels[(height - y - 1) + x * targetWidth] = sourcePixels[x + y * width];
                            break;
                        case 180:
                            targetPixels[(width - x - 1) + (height - y - 1) * width] = sourcePixels[x + y * width];
                            break;
                        case 270:
                            targetPixels[y + (width - x - 1) * targetWidth] = sourcePixels[x + y * width];
                            break;
                    }
                }
            }
            MemoryStream targetStream = new MemoryStream();
            wbTarget.SaveJpeg(targetStream, wbTarget.PixelWidth, wbTarget.PixelHeight, 0, 100);
            return targetStream;
        }
    }

    public static class MultiRes
    {
        public static bool IsHighResolution
        {
            get { return Application.Current.Host.Content.ScaleFactor > 100; }
        }

        public static bool IsLowResolution
        {
            get { return Application.Current.Host.Content.ScaleFactor <= 100; }
        }

        public static bool IsWvga
        {
            get
            {
                return Application.Current.Host.Content.ActualHeight == 800
                    && Application.Current.Host.Content.ScaleFactor == 100;
            }
        }

        public static bool IsWxga
        {
            get { return Application.Current.Host.Content.ScaleFactor == 160; }
        }

        public static bool Is720p
        {
            get { return Application.Current.Host.Content.ScaleFactor == 150; }
        }

        public static String CurrentResolution
        {
            get
            {
                if (IsWvga) return "WVGA";
                else if (IsWxga) return "WXGA";
                else if (Is720p) return "HD720p";
                else throw new InvalidOperationException("Unknown resolution");
            }
        }
    }

}


