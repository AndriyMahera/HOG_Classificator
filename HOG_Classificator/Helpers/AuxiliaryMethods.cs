using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Xml.Serialization;
using HOG_Classificator.Infrastructure;
using HOG_Classificator.Models;
using Accord.MachineLearning.VectorMachines;
using Accord.MachineLearning.VectorMachines.Learning;
using Accord.Statistics.Kernels;

namespace HOG_Classificator.Helpers
{
	/// <summary>
	/// AuxiliaryMethods class
	/// </summary>
	public static class AuxiliaryMethods
	{
		/// <summary>
		/// Writes the text.
		/// </summary>
		/// <param name="pixels">The pixels.</param>
		/// <param name="filePath">The file path.</param>
		/// <param name="height">The height.</param>
		/// <param name="width">The width.</param>
		public static void WriteTxt(IList<double> pixels, string filePath, int height, int width)
		{
			using (StreamWriter sw = new StreamWriter(filePath, false))
			{
				for (int j = 0; j < width; j++)
				{
					sw.WriteLine(String.Concat(pixels.Skip(j * height).Take(height).Select(i => $"{i:0.##}\t")));
				}
			}
		}

		/// <summary>
		/// Saves the image.
		/// </summary>
		/// <param name="bitmapSource">The bitmap source.</param>
		/// <param name="filePath">The file path.</param>
		public static void SaveImage(this BitmapSource bitmapSource, string filePath)
		{
			using (var fileStream = new FileStream(filePath, FileMode.Create))
			{
				BitmapEncoder encoder = new PngBitmapEncoder();
				encoder.Frames.Add(BitmapFrame.Create(bitmapSource));
				encoder.Save(fileStream);
			}
		}

		/// <summary>
		/// To the byte array.
		/// </summary>
		/// <param name="sourceBitmap">The source bitmap.</param>
		/// <returns>byte array from image</returns>
		public static byte[] ToByteArray(this BitmapSource sourceBitmap)
		{
			SessionStorage.InitializeStorage(sourceBitmap, 1);
			byte[] pixels = new byte[SessionStorage.Stride * SessionStorage.Height];

			sourceBitmap.CopyPixels(pixels, SessionStorage.Stride, 0);

			return pixels;
		}

		/// <summary>
		/// Bitmaps the source from array.
		/// </summary>
		/// <param name="pixels">The pixels.</param>
		/// <param name="isFramed"></param>
		/// <returns></returns>
		public static BitmapSource BitmapSourceFromArray(byte[] pixels, bool isFramed = true)
		{
			WriteableBitmap bitmap;

			if (isFramed)
			{
				bitmap = new WriteableBitmap(SessionStorage.NewWidth, SessionStorage.NewHeight, SessionStorage.DpiX, SessionStorage.DpiY, SessionStorage.Format, null);
				bitmap.WritePixels(new Int32Rect(0, 0, SessionStorage.NewWidth, SessionStorage.NewHeight), pixels, SessionStorage.NewStride, 0);
			}
			else
			{
				bitmap = new WriteableBitmap(SessionStorage.Width, SessionStorage.Height, SessionStorage.DpiX, SessionStorage.DpiY, SessionStorage.Format, null);
				bitmap.WritePixels(new Int32Rect(0, 0, SessionStorage.Width, SessionStorage.Height), pixels, SessionStorage.Stride, 0);
			}

			return bitmap;
		}

		/// <summary>
		/// Serializes the specified my object.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="myObject">My object.</param>
		/// <param name="path">The path.</param>
		public static void Serialize<T>(T myObject, string path)
		{
			XmlSerializer formatter = new XmlSerializer(typeof(T));

			using (FileStream fs = new FileStream(path, FileMode.Create))
			{
				formatter.Serialize(fs, myObject);
			}
		}

		/// <summary>
		/// Deserializes the specified path.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="path">The path.</param>
		/// <returns></returns>
		public static T Deserialize<T>(string path)
		{
			XmlSerializer formatter = new XmlSerializer(typeof(T));
			T myObject;

			using (FileStream fs = new FileStream(path, FileMode.OpenOrCreate))
			{
				myObject = (T)formatter.Deserialize(fs);
			}

			return myObject;
		}

		/// <summary>
		/// Makes the serialization.
		/// </summary>
		/// <param name="svm">The SVM.</param>
		/// <param name="path">The path.</param>
		public static void SerializeSVM(SupportVectorMachine<Gaussian> svm, string path)
		{
			SVMGaussianData data = new SVMGaussianData();

			data.Initialize(
				svm.NumberOfInputs,
				svm.NumberOfOutputs,
				svm.SupportVectors,
				svm.Threshold,
				svm.Weights,
				svm.Kernel.Gamma,
				svm.Kernel.Sigma,
				svm.Kernel.SigmaSquared);

			Serialize(data, path);
		}

		/// <summary>
		/// Deserializes the SVM.
		/// </summary>
		/// <param name="path">The path.</param>
		/// <returns></returns>
		public static SupportVectorMachine<Gaussian> DeserializeSVM(string path)
		{
			var teacher = new SequentialMinimalOptimization<Gaussian>()
			{
				UseComplexityHeuristic = true,
				UseKernelEstimation = true
			};
			double[][] inputs2 = new double[4][];

			inputs2[0] = new[] { 1d, 1d };
			inputs2[1] = new[] { 1d, 1d };
			inputs2[2] = new[] { 1d, 1d };
			inputs2[3] = new[] { 1d, 1d };

			double[] outputs2 = { 1, 1, 0, 0 };

			SupportVectorMachine<Gaussian> svmAfter = teacher.Learn(inputs2, outputs2);
			SVMGaussianData dataAfter = Deserialize<SVMGaussianData>(path);

			svmAfter.NumberOfInputs = dataAfter.NumberOfInputs;
			svmAfter.NumberOfOutputs = dataAfter.NumberOfOutputs;
			svmAfter.SupportVectors = dataAfter.SupportVectors;
			svmAfter.Threshold = dataAfter.Threshold;
			svmAfter.Weights = dataAfter.Weights;
			Gaussian kernel = new Gaussian
			{
				Gamma = dataAfter.Gamma,
				Sigma = dataAfter.Sigma,
				SigmaSquared = dataAfter.SigmaSquared
			};

			svmAfter.Kernel = kernel;

			return svmAfter;
		}

		/// <summary>
		/// Writes the weight.
		/// </summary>
		/// <param name="array">The array.</param>
		/// <param name="path">The path.</param>
		public static void WriteWeight(double[] array, string path)
		{
			using (StreamWriter sw = new StreamWriter(path, false))
			{
				sw.Write(string.Join(" ", array));
			}
		}

		/// <summary>
		/// Reads the weight.
		/// </summary>
		/// <param name="path">The path.</param>
		/// <returns></returns>
		public static double[] ReadWeight(string path)
		{
			string file;

			using (StreamReader sr = new StreamReader(path))
			{
				file = sr.ReadToEnd();
			}

			return file.Split(' ').Select(Convert.ToDouble).ToArray();
		}
	}
}