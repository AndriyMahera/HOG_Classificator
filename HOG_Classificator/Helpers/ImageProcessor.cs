using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using HOG_Classificator.Enums;
using HOG_Classificator.Infrastructure;

namespace HOG_Classificator.Helpers
{
	/// <summary>
	/// ImageProcessor class
	/// </summary>
	public class ImageProcessor
	{
		/// <summary>
		/// The report progress
		/// </summary>
		public EventHandler<ProgressArgs> ReportProgress;


		#region Public methods

		/// <summary>
		/// Preprocesses the image.
		/// </summary>
		/// <param name="image">The image.</param>
		/// <param name="filters">The filters.</param>
		/// <returns>preprocessed array</returns>
		/// <exception cref="ArgumentOutOfRangeException"></exception>
		public byte[] PreprocessImage(BitmapSource image, params Filter[] filters)
		{
			var array = image.ToByteArray();
			var grayscaledArray = ToGrayscale(array, Constants.GRAYSCALE_MATRIX);

			grayscaledArray= ContrastStretch(grayscaledArray);

			var framedArray = MakeFrame(grayscaledArray);

			foreach (var filter in filters)
			{
				switch (filter)
				{
					case Filter.Laplasian:
						framedArray = ApplyFilter(framedArray, Constants.LAPLASSIAN_OFFSET, Constants.LAPLASIAN_KERNEL);
						break;
					case Filter.Edge:
						framedArray = ApplyFilter(framedArray, Constants.LAPLASSIAN_OFFSET, Constants.EDGE_KERNEL);
						break;
					case Filter.Sharpening:
						framedArray = ApplyFilter(framedArray, Constants.ZERO_OFFSET, Constants.SHARPENING_KERNEL);
						break;
					case Filter.Gaussian:
						framedArray = ApplyFilter(framedArray, Constants.ZERO_OFFSET, Constants.GAUSIAN_KERNEL);
						break;
					case Filter.Sobel:
						framedArray = ApplyFilter(framedArray, Constants.ZERO_OFFSET, Constants.SOBEL_FIRST_KERNEL, Constants.SOBEL_SECOND_KERNEL);
						break;
					case Filter.LinearContrasting:
						break;
					default:
						throw new ArgumentOutOfRangeException();
				}
			}
			var unFramedArray = RemoveFrame(framedArray);

			return unFramedArray;
			
		}

		/// <summary>
		/// Resizes the image.
		/// </summary>
		/// <param name="bitmapSource">The bitmap source.</param>
		/// <returns></returns>
		public BitmapSource ResizeImage(BitmapSource bitmapSource) =>
			new TransformedBitmap(bitmapSource,
				new ScaleTransform(
					Constants.WIDTH / (double)bitmapSource.PixelWidth,
					Constants.HEIGHT / (double)bitmapSource.PixelHeight));

		/// <summary>
		/// Converts the image.
		/// </summary>
		/// <param name="pathIn">The path in.</param>
		/// <param name="pathOut">The path out.</param>
		public void ConvertImages(string pathIn, string pathOut)
		{
			int counter = 0;
			var fileList = Directory.GetFiles(@pathIn);
			int count = fileList.Length;
			var multiplier = 100d / count;

			Directory.CreateDirectory(Constants.PRE_FOLDER_BACKGROUND);
			Directory.CreateDirectory(Constants.PRE_FOLDER_HUMAN);

			foreach (string fileName in fileList)
			{
				BitmapSource image = new BitmapImage(new Uri(fileName, UriKind.RelativeOrAbsolute));

				var preprocessedArray = PreprocessImage(image, Filter.Sobel);
				var preprocessedImage = AuxiliaryMethods.BitmapSourceFromArray(preprocessedArray, false);
				var resizedImage = ResizeImage(preprocessedImage);

				resizedImage.SaveImage($"{pathOut}\\{counter}.png");

				double percentage = counter * multiplier;

				counter++;

				ReportProgress?.Invoke(this, new ProgressArgs { Percentage = percentage, Message = string.Empty });
			}
		}

		/// <summary>
		/// Cuts the images on slices.
		/// </summary>
		/// <param name="pathIn">The path in.</param>
		/// <param name="pathOut">The path out.</param>
		public void CutImagesOnSlices(string pathIn, string pathOut)
		{
			int counter = 0;

			foreach (string fileName in Directory.GetFiles(@pathIn))
			{
				var image = new BitmapImage(new Uri(fileName, UriKind.RelativeOrAbsolute));

				for (int i = 0; i < image.Height - Constants.HEIGHT; i += Constants.HEIGHT)
				{
					for (int j = 0; j < image.Width - Constants.WIDTH; j += Constants.WIDTH)
					{
						try
						{
							var pieceOfImage = new CroppedBitmap(image, new Int32Rect(j, i, Constants.WIDTH, Constants.HEIGHT));
							pieceOfImage.SaveImage($"{pathOut}\\{counter}.png");
							counter++;
						}
						catch (Exception)
						{
							// ignored
						}
					}
				}
			}
		}

		/// <summary>
		/// Crops the images.
		/// </summary>
		/// <param name="pathAnnotationsIn">The path annotations in.</param>
		/// <param name="pathImagesIn">The path images in.</param>
		/// <param name="pathOut">The path out.</param>
		/// <exception cref="ArgumentException">
		/// Amount of annotation not equal to amount of images.Check train folder
		/// or
		/// There is no coordinates in annotation file
		/// </exception>
		public void CropImages(string pathAnnotationsIn, string pathImagesIn, string pathOut)
		{
			var annotations = Directory.GetFiles(pathAnnotationsIn);
			var images = Directory.GetFiles(pathImagesIn);

			if (annotations.Length != images.Length)
			{
				throw new ArgumentException("Amount of annotation not equal to amount of images.Check train folder");
			}

			var coords = new List<int>();
			Directory.CreateDirectory(Constants.PRE_FOLDER_CROPPED);
			int counter = 0;

			for (int i = 0; i < annotations.Length; i++)
			{
				var annotationContent = File.ReadAllText(annotations[i]);
				var image = new BitmapImage(new Uri(images[i], UriKind.RelativeOrAbsolute));

				Regex regex = new Regex(Constants.PATERN_FOR_CROPPING);

				foreach (Match match in regex.Matches(annotationContent))
				{
					coords.Clear();
					var stringWithValues = match.Value;
					Regex regexForDigit = new Regex(@"\d+");

					foreach (Match digitMatch in regexForDigit.Matches(stringWithValues))
					{
						coords.Add(int.Parse(digitMatch.Value));
					}

					if (coords.Count < 4)
					{
						throw new ArgumentException("There is no coordinates in annotation file");
					}

					var frame = new Int32Rect(coords[0], coords[1], coords[2] - coords[0], coords[3] - coords[1]);
					var croppedImage = CropImage(image, frame);

					croppedImage.SaveImage($"{pathOut}\\{counter}.png");
					counter++;

					var reflectedImage = new TransformedBitmap(croppedImage, new ScaleTransform { ScaleX = -1 });
					reflectedImage.SaveImage($"{pathOut}\\{counter}.png");
					counter++;
				}
			}
		}

		/// <summary>
		/// Crops the image.
		/// </summary>
		/// <param name="bitmapSource">The bitmap source.</param>
		/// <param name="rectangle">The rectangle.</param>
		/// <returns></returns>
		public BitmapSource CropImage(BitmapSource bitmapSource, Int32Rect rectangle) => new CroppedBitmap(bitmapSource, rectangle);

		/// <summary>
		/// Gets the iteration count.
		/// </summary>
		/// <param name="image">The image.</param>
		/// <param name="step">The step.</param>
		/// <returns></returns>
		public int GetIterationCount(BitmapSource image, int step)
		{
			int count = 0;
			int width = Constants.WIDTH;
			int height = Constants.HEIGHT;

			while (width < image.Width && height < image.Height)
			{
				for (int i = 0; i < image.Height - height; i += step)
				{
					for (int j = 0; j < image.Width - width; j += step)
					{
						count++;
					}
				}

				width = (int)Math.Round(width * 1.5);
				height = (int)Math.Round(height * 1.5);
			}

			return count;
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Applies the filter.
		/// </summary>
		/// <param name="inputArray">The input array.</param>
		/// <param name="offset">The offset.</param>
		/// <param name="kernelX">The kernelX.</param>
		/// <returns>filtered byte array</returns>
		/// <exception cref="ArgumentException">Your kernelX matrix is not squared</exception>
		private byte[] ApplyFilter(byte[] inputArray, int offset, int[] kernelX)
		{
			//handling possible exception
			var sqrtResult = Math.Sqrt(kernelX.Length);

			// ReSharper disable once CompareOfFloatsByEqualityOperator
			int root = sqrtResult % 1 == 0
					   && (int)sqrtResult % 2 != 0
					   && (int)((sqrtResult - 1) / 2) == SessionStorage.FrameThickness ? (int)sqrtResult : -1;

			if (root == -1)
			{
				throw new ArgumentException("Your kernelX matrix is not squared");
			}

			//Arrangements
			var outputArray = new byte[inputArray.Length];
			var filterOffset = (int)(sqrtResult - 1) / 2;
			var neighbourPixels = new List<int>();
			int div = 1;
			div = Math.Max(div, kernelX.Sum());
			float invertedDiv = 1f / div;

			//Expand kernel matrix(to cover all channels)
			var newKernel = kernelX.Select(item => Enumerable.Repeat(item, SessionStorage.BytesPerPixel).ToArray()).ToArray();

			for (int j = filterOffset; j < SessionStorage.NewHeight - filterOffset; j++)
			{
				for (int i = filterOffset; i < SessionStorage.NewWidth - filterOffset; i++)
				{
					var byteOffset = j * SessionStorage.NewStride + i * SessionStorage.BytesPerPixel;
					neighbourPixels.Clear();

					for (int filterY = -filterOffset; filterY <= filterOffset; filterY++)
					{
						for (int filterX = -filterOffset; filterX <= filterOffset; filterX++)
						{
							var calcOffset = byteOffset + filterX * SessionStorage.BytesPerPixel + filterY * SessionStorage.NewStride;
							neighbourPixels.Add(BitConverter.ToInt32(inputArray, calcOffset));
						}
					}

					var values = neighbourPixels.Select(BitConverter.GetBytes).ToArray();

					for (int k = 0; k < SessionStorage.BytesPerPixel; k++)
					{
						var result = values.Select((item, index) => item[k] * newKernel[index][k]).Sum() * invertedDiv + offset;

						result = result.Normalize();

						outputArray[byteOffset + k] = (byte)result;

					}
				}
			}

			return outputArray;
		}

		/// <summary>
		/// Applies the filter.
		/// </summary>
		/// <param name="inputArray">The input array.</param>
		/// <param name="offset">The offset.</param>
		/// <param name="kernelX">The kernelX.</param>
		/// <param name="kernelY"></param>
		/// <returns>filtered byte array</returns>
		/// <exception cref="ArgumentException">Your kernelX matrix is not squared</exception>
		private byte[] ApplyFilter(byte[] inputArray, int offset, int[] kernelX, int[] kernelY)
		{
			//handling possible exception
			if (kernelX.Length != kernelY.Length)
			{
				throw new ArgumentException("Your kernels aren't equal");
			}

			var sqrtResult = Math.Sqrt(kernelX.Length);

			// ReSharper disable once CompareOfFloatsByEqualityOperator
			int root = sqrtResult % 1 == 0
					   && (int)sqrtResult % 2 != 0
					   && (int)((sqrtResult - 1) / 2) == SessionStorage.FrameThickness ? (int)sqrtResult : -1;

			if (root == -1)
			{
				throw new ArgumentException("Your kernelX matrix is not squared");
			}

			//Arrangements
			var outputArray = new byte[inputArray.Length];
			var filterOffset = (int)(sqrtResult - 1) / 2;
			var neighbourPixels = new List<int>();
			int divX = 1, divY = 1;
			divX = Math.Max(divX, kernelX.Sum());
			divY = Math.Max(divY, kernelY.Sum());

			float invertedDivX = 1f / divX;
			float invertedDivY = 1f / divY;

			//Expand kernel matrix(to cover all channels)
			var newKernelX = kernelX.Select(item => Enumerable.Repeat(item, SessionStorage.BytesPerPixel).ToArray()).ToArray();
			var newKernelY = kernelY.Select(item => Enumerable.Repeat(item, SessionStorage.BytesPerPixel).ToArray()).ToArray();

			for (int j = filterOffset; j < SessionStorage.NewHeight - filterOffset; j++)
			{
				for (int i = filterOffset; i < SessionStorage.NewWidth - filterOffset; i++)
				{
					var byteOffset = j * SessionStorage.NewStride + i * SessionStorage.BytesPerPixel;
					neighbourPixels.Clear();

					for (int filterY = -filterOffset; filterY <= filterOffset; filterY++)
					{
						for (int filterX = -filterOffset; filterX <= filterOffset; filterX++)
						{
							var calcOffset = byteOffset + filterX * SessionStorage.BytesPerPixel + filterY * SessionStorage.NewStride;
							neighbourPixels.Add(BitConverter.ToInt32(inputArray, calcOffset));
						}
					}

					var values = neighbourPixels.Select(BitConverter.GetBytes).ToArray();

					for (int k = 0; k < SessionStorage.BytesPerPixel; k++)
					{
						var resultX = values.Select((item, index) => item[k] * newKernelX[index][k]).Sum() * invertedDivX + offset;
						var resultY = values.Select((item, index) => item[k] * newKernelY[index][k]).Sum() * invertedDivY + offset;

						resultX = resultX.Normalize();
						resultY = resultY.Normalize();

						var finalValue = (float)Math.Sqrt(resultX * resultX + resultY * resultY);

						finalValue = finalValue.Normalize();
						outputArray[byteOffset + k] = (byte)finalValue;
					}
				}
			}

			return outputArray;
		}

		/// <summary>
		/// Makes the frame.
		/// </summary>
		/// <param name="inputArray">The input array.</param>
		/// <returns>framed byte array</returns>
		private byte[] MakeFrame(byte[] inputArray)
		{
			byte[] framedArray = new byte[SessionStorage.NewStride * SessionStorage.NewHeight];

			//Center
			for (int i = 0; i < SessionStorage.Height; i++)
			{
				Buffer.BlockCopy(
					inputArray,
					SessionStorage.Stride * i,
					framedArray,
					(i + SessionStorage.FrameThickness) * SessionStorage.NewStride + SessionStorage.BytesPerPixel * SessionStorage.FrameThickness,
					SessionStorage.Stride);
			}

			//Horizontal frames
			for (int i = 0; i < SessionStorage.FrameThickness; i++)
			{
				Buffer.BlockCopy(
					framedArray,
					SessionStorage.NewStride * (SessionStorage.FrameThickness - i),
					framedArray,
					SessionStorage.NewStride * (SessionStorage.FrameThickness - (i + 1)),
					SessionStorage.NewStride);

				Buffer.BlockCopy(
					framedArray,
					SessionStorage.NewStride * (SessionStorage.NewHeight - (SessionStorage.FrameThickness - i + 1)),
					framedArray,
					SessionStorage.NewStride * (SessionStorage.NewHeight - (SessionStorage.FrameThickness - i)),
					SessionStorage.NewStride);
			}

			//Vertical frames
			//ToDo to achieve better result need to make another cycle
			for (int i = 0; i < SessionStorage.NewHeight; i++)
			{
				Buffer.BlockCopy(
					framedArray,
					SessionStorage.FrameThickness * SessionStorage.BytesPerPixel + SessionStorage.NewStride * i,
					framedArray,
					i * SessionStorage.NewStride,
					SessionStorage.BytesPerPixel * SessionStorage.FrameThickness);

				Buffer.BlockCopy(
					framedArray,
					SessionStorage.NewStride * (i + 1) - 2 * SessionStorage.FrameThickness * SessionStorage.BytesPerPixel,
					framedArray,
					SessionStorage.NewStride * (i + 1) - SessionStorage.FrameThickness * SessionStorage.BytesPerPixel,
					SessionStorage.BytesPerPixel * SessionStorage.FrameThickness);
			}

			return framedArray;
		}

		/// <summary>
		/// Removes the frame.
		/// </summary>
		/// <param name="inputArray">The input array.</param>
		/// <returns>unframed byte array</returns>
		private byte[] RemoveFrame(byte[] inputArray)
		{
			byte[] outputArray = new byte[SessionStorage.Stride * SessionStorage.Height];

			for (int i = 0; i < SessionStorage.Height; i++)
			{
				Buffer.BlockCopy(
					inputArray,
					(i + SessionStorage.FrameThickness) * SessionStorage.NewStride + SessionStorage.BytesPerPixel * SessionStorage.FrameThickness,
					outputArray,
					i * SessionStorage.Stride,
					SessionStorage.Stride);
			}

			return outputArray;
		}

		/// <summary>
		/// To the grayscale.
		/// </summary>
		/// <param name="inputArray">The input array.</param>
		/// <param name="grayscaleMask">The grayscale mask.</param>
		/// <returns>grayscaled array</returns>
		private byte[] ToGrayscale(byte[] inputArray, float[] grayscaleMask)
		{
			var outputArray = new byte[inputArray.Length];

			for (int j = 0; j < inputArray.Length; j += SessionStorage.BytesPerPixel)
			{
				var result = BitConverter.GetBytes(BitConverter.ToInt32(inputArray, j));

				var value = result.Select((item, index) => item * grayscaleMask[index]).Sum();

				value = value.Normalize();

				for (int k = 0; k < SessionStorage.BytesPerPixel - 1; k++)
				{
					outputArray[j + k] = (byte)value;
				}

				outputArray[j + SessionStorage.BytesPerPixel - 1] = inputArray[j + SessionStorage.BytesPerPixel - 1];
			}

			return outputArray;
		}

		/// <summary>
		/// Contrasts the stretch.
		/// </summary>
		/// <param name="inputArray">The input array.</param>
		/// <returns>stretched image</returns>
		private byte[] ContrastStretch(byte[] inputArray)
		{
			var outputArray = new byte[inputArray.Length];
			var freq = new int[byte.MaxValue + 1];

			for (int j = 0; j < SessionStorage.Height; j++)
			{
				for (int i = 0; i < SessionStorage.Width; i++)
				{
					++freq[inputArray[j * SessionStorage.Stride + i * SessionStorage.BytesPerPixel]];
				}
			}

			int numPixels = SessionStorage.Width * SessionStorage.Height;

			var blackPixels = numPixels * SessionStorage.BlackPercentage;
			int accum = 0;

			int minI = byte.MinValue;

			while (minI < byte.MaxValue)
			{
				accum += freq[minI];
				if (accum > blackPixels)
				{
					break;
				}

				minI++;
			}

			int maxI = byte.MaxValue;
			var whitePixels = numPixels * SessionStorage.WhitePercentage;
			accum = 0;

			while (maxI > byte.MinValue)
			{
				accum += freq[maxI];
				if (accum > whitePixels)
				{
					break;
				}

				maxI--;
			}

			double spread = 255d / (maxI - minI);

			for (int j = 0; j < SessionStorage.Height; j++)
			{
				for (int i = 0; i < SessionStorage.Width; i++)
				{
					var byteOffset = j * SessionStorage.Stride + i * SessionStorage.BytesPerPixel;
					var value = (float)Math.Round((inputArray[byteOffset] - minI) * spread);
					value = value.Normalize();

					for (int k = 0; k < SessionStorage.BytesPerPixel - 1; k++)
					{
						outputArray[byteOffset + k] = (byte)value;
					}

					outputArray[byteOffset + SessionStorage.BytesPerPixel - 1] = inputArray[byteOffset + SessionStorage.BytesPerPixel - 1];
				}
			}

			return outputArray;
		}

		#endregion
	}
}