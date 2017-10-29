using System;

namespace HOG_Classificator.Infrastructure
{
	/// <summary>
	/// ProgressArgs class
	/// </summary>
	/// <seealso cref="System.EventArgs" />
	public class ProgressArgs : EventArgs
	{
		/// <summary>
		/// Gets or sets the percentage.
		/// </summary>
		public double Percentage { get; set; }

		/// <summary>
		/// Gets or sets the message.
		/// </summary>
		public string Message { get; set; }
	}
}