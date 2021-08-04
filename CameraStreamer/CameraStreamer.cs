using DirectShowLib;
using System;
using System.Linq;
using System.Drawing;
using AForge.Video.DirectShow;
using Image = System.Windows.Controls.Image;
using Bitmap = System.Drawing.Bitmap;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;

namespace speyck.CameraStreamer
{
    public class CameraStreamer : IDisposable
    {
        /*
         *  Needed Nuget Libraries:
         *  - AForge
         *  - AForge.Video
         *  - AForge.Video.DirectShow
         *  - DirectShowLib
         *  - System.Drawing.Common
         *  
         *  Libraries from 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.6.1\':
         *  - PresentationCore
         *  - PresentationFramework
        */

        #region Static variables
        /// <summary>
        /// A list of all devices connected to the machine which are recognized as video-input devices
        /// </summary>
        public static IEnumerable<DsDevice> Devices { get; private set; } = DsDevice.GetDevicesOfCat(DirectShowLib.FilterCategory.VideoInputDevice);
        #endregion

        #region Events & Delegates
        /// <summary>
        /// Represents the method that will handle the event raised when a new frame 
        /// </summary>
        /// <param name="sender">The object which raises the event</param>
        /// <param name="e">Arguments such as the Frame are given through the EventArgs</param>
        public delegate void NewFrameEventHandler(object sender, NewFrameEventArgs e);

        /// <summary>
        /// Event which is raisedw when a new frame 
        /// </summary>
        public event NewFrameEventHandler OnNewFrame;
        #endregion

        #region Public variables
        /// <summary>
        /// Height of the webcam/device-frame. This is, under no circumstances, the highest resolution possible on the camera
        /// </summary>
        public int FrameHeight => videoSource.VideoResolution.FrameSize.Height;

        /// <summary>
        /// Width of the webcam/device-frame. This is, under no circumstances, the highest resolution possible on the camera
        /// </summary>
        public int FrameWidth => videoSource.VideoResolution.FrameSize.Width;

        /// <summary>
        /// Framerate of the webcam/device. This is, under no circumstances, the highest framerate possible on the camera
        /// </summary>
        public int FrameRate => videoSource.VideoResolution.AverageFrameRate;

        /// <summary>
        /// Id of the current video/stream source
        /// </summary>
        public string VideoSourceID => videoSource.Source;

        /// <summary>
        /// The control of the Windows.Controls.Image which will be used as a streaming canvas if it's given throug the constructor
        /// </summary>
        public Image ImageControl { get; private set; }
        #endregion

        #region Private variables
        /// <summary>
        /// Token which cancels the reader task
        /// </summary>
        private CancellationTokenSource cancelSource;

        /// <summary>
        /// Task holding the decoding code
        /// </summary>
        private Task streamTask;

        /// <summary>
        /// Device which is streaming the frames
        /// </summary>
        private VideoCaptureDevice videoSource;
        #endregion

        #region Constructors
        /// <summary>
        /// Creates a new instance of the CameraStreamer. It sets the video-source device and optional image-control
        /// </summary>
        /// <param name="deviceIndex">Index of the desired streaming device from the Devices list</param>
        /// <param name="image">Image-Control of the canvas on which the stream should be put on. This should only be used in a WPF application</param>
        public CameraStreamer(int deviceIndex = 0, Image image = null)
        {
            videoSource = GetVideoCaptureDevice(deviceIndex);
            ImageControl = image;
        }

        /// <summary>
        /// Creates a new instance of the CameraStreamer. It sets the video-source device and optional image-control
        /// </summary>
        /// <param name="camera">Device which should be used for streaming</param>
        /// <param name="image">Image-Control of the canvas on which the stream should be put on. This should only be used in a WPF application</param>
        public CameraStreamer(VideoCaptureDevice camera, Image image = null)
        {
            videoSource = camera;
            ImageControl = image;
        }

        /// <summary>
        /// Creates a new instance of the CameraStreamer. It sets the video-source device and optional image-control
        /// </summary>
        /// <param name="camera">Device which should be used for streaming</param>
        /// <param name="image">Image-Control of the canvas on which the stream should be put on</param>
        public CameraStreamer(DsDevice camera, Image image = null)
        {
            videoSource = GetVideoCaptureDevice(camera);
            ImageControl = image;
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Handles NewFrameEvents.
        /// </summary>
        private void NewFrame_Handler(object sender, AForge.Video.NewFrameEventArgs e)
        {
            using (e.Frame)
            using (Bitmap bmp = e.Frame)
            {
                if (ImageControl != null)
                {
                    //Directly write bitmap into a imagecontrol
                    ImageControl.Dispatcher.Invoke(() => ImageControl.Source = bmp.ToBitmapSource());
                }

                //Only raise event if something is subscribed to it
                OnNewFrame?.Invoke(this, new NewFrameEventArgs(bmp));
            }
        }

        /// <summary>
        /// Gets the maximum resolution of the camera and sets it as setting
        /// </summary>
        private void SetCameraSettings()
        {
            foreach (VideoCapabilities property in videoSource.VideoCapabilities)
            {
                if (videoSource.VideoResolution == null)
                {
                    videoSource.VideoResolution = property;
                }
                else if (property.FrameSize.Width > videoSource.VideoResolution.FrameSize.Width)
                {
                    videoSource.VideoResolution = property;
                }
            }
        }
        #endregion

        #region Public Methods
        #region Non-static Methods
        /// <summary>
        /// Starts recieving frames from the set video-capture device
        /// </summary>
        public void Start()
        {
            cancelSource = new CancellationTokenSource();

            streamTask = Task.Run(() =>
            {
                SetCameraSettings();

                videoSource.NewFrame += new AForge.Video.NewFrameEventHandler(NewFrame_Handler);

                videoSource.Start();
            }, cancelSource.Token);
        }

        /// <summary>
        /// Stops recieving frames and calling events. The video stream will stop
        /// </summary>
        public void Stop()
        {
            cancelSource?.Cancel();

            videoSource.NewFrame -= new AForge.Video.NewFrameEventHandler(NewFrame_Handler);
            videoSource.SignalToStop();
        }

        /// <summary>
        /// Stops streaming and releases any managed/unmanaged memory the class uses
        /// </summary>
        public void Dispose()
        {
            cancelSource?.Cancel();

            streamTask?.Dispose();

            if (videoSource.IsRunning)
            {
                Stop();
            }

            videoSource.NewFrame -= new AForge.Video.NewFrameEventHandler(NewFrame_Handler);
        }

        #region ChangeInputDevice Methods
        /// <summary>
        /// Changes the current input device to a new one. The stream will continue
        /// </summary>
        /// <param name="deviceIndex">Index of the new desired streaming device from the Devices list</param>
        public void ChangeInputDevice(int deviceIndex)
        {
            Stop();
            videoSource.NewFrame -= new AForge.Video.NewFrameEventHandler(NewFrame_Handler);
            videoSource = GetVideoCaptureDevice(deviceIndex);
            Start();
        }

        /// <summary>
        /// Changes the current input device to a new one. The stream will continue
        /// </summary>
        /// <param name="device">New device which should be used for streaming</param>
        public void ChangeInputDevice(DsDevice device)
        {
            Stop();
            videoSource = GetVideoCaptureDevice(device);
            Start();
        }

        /// <summary>
        /// Changes the current input device to a new one. The stream will continue
        /// </summary>
        /// <param name="device">New device which should be used for streaming</param>
        public void ChangeInputDevice(VideoCaptureDevice device)
        {
            Stop();
            videoSource.NewFrame -= new AForge.Video.NewFrameEventHandler(NewFrame_Handler);
            videoSource = device;
            Start();
        }
        #endregion

        #region MatchesWithVideoSource Methods
        /// <summary>
        /// Checks if the camera matches the currently used video-streaming device
        /// </summary>
        /// <param name="index">Index of the camera in the Devices list</param>
        /// <returns>Whether the camera matches or not</returns>
        public bool MatchesWithVideoSource(int index)
        {
            return Devices?.ElementAt(index)?.DevicePath == videoSource?.Source;
        }

        /// <summary>
        /// Checks if the camera matches the currently used video-streaming device
        /// </summary>
        /// <param name="device">Device</param>
        /// <returns>Whether the camera matches or not</returns>
        public bool MatchesWithVideoSource(DsDevice device)
        {
            return device?.DevicePath == videoSource?.Source;
        }

        /// <summary>
        /// Checks if the camera matches the currently used video-streaming device
        /// </summary>
        /// <param name="device">Device</param>
        /// <returns>Whether the camera matches or not</returns>
        public bool MatchesWithVideoSource(VideoCaptureDevice device)
        {
            return device?.Source == videoSource?.Source;
        }
        #endregion
        #endregion

        #region Static Methods
        #region GetVideoCaptureDevice Method
        /// <summary>
        /// Returns the VideoCaptureDevice corresponding the given index
        /// </summary>
        /// <param name="index">Index of the device</param>
        /// <returns>Newly generated VideoCaptureDevice</returns>
        public static VideoCaptureDevice GetVideoCaptureDevice(int index)
        {
            return new VideoCaptureDevice(Devices?.ElementAt(index)?.DevicePath);
        }

        /// <summary>
        /// Reutnrs the VideoCaptureDevice corresponding the given device
        /// </summary>
        /// <param name="device">Device</param>
        /// <returns>Newly generated VideoCaptureDevice</returns>
        public static VideoCaptureDevice GetVideoCaptureDevice(DsDevice device)
        {
            return new VideoCaptureDevice(device.DevicePath);
        }
        #endregion

        /// <summary>
        /// Reloads the Devices list
        /// </summary>
        /// <returns>Returns the Devices list</returns>
        public static IEnumerable<DsDevice> ReloadDevices()
        {
            return Devices = DsDevice.GetDevicesOfCat(DirectShowLib.FilterCategory.VideoInputDevice);
        }

        #region Rsize Methods
        /// <summary>
        /// Resizes a area given the size and ratio (optional) to the smaller size of the two lengths
        /// </summary>        
        /// <param name="size">Size of the area</param>
        /// <param name="widthRatio">Width of the ratio (default is 16)</param>
        /// <param name="heightRatio">Height of the ratio (default is 9)</param>
        /// <returns>Downscaled size</returns>
        public static Size ResizeDownScale(Size size, double widthRatio = 16, double heightRatio = 9)
        {
            double height = size.Height;
            double width = size.Width;

            if ((width / height) > (widthRatio / heightRatio))
            {
                width = widthRatio / heightRatio * height;
            }
            else if ((width / height) < (widthRatio / heightRatio))
            {
                height = heightRatio / widthRatio * width;
            }

            return new Size((int)width, (int)height);
        }

        /// <summary>
        /// Resizes a area given the size and ratio (optional) to the bigger size of the two lengths
        /// </summary>
        /// <param name="size">Size of the area</param>
        /// <param name="widthRatio">Width of the ratio (default is 16)</param>
        /// <param name="heightRatio">Height of the ratio (default is 9)</param>
        /// <returns>Upscaled size</returns>
        public static Size ResizeUpScale(Size size, double widthRatio = 16, double heightRatio = 9)
        {
            double height = size.Height;
            double width = size.Width;

            if ((width / height) > (widthRatio / heightRatio))
            {
                height = heightRatio / widthRatio * width;
            }
            else if ((width / height) < (widthRatio / heightRatio))
            {
                width = widthRatio / heightRatio * height;
            }

            return new Size((int)width, (int)height);
        }
        #endregion
        #endregion
        #endregion
    }
}
