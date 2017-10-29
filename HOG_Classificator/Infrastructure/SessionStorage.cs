using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace HOG_Classificator.Infrastructure
{
	public static class SessionStorage
	{
		/// <summary>
		/// Initializes the processor.
		/// </summary>
		/// <param name="sourceBitmap">The source bitmap.</param>
		/// <param name="frameThickness"></param>
		public static void InitializeStorage(BitmapSource sourceBitmap, int frameThickness)
		{
			BytesPerPixel = (sourceBitmap.Format.BitsPerPixel + 7) / 8;
			Stride = sourceBitmap.PixelWidth * BytesPerPixel;
			Height = sourceBitmap.PixelHeight;
			Width = sourceBitmap.PixelWidth;
			DpiX = sourceBitmap.DpiX;
			DpiY = sourceBitmap.DpiY;
			Format = sourceBitmap.Format;
			FrameThickness = frameThickness;
			NewStride = Stride + 2 * FrameThickness * BytesPerPixel;
			NewHeight = Height + 2 * FrameThickness;
			NewWidth = Width + 2 * FrameThickness;
		}

		/// <summary>
		/// Sets the grayscale coef.
		/// </summary>
		/// <param name="blackCoef">The black coef.</param>
		/// <param name="whiteCoef">The white coef.</param>
		public static void SetGrayscaleCoef(double blackCoef, double whiteCoef)
		{
			BlackPercentage = blackCoef / 100d;
			WhitePercentage = whiteCoef / 100d;
		}

		#region Properties

		/// <summary>
		///     Gets or sets the stride.
		/// </summary>
		public static int Stride { get; set; }

		/// <summary>
		///     Gets or sets the stride.
		/// </summary>
		public static int NewStride { get; set; }

		/// <summary>
		///     Gets or sets the bytes per pixel.
		/// </summary>
		public static int BytesPerPixel { get; set; }

		/// <summary>
		///     Gets the height.
		/// </summary>
		public static int Height { get; set; }

		/// <summary>
		///     Gets the height.
		/// </summary>
		public static int NewHeight { get; set; }

		/// <summary>
		///     Gets or sets the width.
		/// </summary>
		public static int Width { get; set; }

		/// <summary>
		///     Gets or sets the new width.
		/// </summary>
		public static int NewWidth { get; set; }

		/// <summary>
		///     Gets or sets the dpi x.
		/// </summary>
		public static double DpiX { get; set; }

		/// <summary>
		///     Gets or sets the dpi y.
		/// </summary>
		public static double DpiY { get; set; }

		/// <summary>
		///     Gets or sets the format.
		/// </summary>
		public static PixelFormat Format { get; set; }

		/// <summary>
		///     Gets or sets the frame thickness.
		/// </summary>
		public static int FrameThickness { get; set; }

		/// <summary>
		///     Gets or sets the black percentage.
		/// </summary>
		public static double BlackPercentage { get; set; }

		/// <summary>
		///     Gets or sets the white percentage.
		/// </summary>
		public static double WhitePercentage { get; set; }

		#endregion
	}
}