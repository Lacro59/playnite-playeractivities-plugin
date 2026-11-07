using CommonPluginsShared;
using CommonPluginsShared.Images;
using Playnite.SDK;
using System;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace PlayerActivities.Services
{
    /// <summary>
    /// Opens a lightweight dialog sized to a screenshot's native dimensions (DIP), capped by the owner work area.
    /// </summary>
    public static class PaScreenshotViewer
    {
        private const double TitleBarChromeDip = 25d;
        private const double WorkAreaMarginDip = 40d;
        private const double MinContentDip = 64d;

        /// <summary>
        /// Shows <paramref name="filePath"/> in a non-persistent dialog fitted to the image size.
        /// </summary>
        /// <param name="filePath">Absolute path to an image file.</param>
        public static void TryShow(string filePath)
        {
            string fileName = string.IsNullOrEmpty(filePath) ? string.Empty : Path.GetFileName(filePath);

            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                LogManager.GetLogger().Warn($"[PaScreenshotViewer][open] skipped reason=missing file={fileName}");
                return;
            }

            ImageProperty props = ImageTools.GetImageProperty(filePath);
            if (props == null || props.Width <= 0 || props.Height <= 0)
            {
                LogManager.GetLogger().Warn($"[PaScreenshotViewer][open] skipped reason=invalidProps file={fileName}");
                return;
            }

            Window owner = API.Instance?.Dialogs?.GetCurrentAppWindow();
            Rect workArea = GetOwnerWorkAreaDip(owner);
            GetDpiScale(owner, out double scaleX, out double scaleY);

            double nativeWidthDip = props.Width * scaleX;
            double nativeHeightDip = props.Height * scaleY;

            double maxContentWidth = Math.Max(MinContentDip, workArea.Width - WorkAreaMarginDip);
            double maxContentHeight = Math.Max(MinContentDip, workArea.Height - TitleBarChromeDip - WorkAreaMarginDip);

            double fitScale = Math.Min(1d, Math.Min(maxContentWidth / nativeWidthDip, maxContentHeight / nativeHeightDip));
            double contentWidth = Math.Max(MinContentDip, nativeWidthDip * fitScale);
            double contentHeight = Math.Max(MinContentDip, nativeHeightDip * fitScale);

            UserControl view;
            try
            {
                view = CreateView(filePath, contentWidth, contentHeight);
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, $"[PaScreenshotViewer][open] loadFailed file={fileName}", false, "PlayerActivities");
                return;
            }

            WindowOptions windowOptions = new WindowOptions
            {
                ShowMinimizeButton = false,
                ShowMaximizeButton = false,
                ShowCloseButton = true,
                CanBeResizable = false,
                EnableWindowPersistence = false,
                Width = contentWidth,
                Height = contentHeight + TitleBarChromeDip,
                MaxWidth = nativeWidthDip,
                MaxHeight = nativeHeightDip + TitleBarChromeDip
            };

            string title = ResourceProvider.GetString("LOCPa") + " - " + filePath;
            Common.LogDebug(
                $"[PaScreenshotViewer][open] file={fileName} native={props.Width}x{props.Height}px " +
                $"window={contentWidth:0}x{contentHeight + TitleBarChromeDip:0}dip fitScale={fitScale:0.###}");

            Window window = PlayniteUiHelper.CreateExtensionWindow(title, view, windowOptions);
            _ = window.ShowDialog();
        }

        private static UserControl CreateView(string filePath, double width, double height)
        {
            BitmapImage bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.UriSource = new Uri(filePath, UriKind.Absolute);
            bitmap.EndInit();
            bitmap.Freeze();

            System.Windows.Controls.Image image = new System.Windows.Controls.Image
            {
                Source = bitmap,
                Stretch = Stretch.Uniform,
                SnapsToDevicePixels = true,
                Width = width,
                Height = height
            };

            return new UserControl
            {
                Width = width,
                Height = height,
                Content = image
            };
        }

        private static void GetDpiScale(Window owner, out double scaleX, out double scaleY)
        {
            scaleX = 1d;
            scaleY = 1d;

            if (owner == null)
            {
                return;
            }

            PresentationSource source = PresentationSource.FromVisual(owner);
            if (source?.CompositionTarget == null)
            {
                return;
            }

            Matrix fromDevice = source.CompositionTarget.TransformFromDevice;
            scaleX = fromDevice.M11;
            scaleY = fromDevice.M22;
        }

        private static Rect GetOwnerWorkAreaDip(Window owner)
        {
            Rect defaultWorkArea = SystemParameters.WorkArea;
            if (owner == null)
            {
                return defaultWorkArea;
            }

            IntPtr ownerHandle = new WindowInteropHelper(owner).Handle;
            if (ownerHandle == IntPtr.Zero)
            {
                return defaultWorkArea;
            }

            System.Windows.Forms.Screen ownerScreen = System.Windows.Forms.Screen.FromHandle(ownerHandle);
            Rectangle workingAreaPixels = ownerScreen.WorkingArea;

            PresentationSource ownerSource = PresentationSource.FromVisual(owner);
            if (ownerSource?.CompositionTarget == null)
            {
                return defaultWorkArea;
            }

            System.Windows.Point topLeftDip = ownerSource.CompositionTarget.TransformFromDevice.Transform(
                new System.Windows.Point(workingAreaPixels.Left, workingAreaPixels.Top));
            System.Windows.Point bottomRightDip = ownerSource.CompositionTarget.TransformFromDevice.Transform(
                new System.Windows.Point(workingAreaPixels.Right, workingAreaPixels.Bottom));

            return new Rect(topLeftDip, bottomRightDip);
        }
    }
}
