using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
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
using Point = System.Windows.Point;

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
		/// The clusterisation processor
		/// </summary>
		private readonly ClusterisationProcessor _clusterisationProcessor;

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
			_clusterisationProcessor = new ClusterisationProcessor();
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

		/// <summary>
		/// Extracts the hog accord.
		/// </summary>
		/// <param name="image">The image.</param>
		/// <returns></returns>
		public IList<double> ExtractHogAccord(Bitmap image)
		{
			var hog = new HistogramsOfOrientedGradients();
			hog.ProcessImage(image);

			var lined = hog.Histograms;

			return ToOneLine(lined);

		}

		/// <summary>
		/// To the one line.
		/// </summary>
		/// <param name="hog">The hog.</param>
		/// <returns></returns>
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
			var framedArray = _imageProcessor.MakeFrame(inputArray);

			var position = 0;
			var newCellSize = Constants.CELL_SIZE + 2;
			var byteLength = newCellSize * SessionStorage.BytesPerPixel;
			var byteCellSize = newCellSize * byteLength;

			byte[][] outputArray = new byte[Constants.WIDTH * Constants.HEIGHT / Constants.CELL_SIZE / Constants.CELL_SIZE][];

			for (int j = 0; j < SessionStorage.Height; j += Constants.CELL_SIZE)
			{
				for (int i = 0; i < SessionStorage.Width; i += Constants.CELL_SIZE)
				{
					var byteOffset = j * SessionStorage.Width + i * SessionStorage.BytesPerPixel;

					outputArray[position] = new byte[byteCellSize];

					for (int k = 0; k < newCellSize; k++)
					{
						Buffer.BlockCopy(framedArray, byteOffset + k * (SessionStorage.Stride + 2 * SessionStorage.BytesPerPixel),
							outputArray[position], k * byteLength, byteLength);
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
					var multiplier = 1d/Math.Sqrt(norma * norma + 0.01);

					finalList.AddRange(unitedList.Select(item => item * multiplier));
				}
			}

			return finalList;

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
				//var bimapImage = new Bitmap(filename);

				var byteArray = image.ToByteArray();
				var hogVector = ExtractHog(byteArray);
				//var hogVector = ExtractHogAccord(bimapImage);

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
		private Task<List<RecognizedObject>> OnePassOfWindow(BitmapSource src, int width, int height, int step)
		{
			var recognizedList = new List<RecognizedObject>();
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

							//var exactBitmap = GetBitmap(resizedImage);

							var hog = ExtractHog(resizedImage.ToByteArray()).ToArray();
							//var hog = ExtractHogAccord(exactBitmap).ToArray();

							double percent = _svm.Probability(hog);
							bool isHuman = percent >= Constants.ETALON_PERCENTAGE;

							if (isHuman)
							{
								recognizedList.Add(new RecognizedObject
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
		public Task<List<RecognizedObject>[]> AllPassesOfWindow(BitmapSource src, int step)
		{
			var preprocessedArray = _imageProcessor.PreprocessImage(src, Filter.Sobel);
			var preprocessedImage = AuxiliaryMethods.BitmapSourceFromArray(preprocessedArray, false);

			int width = Constants.WIDTH;
			int height = Constants.HEIGHT;

			var taskList = new List<Task<List<RecognizedObject>>>();

			while (width < preprocessedImage.Width && height < preprocessedImage.Height)
			{
				src.Freeze();
				taskList.Add(OnePassOfWindow(src, width, height, step));
				width = (int)Math.Round(width * 1.5);
				height = (int)Math.Round(height * 1.5);
			}

			return Task.WhenAll(taskList);
		}

		/// <summary>
		/// Determines whether [is centers are equal] [the specified center real].
		/// </summary>
		/// <param name="centerReal">The center real.</param>
		/// <param name="centerComputed">The center computed.</param>
		/// <param name="tolerance">The tolerance.</param>
		/// <returns>
		///   <c>true</c> if [is centers are equal] [the specified center real]; otherwise, <c>false</c>.
		/// </returns>
		private bool IsCentersAreEqual(Point centerReal, Point centerComputed, int tolerance) =>
			Math.Pow(centerComputed.X - centerReal.X, 2) + Math.Pow(centerComputed.Y - centerReal.Y, 2) <= tolerance * tolerance;

		/// <summary>
		/// 
		/// </summary>
		/// <param name="pathAnnotationsIn"></param>
		/// <param name="pathImagesIn"></param>
		public async Task<double> TestClassificator(string pathAnnotationsIn, string pathImagesIn)
		{
			var annotations = Directory.GetFiles(pathAnnotationsIn);
			var images = Directory.GetFiles(pathImagesIn);

			var listOfRealObjects = new List<List<RecognizedObject>>();
			var listOfRecognizedObjects = new List<List<RecognizedObject>>();

			if (annotations.Length != images.Length)
			{
				throw new ArgumentException("Amount of annotation not equal to amount of images.Check train folder");
			}

			var coords = new List<int>();

			int counter = 0;

			//Read real data about images
			foreach (string annotation in annotations)
			{
				var annotationContent = File.ReadAllText(annotation);

				Regex regex = new Regex(Constants.PATERN_FOR_CROPPING);

				listOfRealObjects.Add(new List<RecognizedObject>());

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

					listOfRealObjects.Last().Add(new RecognizedObject
					{
						Frame = frame,
						ID = counter
					});

					counter++;
				}
			}

			//Work of classificator
			for (int i = 0; i < annotations.Length; i++)
			{
				var image = new BitmapImage(new Uri(images[i], UriKind.RelativeOrAbsolute));
				listOfRecognizedObjects.Add(new List<RecognizedObject>());
				var potentialObjects = (await AllPassesOfWindow(image, 64)).SelectMany(item => item).OrderByDescending(item => item.Percentage).ToList();
				var clusters = _clusterisationProcessor.GetClusters(potentialObjects, Constants.TOLERANCE);

				foreach (var cluster in clusters)
				{
					listOfRecognizedObjects.Last().Add(new RecognizedObject
					{
						Frame = cluster.Frame,
						ID = counter
					});
				}
			}

			return GetPercentageOfRealRecognition(listOfRealObjects, listOfRecognizedObjects, Constants.TOLERANCE);

		}

		/// <summary>
		/// Gets the percentage of real recognition.
		/// </summary>
		/// <param name="etalonList">The etalon list.</param>
		/// <param name="computedList">The computed list.</param>
		/// <param name="tolerance">The tolerance.</param>
		/// <returns></returns>
		public double GetPercentageOfRealRecognition(List<List<RecognizedObject>> etalonList,
			List<List<RecognizedObject>> computedList, int tolerance)
		{
			int counterOfFineRecognition = 0;

			for (int i = 0; i < etalonList.Count; i++)
			{
				var etalonRow = etalonList[i];
				var computedRow = computedList[i];

				foreach (RecognizedObject cell in computedRow)
				{
					if (!cell.Frame.IsEmpty && etalonRow.Any(x => IsCentersAreEqual(x.CalculatedCenter, cell.CalculatedCenter, tolerance)))
					{
						counterOfFineRecognition++;
					}
				}
			}

			int commonCount = etalonList.SelectMany(item => item).Count();

			return counterOfFineRecognition * 100d / (double)commonCount;

		}
	}
}