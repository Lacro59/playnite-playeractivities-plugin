using System;

namespace PlayerActivities.Models
{
    /// <summary>
    /// Lightweight screenshot row for PaView timeline strips (not persisted in PA DB).
    /// </summary>
    public sealed class PaScreenshotDisplay
    {
        /// <summary>
        /// Gets or sets the absolute image file path.
        /// </summary>
        public string FilePath { get; set; }

        /// <summary>
        /// Gets or sets the timestamp used for day filtering (aligned with SV <c>Modifed</c>).
        /// </summary>
        public DateTime DateTaken { get; set; }
    }
}
