using System;
using System.Drawing;

namespace speyck.CameraStreamer
{
    /// <summary>
    /// Arguments for OnNewFrame event from CameraStreamer
    /// </summary>
    public class NewFrameEventArgs : EventArgs, IDisposable
    {
        /// <summary>
        /// Bitmap holding the frame
        /// </summary>
        public readonly Bitmap Frame;

        /// <summary>
        /// Initializes a new instance of the NewFrameEventArgs class
        /// </summary>
        /// <param name="frame">Bitmap holding the frame</param>
        public NewFrameEventArgs(Bitmap frame)
        {
            Frame = frame;
        }

        public void Dispose()
        {
            Frame.Dispose();
        }
    }
}
