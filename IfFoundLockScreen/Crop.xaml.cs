﻿using System;
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

namespace IfFoundLockScreen
{
    public partial class Crop : PhoneApplicationPage
    {
        public Crop()
        {
            InitializeComponent();

        }

        // these two fields fully define the zoom state:
        private double TotalImageScale = 1d;
        private Point ImagePosition = new Point(0, 0);

        private const double MAX_IMAGE_ZOOM = 5;
        private Point _oldFinger1;
        private Point _oldFinger2;
        private double _oldScaleFactor;

        #region Event handlers

        /// <summary>
        /// Initializes the zooming operation
        /// </summary>
        private void OnPinchStarted(object sender, PinchStartedGestureEventArgs e)
        {
            _oldFinger1 = e.GetPosition(ImageToCrop, 0);
            _oldFinger2 = e.GetPosition(ImageToCrop, 1);
            _oldScaleFactor = 1;
        }

        /// <summary>
        /// Computes the scaling and translation to correctly zoom around your fingers.
        /// </summary>
        private void OnPinchDelta(object sender, PinchGestureEventArgs e)
        {
            var scaleFactor = e.DistanceRatio / _oldScaleFactor;
            if (!IsScaleValid(scaleFactor))
                return;

            var currentFinger1 = e.GetPosition(ImageToCrop, 0);
            var currentFinger2 = e.GetPosition(ImageToCrop, 1);

            var translationDelta = GetTranslationDelta(
                currentFinger1,
                currentFinger2,
                _oldFinger1,
                _oldFinger2,
                ImagePosition,
                scaleFactor);

            _oldFinger1 = currentFinger1;
            _oldFinger2 = currentFinger2;
            _oldScaleFactor = e.DistanceRatio;

            UpdateImageScale(scaleFactor);
            UpdateImagePosition(translationDelta);
        }

        /// <summary>
        /// Moves the image around following your finger.
        /// </summary>
        private void OnDragDelta(object sender, DragDeltaGestureEventArgs e)
        {
            var translationDelta = new Point(e.HorizontalChange, e.VerticalChange);

            if (IsDragValid(1, translationDelta))
                UpdateImagePosition(translationDelta);
        }

        /// <summary>
        /// Resets the image scaling and position
        /// </summary>
        private void OnDoubleTap(object sender, Microsoft.Phone.Controls.GestureEventArgs e)
        {
            ResetImagePosition();
        }
        
        #endregion

        #region Utils

        /// <summary>
        /// Computes the translation needed to keep the image centered between your fingers.
        /// </summary>
        private Point GetTranslationDelta(
            Point currentFinger1, Point currentFinger2,
            Point oldFinger1, Point oldFinger2,
            Point currentPosition, double scaleFactor)
        {
            var newPos1 = new Point(
             currentFinger1.X + (currentPosition.X - oldFinger1.X) * scaleFactor,
             currentFinger1.Y + (currentPosition.Y - oldFinger1.Y) * scaleFactor);

            var newPos2 = new Point(
             currentFinger2.X + (currentPosition.X - oldFinger2.X) * scaleFactor,
             currentFinger2.Y + (currentPosition.Y - oldFinger2.Y) * scaleFactor);

            var newPos = new Point(
                (newPos1.X + newPos2.X) / 2,
                (newPos1.Y + newPos2.Y) / 2);

            return new Point(
                newPos.X - currentPosition.X,
                newPos.Y - currentPosition.Y);
        }

        /// <summary>
        /// Updates the scaling factor by multiplying the delta.
        /// </summary>
        private void UpdateImageScale(double scaleFactor)
        {
            TotalImageScale *= scaleFactor;
            ApplyScale();
        }

        /// <summary>
        /// Applies the computed scale to the image control.
        /// </summary>
        private void ApplyScale()
        {
            ((CompositeTransform)ImageToCrop.RenderTransform).ScaleX = TotalImageScale;
            ((CompositeTransform)ImageToCrop.RenderTransform).ScaleY = TotalImageScale;
        }

        /// <summary>
        /// Updates the image position by applying the delta.
        /// Checks that the image does not leave empty space around its edges.
        /// </summary>
        private void UpdateImagePosition(Point delta)
        {
            var newPosition = new Point(ImagePosition.X + delta.X, ImagePosition.Y + delta.Y);

            //if (newPosition.X > 0) newPosition.X = 0;
            //if (newPosition.Y > 0) newPosition.Y = 0;

            //if ((ImageToCrop.ActualWidth * TotalImageScale) + newPosition.X < ImageToCrop.ActualWidth)
            //    newPosition.X = ImageToCrop.ActualWidth - (ImageToCrop.ActualWidth * TotalImageScale);

            //if ((ImageToCrop.ActualHeight * TotalImageScale) + newPosition.Y < ImageToCrop.ActualHeight)
            //    newPosition.Y = ImageToCrop.ActualHeight - (ImageToCrop.ActualHeight * TotalImageScale);

            ImagePosition = newPosition;

            ApplyPosition();
        }

        /// <summary>
        /// Applies the computed position to the image control.
        /// </summary>
        private void ApplyPosition()
        {
            ((CompositeTransform)ImageToCrop.RenderTransform).TranslateX = ImagePosition.X;
            ((CompositeTransform)ImageToCrop.RenderTransform).TranslateY = ImagePosition.Y;
        }

        /// <summary>
        /// Resets the zoom to its original scale and position
        /// </summary>
        private void ResetImagePosition()
        {
            TotalImageScale = 1;
            ImagePosition = new Point(0, 0);
            ApplyScale();
            ApplyPosition();
        }

        /// <summary>
        /// Checks that dragging by the given amount won't result in empty space around the image
        /// </summary>
        private bool IsDragValid(double scaleDelta, Point translateDelta)
        {
            return true;

            if (ImagePosition.X + translateDelta.X > 0 || ImagePosition.Y + translateDelta.Y > 0)
                return false;

            if ((ImageToCrop.ActualWidth * TotalImageScale * scaleDelta) + (ImagePosition.X + translateDelta.X) < ImageToCrop.ActualWidth)
                return false;

            if ((ImageToCrop.ActualHeight * TotalImageScale * scaleDelta) + (ImagePosition.Y + translateDelta.Y) < ImageToCrop.ActualHeight)
                return false;

            return true;
        }

        /// <summary>
        /// Tells if the scaling is inside the desired range
        /// </summary>
        private bool IsScaleValid(double scaleDelta)
        {
            return (TotalImageScale * scaleDelta >= 0.8) && (TotalImageScale * scaleDelta <= MAX_IMAGE_ZOOM);
        }

        #endregion

        private void ApplicationBarIconButton_Click(object sender, EventArgs e)
        {
            // Get the size of the source image 
            BitmapImage obi = ImageToCrop.Source as BitmapImage;

            double originalImageWidth = obi.PixelWidth;
            double originalImageHeight = obi.PixelHeight;

            // Get the size of the image when it is displayed on the phone
            double displayedWidth = ImageToCrop.ActualWidth;
            double displayedHeight = ImageToCrop.ActualHeight;

            // Calculate the ratio of the original image to the displayed image
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

            double viewPortWidthRatio = CropBorder.ActualWidth / goalWidth;
            double viewPortHeightRatio = CropBorder.ActualHeight / goalHeight;

            WriteableBitmap wb = new WriteableBitmap((int)goalWidth, (int)goalHeight); //size of our goal

            // Calculate the offset of the cropped image. This is the distance, in pixels, to the top left corner
            // of the cropping rectangle, multiplied by the image size ratio.
            int xoffset = (int)(((imageToCropP1.X < imageToCropP2.X) ? imageToCropP1.X : imageToCropP2.X) * widthRatio);
            int yoffset = (int)(((imageToCropP1.Y < imageToCropP2.Y) ? imageToCropP1.Y : imageToCropP2.X) * heightRatio);

            CompositeTransform transform = new CompositeTransform();
            transform.ScaleX = TotalImageScale * (1 / viewPortWidthRatio);
            transform.ScaleY = TotalImageScale * (1 / viewPortHeightRatio);
            transform.CenterX = imageToCropP1.X;
            transform.CenterY = imageToCropP1.Y;
            transform.TranslateX = -(imageToCropP1.X);
            transform.TranslateY = -(imageToCropP1.Y);
            wb.Render(ImageToCrop, transform);
            wb.Invalidate();

            //Actually save it
            ((App)App.Current).SaveCustomBackground(wb);

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
                
                    //When we've darken the outside, now fly away!
                    ImageToCrop.Clip = geo;

                    Storyboard sb1 = new Storyboard();
                    sb1.Duration = new Duration(TimeSpan.FromMilliseconds(400));
                    
                    var flyAway = new DoubleAnimationUsingKeyFrames();
                    flyAway.KeyFrames.Add(new LinearDoubleKeyFrame() { KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(0)), Value = 0 });
                    flyAway.KeyFrames.Add(new LinearDoubleKeyFrame() { KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(400)), Value = 800 });
                    
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

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            //Did we get deep linked to?
            // Get a dictionary of query string keys and values.
            IDictionary<string, string> queryStrings = this.NavigationContext.QueryString;

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
                MessageBox.Show("Angle: " + _angle);

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

    }


