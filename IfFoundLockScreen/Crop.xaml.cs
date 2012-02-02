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
            //WriteableBitmap wb = new WriteableBitmap(480, 800);
            
            //CompositeTransform r = new CompositeTransform();
            //r.ScaleX = TotalImageScale * 1.3;
            //r.ScaleY = TotalImageScale * 1.3;
            //r.TranslateX = ImagePosition.X-60;
            //r.TranslateY = ImagePosition.Y-60;
            //r.Transform(new Point(0,0));
            

            //wb.Render(ImageToCrop, r);
            //wb.Invalidate();

            // Get the size of the source image captured by the camera
            BitmapImage obi = ImageToCrop.Source as BitmapImage;

            double originalImageWidth = obi.PixelWidth;
            double originalImageHeight = obi.PixelHeight;

            // Get the size of the image when it is displayed on the phone
            double displayedWidth = ImageToCrop.ActualWidth;
            double displayedHeight = ImageToCrop.ActualHeight;

            // Calculate the ratio of the original image to the displayed image
            double widthRatio = originalImageWidth / displayedWidth;
            double heightRatio = originalImageHeight / displayedHeight;

            // Create a new WriteableBitmap. The size of the bitmap is the size of the cropping rectangle
            // drawn by the user, multiplied by the image size ratio.

            Point p1 = new Point(60, 60);
            Point p2 = new Point(60 + 360, 60 + 600);

            WriteableBitmap wb = new WriteableBitmap((int)(widthRatio * Math.Abs(p2.X - p1.X)), (int)(heightRatio * Math.Abs(p2.Y - p1.Y)));

            // Calculate the offset of the cropped image. This is the distance, in pixels, to the top left corner
            // of the cropping rectangle, multiplied by the image size ratio.
            int xoffset = (int)(((p1.X < p2.X) ? p1.X : p2.X) * widthRatio);
            int yoffset = (int)(((p1.Y < p2.Y) ? p1.Y : p2.X) * heightRatio);

            // Copy the pixels from the targeted region of the source image into the target image, 
            // using the calculated offset
            for (int i = 0; i < wb.Pixels.Length; i++)
            {
                int x = (int)((i % wb.PixelWidth) + xoffset);
                int y = (int)((i / wb.PixelHeight) + yoffset);
                wb.Pixels[i] = wb.Pixels[y * wb.PixelHeight + x];
            }



            ((App)App.Current).SaveCustomBackground(wb);


            //this.NavigationService.GoBack();
        }
    }

}

