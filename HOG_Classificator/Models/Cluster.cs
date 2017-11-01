using System.Collections.Generic;
using System.Windows;
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
		public RecognizedObject Center { get; set; }
	}
}