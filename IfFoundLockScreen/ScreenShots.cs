//
// Copyright (c) 2010-2011 Jeff Wilcox
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//    http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//

using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.IsolatedStorage;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace System.Windows
{
    /// <summary>
    /// Offers the ability to store images every two seconds into the isolated
    /// storage that can then be retrieved using the isolated storage tool in
    /// the 7.1 SDK.
    /// </summary>
    public class ScreenShots
    {
        private ScreenShots()
        {
            _isf = IsolatedStorageFile.GetUserStoreForApplication();

            try
            {
                _isf.CreateDirectory("screenshots");
            }
            catch
            {
                // OK the directory already exists.
            }
        }

        private DispatcherTimer _dt;
        private double _interval;
        private IsolatedStorageFile _isf;
        private static ScreenShots _instance;

        public static void BeginTakingPictures(double interval = 2.0)
        {
            if (_instance == null)
            {
                _instance = new ScreenShots();
                _instance.Start(interval);
            }
            else if (_instance._dt != null)
            {
                _instance._dt.Start();
            }
        }

        public static void Stop()
        {
            if (_instance != null && _instance._dt != null)
            {
                _instance._dt.Stop();
            }
        }

        private void Start(double interval)
        {
            _interval = interval;

            if (_dt == null)
            {
                _dt = new DispatcherTimer();
                _dt.Interval = TimeSpan.FromSeconds(_interval);
                _dt.Tick += OnTick;
                _dt.Start();
            }
        }

        private void OnTick(object sender, EventArgs e)
        {
            var ui = Application.Current.RootVisual;
            try
            {
                if (ui != null)
                {
                    FrameworkElement fe = ui as FrameworkElement;
                    if (fe != null)
                    {
                        var width = fe.ActualWidth;
                        var height = fe.ActualHeight;

                        WriteableBitmap wb = new WriteableBitmap(ui,
                            new TranslateTransform());
                        wb.Render(ui, new TranslateTransform());
                        byte[] bb = EncodeToJpeg(wb);

                        string filename = "screenshots\\"
                            + DateTime.Now.Ticks
                            .ToString(CultureInfo.InvariantCulture)
                            + ".jpg";
                        using (var st = _isf.CreateFile(filename))
                        {
                            st.Write(bb, 0, bb.Length);
                        }

                        Debug.WriteLine("Saved screenshot to " + filename);
                    }
                }
            }
            catch (Exception)
            {
            }
        }

        public byte[] EncodeToJpeg(WriteableBitmap wb)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                wb.SaveJpeg(
                    stream,
                    wb.PixelWidth,
                    wb.PixelHeight,
                    0,
                    85);
                return stream.ToArray();
            }
        }
    }
}
