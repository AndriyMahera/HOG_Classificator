using System;

namespace HOG_Classificator.Models
{
	/// <summary>
	/// SVMGaussianData class
	/// </summary>
	/// <seealso cref="HOG_Classificator.Models.SVMKernelData" />
	[Serializable]
	public class SVMGaussianData : SVMKernelData
	{
		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="SVMGaussianData"/> class.
		/// </summary>
		public SVMGaussianData() { }

		#endregion

		#region Properties

		/// <summary>
		/// Gets or sets the gamma.
		/// </summary>
		public double Gamma { get; set; }

		/// <summary>
		/// Gets or sets the sigma.
		/// </summary>
		public double Sigma { get; set; }

		/// <summary>
		/// Gets or sets the sigma squared.
		/// </summary>
		public double SigmaSquared { get; set; }

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
		/// <param name="gamma">The gamma.</param>
		/// <param name="sigma">The sigma.</param>
		/// <param name="sigmaSquared">The sigma squared.</param>
		public void Initialize(int numOfInputs, int numOfOutputs, double[][] supportVectors, double threshold,
			double[] weights, double gamma, double sigma, double sigmaSquared)
		{
			base.Initialize(numOfInputs, numOfOutputs, supportVectors, threshold, weights);
			Gamma = gamma;
			Sigma = sigma;
			SigmaSquared = sigmaSquared;
		} 

		#endregion
	}
}