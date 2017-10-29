namespace HOG_Classificator.Models
{
	/// <summary>
	/// ObjectOfRecognition class
	/// </summary>
	public class ObjectOfRecognition
	{
		/// <summary>
		/// Gets or sets the identifier.
		/// </summary>
		public long ID { get; set; }

		/// <summary>
		/// Gets or sets the hog.
		/// </summary>
		public double[] HOG { get; set; }

		/// <summary>
		/// Gets or sets the is human.
		/// </summary>
		public int IsHuman { get; set; }
	}
}