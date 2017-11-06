using System.Windows.Markup;

namespace HOG_Classificator.Infrastructure
{
	/// <summary>
	/// Constants class
	/// </summary>
	public static class Constants
	{
		#region Convolution Kernels 

		/// <summary>
		/// The laplasian matrix
		/// </summary>
		public static readonly int[] LAPLASIAN_KERNEL = { -1, 0, -1, 0, 4, 0, -1, 0, -1 };

		/// <summary>
		/// The edge matrix
		/// </summary>
		public static readonly int[] EDGE_KERNEL = { 1, 1, 1, 0, 0, 0, -1, -1, -1 };

		/// <summary>
		/// The sharpening matrix2
		/// </summary>
		public static readonly int[] SHARPENING_KERNEL = { 0, -2, 0, -2, 11, -2, 0, -2, 0 };

		/// <summary>
		/// The sobel first kernel
		/// </summary>
		public static readonly int[] SOBEL_FIRST_KERNEL = { -1, -2, -1, 0, 0, 0, 1, 2, 1 };

		/// <summary>
		/// The sobel second kernel
		/// </summary>
		public static readonly int[] SOBEL_SECOND_KERNEL = { -1, 0, 1, -2, 0, 2, -1, 0, 1 };

		/// <summary>
		/// The gausian kernel
		/// </summary>
		public static readonly int[] GAUSIAN_KERNEL = { 1, 2, 1, 2, 4, 2, 1, 2, 1 };


		#endregion

		#region Standard offsets

		/// <summary>
		/// The laplassian offset
		/// </summary>
		public const int LAPLASSIAN_OFFSET = 127;

		/// <summary>
		/// The zero offset
		/// </summary>
		public const int ZERO_OFFSET = 0;

		#endregion

		/// <summary>
		/// The grayscale matrix
		/// </summary>
		public static readonly float[] GRAYSCALE_MATRIX = { 0.072f, 0.752f, 0.2126f, 0f };

		/// <summary>
		/// The width
		/// </summary>
		public const int WIDTH = 64;

		/// <summary>
		/// The height
		/// </summary>
		public const int HEIGHT = 128;

		/// <summary>
		/// The cell size
		/// </summary>
		public const int CELL_SIZE = 8;

		/// <summary>
		/// The number of bins
		/// </summary>
		public const int NUM_OF_BINS = 9;

		/// <summary>
		/// The maximum angle
		/// </summary>
		public const int MAX_ANGLE = 180;

		/// <summary>
		/// The cells in block
		/// </summary>
		public const int CELLS_IN_BLOCK = 4;

		/// <summary>
		/// The multiplier
		/// </summary>
		public const double MULTIPLIER = NUM_OF_BINS / (double)MAX_ANGLE;

		/// <summary>
		/// The human title
		/// </summary>
		public const string HUMAN_TITLE = "Human";

		/// <summary>
		/// The pre folder human
		/// </summary>
		public const string PRE_FOLDER_HUMAN = "Human Preprocessed";

		/// <summary>
		/// The pre folder background
		/// </summary>
		public const string PRE_FOLDER_BACKGROUND = "Background Preprocessed";

		/// <summary>
		/// The pre folder background
		/// </summary>
		public const string PRE_FOLDER_CROPPED = "Cropped";

		/// <summary> 
		/// The patern for cropping
		/// </summary>
		public const string PATERN_FOR_CROPPING = @"[(]\d{1,}\W{2}\d{1,}[)]\W{3}[(]\d{1,}\W{2}\d{1,}[)]";

		/// <summary>
		/// Database xml
		/// </summary>
		public const string DATABASE_PATH_HUMAN = "Database_human.xml";

		/// <summary>
		/// Database xml
		/// </summary>
		public const string DATABASE_PATH_BACKGROUND = "Database_background.xml";


		/// <summary>
		/// Minimum width of picture
		/// </summary>
		public const int MINIMUM_STRIDE = 256;

		/// <summary>
		/// Number of slices
		/// </summary>
		public const int NUM_OF_SLICES = 100;

		/// <summary>
		/// The path weight
		/// </summary>
		public const string PATH_WEIGHT = "Weight.txt";

		/// <summary>
		/// The path SVM
		/// </summary>
		public const string PATH_SVM = "SVM.xml";

		/// <summary>
		/// The percentage
		/// </summary>
		public const double ETALON_PERCENTAGE = 0.7;

		/// <summary>
		/// The output folder
		/// </summary>
		public const string OUTPUT_FOLDER = "OutPut";

		/// <summary>
		/// The tolerance
		/// </summary>
		public const int TOLERANCE = 64;
	}
}