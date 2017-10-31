using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using Accord.Imaging;
using Accord.MachineLearning.VectorMachines;
using Accord.MachineLearning.VectorMachines.Learning;
using Accord.Statistics.Kernels;
using HOG_Classificator.Enums;
using HOG_Classificator.Infrastructure;
using HOG_Classificator.Models;

namespace HOG_Classificator.Helpers
{
	/// <summary>
	/// HogProcessor class
	/// </summary>
	public class HogProcessor
	{
		#region Private fields

		/// <summary>
		/// The report progress
		/// </summary>
		public EventHandler<ProgressArgs> ReportProgress;

		/// <summary>
		/// The image processor
		/// </summary>
		private readonly ImageProcessor _imageProcessor;

		/// <summary>
		/// The SVM
		/// </summary>
		private readonly SupportVectorMachine<Gaussian> _svm;

		#endregion

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="HogProcessor"/> class.
		/// </summary>
		public HogProcessor()
		{
			if (File.Exists(Constants.PATH_SVM))
			{
				_svm = AuxiliaryMethods.DeserializeSVM(Constants.PATH_SVM);
			}

			_imageProcessor = new ImageProcessor();
		}

		#endregion

		/// <summary>
		/// Extracts the hog.
		/// </summary>
		/// <param name="inputArray">The input array.</param>
		public IList<double> ExtractHog(byte[] inputArray)
		{
			var cells = SliceOnCells(inputArray);
			var histograms = new double[cells.Length][];

			for (int i = 0; i < cells.Length; i++)
			{
				var hogContainer = CalculateGradients(cells[i]);
				histograms[i] = FormHistogram(hogContainer);
			}

			var normalizedVector = NormalizeBlocks(histograms);

			return normalizedVector;
		}
		public IList<double> ExtractHogAccord(Bitmap image)
		{
			var hog = new HistogramsOfOrientedGradients();
			hog.ProcessImage(image);

			var lined = hog.Histograms;

			return ToOneLine(lined);

		}

		public IList<double> ToOneLine(double[,][] hog)
		{
			List<double> list = new List<double>();
			for (int i = 0; i < hog.GetLength(0); i++)
			{
				for (int j = 0; j < hog.GetLength(1); j++)
				{
					list.AddRange(hog[i, j].Select(x => x));
				}
			}
			return list;
		}

		/// <summary>
		/// Slices the on cells.
		/// </summary>
		/// <param name="inputArray">The input array.</param>
		/// <returns>sliced array</returns>
		private byte[][] SliceOnCells(byte[] inputArray)
		{
			var position = 0;
			var byteLength = Constants.CELL_SIZE * SessionStorage.BytesPerPixel;
			var cellSize = Constants.CELL_SIZE * byteLength;

			byte[][] outputArray = new byte[Constants.WIDTH * Constants.HEIGHT / Constants.CELL_SIZE / Constants.CELL_SIZE][];

			for (int j = 0; j < SessionStorage.Height; j += Constants.CELL_SIZE)
			{
				for (int i = 0; i < SessionStorage.Width; i += Constants.CELL_SIZE)
				{
					var byteOffset = j * SessionStorage.Width + i * SessionStorage.BytesPerPixel;

					outputArray[position] = new byte[cellSize];

					for (int k = 0; k < Constants.CELL_SIZE; k++)
					{
						Buffer.BlockCopy(inputArray, byteOffset + k * SessionStorage.Stride, outputArray[position], k * byteLength,
							byteLength);
					}

					position++;
				}
			}

			return outputArray;
		}

		/// <summary>
		/// Calculates the gradients for cell
		/// </summary>
		/// <param name="inputArray">The input array.</param>
		/// <returns></returns>
		private HogContainer CalculateGradients(byte[] inputArray)
		{
			byte[] targetArray = new byte[inputArray.Length];
			Buffer.BlockCopy(inputArray, 0, targetArray, 0, inputArray.Length);

			var magnitudes = new int[Constants.CELL_SIZE * Constants.CELL_SIZE];
			var angles = new int[Constants.CELL_SIZE * Constants.CELL_SIZE];
			var size = Constants.CELL_SIZE + 2 * SessionStorage.FrameThickness;
			targetArray = MakeCellFrame(inputArray, 1);
			targetArray = GetChannel(targetArray).ToArray();

			var filterOffset = 1;
			var position = 0;

			for (int j = filterOffset; j < size - 1; j++)
			{
				for (int i = filterOffset; i < size - 1; i++)
				{
					var offset = j * size + i;

					var horizontalDiff = targetArray[offset + 1] - targetArray[offset - 1];
					var verticalDiff = targetArray[offset - size] - targetArray[offset + size];

					magnitudes[position] = (int)Math.Sqrt(horizontalDiff * horizontalDiff + verticalDiff * verticalDiff);

					var radian = Math.Atan2(verticalDiff, horizontalDiff);
					radian = radian >= 0 ? radian : radian + 2 * Math.PI;

					var degree = (int)(radian * Constants.MAX_ANGLE / Math.PI);

					angles[position] = degree > Constants.MAX_ANGLE ? degree - Constants.MAX_ANGLE : degree;

					position++;
				}
			}

			return new HogContainer
			{
				Magnitude = magnitudes,
				Angle = angles
			};
		}

		/// <summary>
		/// Gets the channel.
		/// </summary>
		/// <param name="inputArray">The input array.</param>
		/// <param name="channel">The channel.</param>
		/// <returns></returns>
		private IEnumerable<byte> GetChannel(byte[] inputArray, int channel = 0)
		{
			for (int i = channel; i < inputArray.Length; i += SessionStorage.BytesPerPixel)
			{
				yield return inputArray[i];
			}
		}

		/// <summary>
		/// Forms the histogram.
		/// </summary>
		/// <param name="hogContainer">The hog container.</param>
		/// <returns>histogram for one cell</returns>
		private double[] FormHistogram(HogContainer hogContainer)
		{
			double[] output = new double[Constants.NUM_OF_BINS];

			for (int i = 0; i < hogContainer.Magnitude.Length; i++)
			{
				var doubleResult = hogContainer.Angle[i] * Constants.MULTIPLIER;
				doubleResult = doubleResult == Constants.NUM_OF_BINS ? doubleResult - 1 : doubleResult;

				var intResult = (int)doubleResult;
				var diff = doubleResult - intResult;
				var next = intResult == Constants.NUM_OF_BINS - 1 ? 0 : intResult + 1;

				output[next] = diff * hogContainer.Magnitude[i];
				output[intResult] = (1 - diff) * hogContainer.Magnitude[i];
			}

			return output;
		}

		/// <summary>
		/// Normalizes the blocks.
		/// </summary>
		/// <param name="inputArray">The input array.</param>
		private IList<double> NormalizeBlocks(double[][] inputArray)
		{
			int width = Constants.WIDTH / Constants.CELL_SIZE;
			int height = Constants.HEIGHT / Constants.CELL_SIZE;
			int halfCell = (int)Math.Sqrt(Constants.CELLS_IN_BLOCK);
			var unitedList = new List<double>();
			var finalList = new List<double>();

			for (int j = 0; j < height - 1; j++)
			{
				for (int i = 0; i < width - 1; i++)
				{
					var offset = j * width + i;
					unitedList.Clear();

					//form block from cells
					for (int m = 0; m < halfCell; m++)
					{
						for (int n = 0; n < halfCell; n++)
						{
							var calcOffset = offset + n + m * width;
							unitedList.AddRange(inputArray[calcOffset]);
						}
					}

					var norma = Math.Sqrt(unitedList.Select(item => item * item).Sum());
					var divider = norma == 0 ? 1 : 1d / norma;

					finalList.AddRange(unitedList.Select(item => item * divider));
				}
			}

			return finalList;

		}

		/// <summary>
		/// Makes the cell frame.
		/// </summary>
		/// <param name="inputArray">The input array.</param>
		/// <param name="frameThickness">The frame thickness.</param>
		/// <returns></returns>
		private byte[] MakeCellFrame(byte[] inputArray, int frameThickness)
		{
			var stride = Constants.CELL_SIZE * SessionStorage.BytesPerPixel;
			var newStride = stride + 2 * frameThickness * SessionStorage.BytesPerPixel;
			var newHeight = Constants.CELL_SIZE + 2 * frameThickness;

			byte[] framedArray = new byte[newStride * newHeight];

			//CalculatedCenter
			for (int i = 0; i < Constants.CELL_SIZE; i++)
			{
				Buffer.BlockCopy(
					inputArray,
					stride * i,
					framedArray,
					(i + frameThickness) * newStride + SessionStorage.BytesPerPixel * frameThickness,
					stride);
			}

			//Horizontal frames
			for (int i = 0; i < frameThickness; i++)
			{
				Buffer.BlockCopy(
					framedArray,
					newStride * (frameThickness - i),
					framedArray,
					newStride * (frameThickness - (i + 1)),
					newStride);

				Buffer.BlockCopy(
					framedArray,
					newStride * (newHeight - (frameThickness - i + 1)),
					framedArray,
					newStride * (newHeight - (frameThickness - i)),
					newStride);
			}

			//Vertical frames
			//ToDo to achieve better result need to make another cycle
			for (int i = 0; i < newHeight; i++)
			{
				Buffer.BlockCopy(
					framedArray,
					frameThickness * SessionStorage.BytesPerPixel + newStride * i,
					framedArray,
					i * newStride,
					SessionStorage.BytesPerPixel * frameThickness);

				Buffer.BlockCopy(
					framedArray,
					newStride * (i + 1) - 2 * frameThickness * SessionStorage.BytesPerPixel,
					framedArray,
					newStride * (i + 1) - frameThickness * SessionStorage.BytesPerPixel,
					SessionStorage.BytesPerPixel * frameThickness);
			}

			return framedArray;
		}

		/// <summary>
		/// Adds the training samples to database.
		/// </summary>
		/// <param name="pathIn">The path in.</param>
		/// <param name="pathOut">The path out.</param>
		public void AddTrainingSamplesToDatabase(string pathIn, string pathOut)
		{
			var samplesHuman = new List<ObjectOfRecognition>();
			var samplesBackGround = new List<ObjectOfRecognition>();

			int isHuman = pathIn.Contains(Constants.HUMAN_TITLE) ? 1 : 0;

			long counter = 0;
			var fileList = Directory.GetFiles(pathIn);
			int count = fileList.Length;


			foreach (string filename in fileList)
			{
				if (!filename.Contains(".png") && !filename.Contains(".jpg"))
				{
					continue;
				}

				var image = new BitmapImage(new Uri(filename, UriKind.RelativeOrAbsolute));
				var bimapImage = new Bitmap(filename);

				var byteArray = image.ToByteArray();
				//var hogVector = ExtractHog(byteArray);
				var hogVector = ExtractHogAccord(bimapImage);

				var objOfRec = new ObjectOfRecognition
				{
					ID = counter,
					IsHuman = isHuman,
					HOG = hogVector.ToArray()
				};

				if (pathIn.Contains(Constants.HUMAN_TITLE))
				{
					samplesHuman.Add(objOfRec);
				}
				else
				{
					samplesBackGround.Add(objOfRec);
				}

				double percentage = counter * 100d / count;

				counter++;

				ReportProgress?.Invoke(this, new ProgressArgs { Percentage = percentage, Message = string.Empty });
			}

			AuxiliaryMethods.Serialize(pathIn.Contains(Constants.HUMAN_TITLE) ? samplesHuman : samplesBackGround, pathOut);
		}

		/// <summary>
		/// Trains the specified true path.
		/// </summary>
		/// <param name="truePath">The true path.</param>
		/// <param name="falsePath">The false path.</param>
		public void Train(string truePath, string falsePath)
		{
			var samples = new List<ObjectOfRecognition>();
			samples.AddRange(AuxiliaryMethods.Deserialize<List<ObjectOfRecognition>>(truePath));
			samples.AddRange(AuxiliaryMethods.Deserialize<List<ObjectOfRecognition>>(falsePath));

			var trainArray = samples.Select(item => item.HOG).ToArray();
			var outputArray = samples.Select(item => item.IsHuman).ToArray();

			var teacher = new SequentialMinimalOptimization<Gaussian>
			{
				UseComplexityHeuristic = true,
				UseKernelEstimation = true
			};

			SupportVectorMachine<Gaussian> svm = teacher.Learn(trainArray, outputArray);

			var resultLine = svm.Weights;
			AuxiliaryMethods.WriteWeight(resultLine, Constants.PATH_WEIGHT);
			AuxiliaryMethods.SerializeSVM(svm, Constants.PATH_SVM);
		}

		Bitmap GetBitmap(BitmapSource source)
		{
			Bitmap bmp = new Bitmap(
				source.PixelWidth,
				source.PixelHeight,
				PixelFormat.Format32bppPArgb);
			BitmapData data = bmp.LockBits(
				new Rectangle(System.Drawing.Point.Empty, bmp.Size),
				ImageLockMode.WriteOnly,
				PixelFormat.Format32bppPArgb);
			source.CopyPixels(
				Int32Rect.Empty,
				data.Scan0,
				data.Height * data.Stride,
				data.Stride);
			bmp.UnlockBits(data);
			return bmp;
		}

		/// <summary>
		/// Called when [pass of window].
		/// </summary>
		/// <param name="src">The source.</param>//
		/// <param name="width">The width.</param>
		/// <param name="height">The height.</param>
		/// <param name="step">The step.</param>
		private Task<List<RecognizedObjects>> OnePassOfWindow(BitmapSource src, int width, int height, int step)
		{
			var recognizedList = new List<RecognizedObjects>();
			int counter = 0;

			return Task.Run(() =>
				{
					for (int i = 0; i < src.Height - height; i += 2 * step)
					{
						for (int j = 0; j < src.Width - width; j += step)
						{
							var frame = new Int32Rect(j, i, width, height);
							var croppedImage = _imageProcessor.CropImage(src, frame);
							var resizedImage = _imageProcessor.ResizeImage(croppedImage);

							var exactBitmap = GetBitmap(resizedImage);

							//var hog = ExtractHog(resizedImage.ToByteArray()).ToArray();
							var hog = ExtractHogAccord(exactBitmap).ToArray();

							double percent = _svm.Probability(hog);
							bool isHuman = percent >= Constants.ETALON_PERCENTAGE;

							if (isHuman)
							{
								recognizedList.Add(new RecognizedObjects
								{
									ID = counter,
									Percentage = percent,
									Frame = frame,
									Image = croppedImage,
									Name = $"{width}_{height}_{counter}"
								});

								resizedImage.SaveImage($"{width}_{height}_{percent}.png");
								counter += 1;
							}
						}
					}

					return recognizedList;
				}
			);
		}


		/// <summary>
		/// Alls the passes of window.
		/// </summary>
		/// <param name="src">The source.</param>
		/// <param name="step">The step.</param>
		/// <returns></returns>
		public Task<List<RecognizedObjects>[]> AllPassesOfWindow(BitmapSource src, int step)
		{
			var preprocessedArray = _imageProcessor.PreprocessImage(src, Filter.Sobel);
			var preprocessedImage = AuxiliaryMethods.BitmapSourceFromArray(preprocessedArray, false);

			int width = Constants.WIDTH;
			int height = Constants.HEIGHT;

			var taskList = new List<Task<List<RecognizedObjects>>>();

			while (width < preprocessedImage.Width && height < preprocessedImage.Height)
			{
				src.Freeze();
				taskList.Add(OnePassOfWindow(src, width, height, step));
				width = (int)Math.Round(width * 1.5);
				height = (int)Math.Round(height * 1.5);
			}

			return Task.WhenAll(taskList);
		}
	}
}