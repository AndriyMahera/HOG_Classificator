using System.Windows;
using System.Windows.Media.Imaging;

namespace HOG_Classificator.Helpers
{
	public class RecognizedObjects
	{
		/// <summary>
		/// Gets or sets the identifier.
		/// </summary>
		public int ID { get; set; }
		/// <summary>
		/// Gets or sets the image.
		/// </summary>
		public BitmapSource Image { get; set; }

		/// <summary>
		/// Gets or sets the frame.
		/// </summary>
		public Int32Rect Frame { get; set; }

		/// <summary>
		/// Gets the center.
		/// </summary>
		public Point Center => new Point(Frame.X + Frame.Width / 2, Frame.Y + Frame.Y / 2);

		/// <summary>
		/// Gets or sets the percentage.
		/// </summary>
		public double Percentage { get; set; }

		/// <summary>
		/// Gets or sets the name.
		/// </summary>
		public string Name { get; set; }
	}
}