namespace HOG_Classificator.Infrastructure
{
	/// <summary>
	/// Extensions class
	/// </summary>
	public static class Extensions
	{
		/// <summary>
		/// To the byte.
		/// </summary>
		/// <param name="value">The value.</param>
		/// <returns>normalized float value</returns>
		public static float Normalize(this float value)
		{
			if (value > byte.MaxValue)
			{
				return byte.MaxValue;
			}

			if (value < byte.MinValue)
			{
				return byte.MinValue;
			}

			return (byte)value;
		}
	}
}