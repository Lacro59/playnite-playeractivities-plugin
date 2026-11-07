using System;

namespace PlayerActivities.Models.ScreenshotsVisualizer
{
    /// <summary>
    /// Subset of ScreenshotsVisualizer screenshot JSON used for import and display reads.
    /// </summary>
    public class Screenshot
    {
        /// <summary>
        /// Gets or sets the absolute file path of the screenshot or video.
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// Gets or sets the file modification timestamp (typo preserved to match SV JSON).
        /// </summary>
        public DateTime Modifed { get; set; }
    }
}
