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

            ImageToCrop.ManipulationDelta += new EventHandler<ManipulationDeltaEventArgs>(ImageToCrop_ManipulationDelta);
            ImageToCrop.ManipulationStarted += new EventHandler<ManipulationStartedEventArgs>(ImageToCrop_ManipulationStarted);
            ImageToCrop.RenderTransform = new CompositeTransform();
        }

        private Point? lastOrigin;
        private double lastUniformScale;

        void ImageToCrop_ManipulationStarted(object sender, ManipulationStartedEventArgs e)
        {
            lastUniformScale = Math.Sqrt(2);
            lastOrigin = null;
        }

        void ImageToCrop_ManipulationDelta(object sender, ManipulationDeltaEventArgs e)
        {
            var transform = ImageToCrop.RenderTransform as CompositeTransform;
            if (transform != null)
            {
                var origin = e.ManipulationContainer.TransformToVisual(this).Transform(e.ManipulationOrigin);

                if (!lastOrigin.HasValue)
                    lastOrigin = origin;

                //Calculate uniform scale factor
                double uniformScale = Math.Sqrt(Math.Pow(e.CumulativeManipulation.Scale.X, 2) +
                                                Math.Pow(e.CumulativeManipulation.Scale.Y, 2));
                if (uniformScale == 0)
                    uniformScale = lastUniformScale;

                //Current scale factor
                double scale = uniformScale / lastUniformScale;

                if (scale > 0 && scale != 1)
                {
                    //Apply scaling
                    transform.ScaleY = transform.ScaleX *= scale;
                    //Update the offset caused by this scaling
                    var ul = ImageToCrop.TransformToVisual(this).Transform(new Point());
                    transform.TranslateX = origin.X - (origin.X - ul.X) * scale;
                    transform.TranslateY = origin.Y - (origin.Y - ul.Y) * scale;
                }
                //Apply translate caused by drag
                transform.TranslateX += (origin.X - lastOrigin.Value.X);
                transform.TranslateY += (origin.Y - lastOrigin.Value.Y);

                //Cache values for next time
                lastOrigin = origin;
                lastUniformScale = uniformScale;
            }

        }

        
        private void ApplicationBarIconButton_Click(object sender, EventArgs e)
        {
            lock (imageToCropReadyLock)
            {
                if (!imageToCropReady) return;
                imageToCropReady = true;
            }
            
            // Get the size of the source image 
            BitmapImage obi = ImageToCrop.Source as BitmapImage;

            double originalImageWidth = obi.PixelWidth;
            double originalImageHeight = obi.PixelHeight;

            //// Get the size of the image when it is displayed on the phone
            double displayedWidth = ImageToCrop.ActualWidth;
            double displayedHeight = ImageToCrop.ActualHeight;

            //// Calculate the ratio of the original image to the displayed image
            double widthRatio = originalImageWidth / displayedWidth;
            double heightRatio = originalImageHeight / displayedHeight;

            //Our goal is an image that is this size
            double goalHeight = 800;
            double goalWidth = 480;

            //Get the Rect of our crop window
            Point imageToCropP1 = CropBorder.TransformToVisual(ImageToCrop).Transform(new Point(0, 0));
            Point imageToCropP2 = CropBorder.TransformToVisual(ImageToCrop).Transform(new Point(CropBorder.ActualWidth, CropBorder.ActualHeight));

            RectangleGeometry geo = new RectangleGeometry();
            geo.Rect = new Rect(imageToCropP1, imageToCropP2);

            //double viewPortWidthRatio = CropBorder.ActualWidth / goalWidth;
            //double viewPortHeightRatio = CropBorder.ActualHeight / goalHeight;

            WriteableBitmap wb = new WriteableBitmap((int)goalWidth, (int)goalHeight); //size of our goal

            //double uniformScaleFactor = (Math.Sqrt(Math.Pow((60 / ImageToCrop.ActualWidth), 2) +
            //                        Math.Pow((60 / ImageToCrop.ActualHeight), 2)));

            // Calculate the offset of the cropped image. This is the distance, in pixels, to the top left corner
            // of the cropping rectangle, multiplied by the image size ratio.
            //int xoffset = (int)(((imageToCropP1.X < imageToCropP2.X) ? imageToCropP1.X : imageToCropP2.X) * ((CompositeTransform)ImageToCrop.RenderTransform).ScaleX);
            //int yoffset = (int)(((imageToCropP1.Y < imageToCropP2.Y) ? imageToCropP1.Y : imageToCropP2.X) * ((CompositeTransform)ImageToCrop.RenderTransform).ScaleY);
            //int xoffset = (int)((60 * ((CompositeTransform)ImageToCrop.RenderTransform).ScaleX));
            //int yoffset = (int)((60 * ((CompositeTransform)ImageToCrop.RenderTransform).ScaleY));

            CompositeTransform transform = new CompositeTransform();
            transform.ScaleX = ((CompositeTransform)ImageToCrop.RenderTransform).ScaleX; //viewportratio
            transform.ScaleY = ((CompositeTransform)ImageToCrop.RenderTransform).ScaleY;
            transform.CenterX = 0;
            transform.CenterY = 0;
            transform.TranslateX = ((CompositeTransform)ImageToCrop.RenderTransform).TranslateX;
            transform.TranslateY = ((CompositeTransform)ImageToCrop.RenderTransform).TranslateY;

            wb.Render(ImageToCrop, transform);
            wb.Invalidate();

            //Actually save it
            ((App)App.Current).SaveCustomBackground(wb);

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

                Storyboard.SetTarget(fadeOut, canvas.FindName(brush) as DependencyObject);
                Storyboard.SetTargetProperty(fadeOut, new PropertyPath("Opacity"));
                sb.Children.Add(fadeOut);
            }
            sb.Begin();

            sb.Completed += (s, ea) => { 
                
                    //When we've darkened the outside, now fly away!
                    ImageToCrop.Clip = geo;

                    Storyboard sb1 = new Storyboard();
                    sb1.Duration = new Duration(TimeSpan.FromMilliseconds(250));
                    
                    var flyAway = new DoubleAnimationUsingKeyFrames();
                    flyAway.KeyFrames.Add(new LinearDoubleKeyFrame() { KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(0)), Value = 0 });
                    flyAway.KeyFrames.Add(new LinearDoubleKeyFrame() { KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(250)), Value = 800 });
                    
                    Storyboard.SetTarget(flyAway, this.ContentPanel);
                    Storyboard.SetTargetProperty(flyAway, 
                        new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)")); 
                    sb1.Children.Add(flyAway);
                    sb1.Begin();
                    sb1.Completed += (s2,ea2) => {
                        this.NavigationService.GoBack();
                    };
            };
            
        }

        protected override void OnBackKeyPress(System.ComponentModel.CancelEventArgs e)
        {
            this.NavigationService.RemoveBackEntry();
            base.OnBackKeyPress(e);
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
                ImageToCrop.Source = new BitmapImage(new Uri("Images/BigImage.jpg", UriKind.Relative));
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
                

                //DEBUG
                //MessageBox.Show("Angle: " + _angle);

                Stream resultStream;
                if (_angle > 0d)
                {
                    resultStream = RotateStream(s, _angle);
                }
                else
                {
                    resultStream = s;
                }

                BitmapImage bitmap = new BitmapImage();
                bitmap.CreateOptions = BitmapCreateOptions.None;
                bitmap.SetSource(resultStream);

                this.ImageToCrop.Source = bitmap;

                //TODO: Hardware accelerated attempt #1
                //RotateImage(_angle, ImageToCrop, wb);

                //TODO: Hardware accelerated attempt #2
                //CompositeTransform t = ImageToCrop.RenderTransform as CompositeTransform;
                //RotateTransform r = new RotateTransform();
                //r.Angle = _angle;


            }
            base.OnNavigatedTo(e);
        }

        private void RotateImage(int angle, Image image, WriteableBitmap _wbImage)
        {
            WriteableBitmap wbdest = new WriteableBitmap(_wbImage.PixelHeight, _wbImage.PixelWidth);

            System.Windows.Media.RotateTransform rotTransform = new System.Windows.Media.RotateTransform();
            System.Windows.Media.TranslateTransform traTransform = new System.Windows.Media.TranslateTransform();
            System.Windows.Media.TransformGroup group = new System.Windows.Media.TransformGroup();

            traTransform.X = -(_wbImage.PixelWidth - _wbImage.PixelHeight) / 2.0;
            traTransform.Y = -(_wbImage.PixelHeight - _wbImage.PixelWidth) / 2.0;

            rotTransform.CenterX = (double)_wbImage.PixelWidth / 2.0;
            rotTransform.CenterY = (double)_wbImage.PixelHeight / 2.0;
            rotTransform.Angle = angle;

            group.Children = new System.Windows.Media.TransformCollection();
            group.Children.Add(rotTransform);
            group.Children.Add(traTransform);

            image.Margin = new Thickness(0);
            image.Width = _wbImage.PixelWidth;
            image.Height = _wbImage.PixelHeight;
            image.Source = _wbImage;
            image.RenderTransformOrigin = new Point(0, 0);

            wbdest.Render(image, group);

            //image = null;
            rotTransform = null;
            traTransform = null;
            group = null;

            wbdest.Invalidate();

            //AfterAction(wbdest);

            wbdest = null;
            GC.Collect();
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

          private object imageToCropReadyLock = new object();  
          private bool imageToCropReady = false;
          private void ImageToCrop_Loaded(object sender, RoutedEventArgs e)
          {
              lock (imageToCropReadyLock)
              {
                  imageToCropReady = true;
              }
          }

    }

    }


