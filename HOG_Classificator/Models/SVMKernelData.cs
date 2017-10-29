using System;

namespace HOG_Classificator.Models
{
	/// <summary>
	/// SVMKernelData class
	/// </summary>
	[Serializable]
	public class SVMKernelData
	{
		/// <summary>
		/// The number of inputs
		/// </summary>
		public int NumberOfInputs { get; set; }

		/// <summary>
		/// Gets the number of outputs.
		/// </summary>
		public int NumberOfOutputs { get; set; }

		/// <summary>
		/// Gets the support vectors.
		/// </summary>
		public double[][] SupportVectors { get; set; }

		/// <summary>
		/// Gets the threshold.
		/// </summary>
		public double Threshold { get; set; }

		/// <summary>
		/// Gets the weights.
		/// </summary>
		public double[] Weights { get; set; }

		#region Constructors 

		/// <summary>
		/// Initializes a new instance of the <see cref="SVMKernelData"/> class.
		/// </summary>
		public SVMKernelData() { }

		#endregion

		#region Public methods

		/// <summary>
		/// Initializes the specified number of inputs.
		/// </summary>
		/// <param name="numOfInputs">The number of inputs.</param>
		/// <param name="numOfOutputs">The number of outputs.</param>
		/// <param name="supportVectors">The support vectors.</param>
		/// <param name="threshold">The threshold.</param>
		/// <param name="weights">The weights.</param>
		public void Initialize(int numOfInputs, int numOfOutputs, double[][] supportVectors, double threshold,
			double[] weights)
		{
			NumberOfInputs = numOfInputs;
			NumberOfOutputs = numOfOutputs;
			SupportVectors = supportVectors;
			Threshold = threshold;
			Weights = weights;
		}

		#endregion
	}
}