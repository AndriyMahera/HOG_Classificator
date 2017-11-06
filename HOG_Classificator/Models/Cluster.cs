using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Accord;
using HOG_Classificator.Helpers;

namespace HOG_Classificator.Models
{
	public class Cluster
	{
		/// <summary>
		/// Gets or sets the index.
		/// </summary>
		public int INDEX { get; set; }

		/// <summary>
		/// Gets or sets the items list.
		/// </summary>
		public IList<RecognizedObject> ItemsList { get; set; }

		/// <summary>
		/// Gets the center.
		/// </summary>
		public RecognizedObject CenterObject { get; set; }

		/// <summary>
		/// Gets the frame.
		/// </summary>
		public Int32Rect Frame
		{
			get
			{
				var meanWidth = ItemsList.Select(item => item.Frame.Width).Average();
				var meanHeight = ItemsList.Select(item => item.Frame.Height).Average();

				return new Int32Rect((int)(CenterObject.Center.X - meanWidth / 2), (int)(CenterObject.Center.Y - meanHeight / 2),
					(int)meanWidth, (int)meanHeight);
			}
		}
	}
}