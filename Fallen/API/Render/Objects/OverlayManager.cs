﻿using Overlay;
using System;
using System.Threading;

namespace FormOverlayExamples.Objects
{
    public class OverlayManager : IDisposable
    {
        bool exitThread;
        Thread serviceThread;

        public IntPtr ParentWindowHandle { get; private set; }

        public OverlayWindow Window { get; private set; }
        public Direct2DRenderer Graphics { get; private set; }

        public bool IsParentWindowVisible { get; private set; }

        private OverlayManager()
        {
        }

        public OverlayManager(IntPtr parentWindowHandle, bool vsync = false, bool measurefps = false, bool antialiasing = true)
        {
            var options = new Direct2DRendererOptions()
            {
                AntiAliasing = antialiasing,
                Hwnd = IntPtr.Zero,
                MeasureFps = measurefps,
                VSync = vsync
            };
            setupInstance(parentWindowHandle, options);
        }

        public OverlayManager(IntPtr parentWindowHandle, Direct2DRendererOptions options)
        {
            setupInstance(parentWindowHandle, options);
        }

        ~OverlayManager()
        {
            Dispose(false);
        }

        void setupInstance(IntPtr parentWindowHandle, Direct2DRendererOptions options)
        {
            ParentWindowHandle = parentWindowHandle;

            if (PInvoke.IsWindow(parentWindowHandle) == 0) throw new Exception("The parent window does not exist");

            var bounds = new PInvoke.RECT();
            PInvoke.GetRealWindowRect(parentWindowHandle, out bounds);

            var x = bounds.Left;
            var y = bounds.Top;

            var width = bounds.Right - x;
            var height = bounds.Bottom - y;

            Window = new OverlayWindow(x, y, width, height);

            options.Hwnd = Window.WindowHandle;

            Graphics = new Direct2DRenderer(options);

            serviceThread = new Thread(new ThreadStart(windowServiceThread))
            {
                IsBackground = true,
                Priority = ThreadPriority.BelowNormal
            };

            serviceThread.Start();
        }

        void windowServiceThread()
        {
            var bounds = new PInvoke.RECT();

            while (!exitThread)
            {
                Thread.Sleep(100);

                IsParentWindowVisible = PInvoke.IsWindowVisible(ParentWindowHandle) != 0;

                if (!IsParentWindowVisible)
                {
                    if (Window.IsVisible) Window.HideWindow();
                    continue;
                }

                if (!Window.IsVisible) Window.ShowWindow();

                PInvoke.GetRealWindowRect(ParentWindowHandle, out bounds);

                var x = bounds.Left;
                var y = bounds.Top;

                var width = bounds.Right - x;
                var height = bounds.Bottom - y;

                if (Window.X == x
                    && Window.Y == y
                    && Window.Width == width
                    && Window.Height == height) continue;

                Window.SetWindowBounds(x, y, width, height);
                Graphics.Resize(width, height);
            }
        }

        #region IDisposable Support

        bool disposedValue;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // managed
                }

                // unmanaged

                if (serviceThread != null)
                {
                    exitThread = true;

                    try
                    {
                        serviceThread.Join();
                    }
                    catch
                    {
                    }
                }

                Graphics.Dispose();
                Window.Dispose();

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion IDisposable Support
    }
}