using System;
using System.Collections.Generic;
using System.Linq;
using HOG_Classificator.Models;
using System.Windows;
using Accord.Math;

namespace HOG_Classificator.Helpers
{
	/// <summary>
	/// ClusterisationProcessor class
	/// </summary>
	public class ClusterisationProcessor
	{
		/// <summary>
		/// Gets the clusters.
		/// </summary>
		/// <param name="itemsList">The items list.</param>
		/// <param name="threshold"></param>
		/// <returns></returns>
		public IList<Cluster> GetClusters(IList<RecognizedObject> itemsList, int threshold)
		{
			List<Cluster> clustersList = new List<Cluster>();

			bool isClustersEqual = false;

			var centresCopy = new List<RecognizedObject>(FindClustersCenter(itemsList, threshold));

			while (!isClustersEqual)
			{
				var oldClusters = new List<Cluster>(clustersList);
				clustersList.Clear();

				foreach (var center in centresCopy)
				{
					clustersList.Add(new Cluster
					{
						Center = center
					});
				}

				var currentList = Except(itemsList, centresCopy, false);
				var distanses = new double[currentList.Count][];

				for (int i = 0; i < clustersList.Count; i++)
				{
					distanses[i] = FindDistanses(currentList, clustersList[i].Center, false).ToArray();
				}

				double[] minDistanseArray = new double[currentList.Count];
				int[] minIndexArray = new int[currentList.Count];

				for (int j = 0; j < currentList.Count; j++)
				{
					double[] innerArray = new double[clustersList.Count];

					for (int i = 0; i < clustersList.Count; i++)
					{
						innerArray[i] = distanses[i][j];
					}

					var minValue = innerArray.Min();
					minDistanseArray[j] = minValue;
					minIndexArray[j] = innerArray.IndexOf(minValue);
				}

				for (int j = 0; j < currentList.Count; j++)
				{
					var index = minIndexArray[j];
					clustersList[index].ItemsList.Add(currentList[j]);
				}

				centresCopy = new List<RecognizedObject>(FindNewCenters(clustersList));

				isClustersEqual = IsOldClustersEqualNews(oldClusters, clustersList);

			}

			return clustersList;
		}


		/// <summary>
		/// Finds the cluster center.
		/// </summary>
		/// <param name="itemsList">The items list.</param>
		/// <param name="threshold">The threshold.</param>
		/// <returns></returns>
		public IList<RecognizedObject> FindClustersCenter(IList<RecognizedObject> itemsList, double threshold)
		{
			var pointsList = new List<RecognizedObject>();
			var maxDistancesList = new List<double>();
			var firstPoint = itemsList.First();

			pointsList.Add(firstPoint);

			var currentList = Except(itemsList, pointsList);
			var distanseVector = FindDistanses(currentList, firstPoint);

			var L = distanseVector.Max();
			maxDistancesList.Add(L);

			if (L < threshold)
			{
				return pointsList;
			}

			var indexOfMaxPoint = distanseVector.IndexOf(L) + 1;
			var pointWithMaxDistanse = itemsList[indexOfMaxPoint];

			pointsList.Add(pointWithMaxDistanse);

			int clusterCounter = 2;
			bool needToStop = false;

			while (!needToStop)
			{
				currentList = Except(itemsList, pointsList);
				var distanses = new double[clusterCounter][];

				for (int i = 0; i < clusterCounter; i++)
				{
					distanses[i] = FindDistanses(currentList, pointsList[i]).ToArray();
				}

				double[] minDistanseArray = new double[currentList.Count];
				double[] minIndexArray = new double[currentList.Count];

				for (int j = 0; j < currentList.Count; j++)
				{
					double[] innerArray = new double[clusterCounter];

					for (int i = 0; i < clusterCounter; i++)
					{
						innerArray[i] = distanses[i][j];
					}

					var minValue = innerArray.Min();
					minDistanseArray[j] = minValue;
					minIndexArray[j] = innerArray.IndexOf(minValue);
				}

				var maxValue = minDistanseArray.Max();

				indexOfMaxPoint = minDistanseArray.IndexOf(maxValue);
				pointWithMaxDistanse = currentList[indexOfMaxPoint];


				var Lc = maxDistancesList.Count > 1
					? maxDistancesList.Sum() / maxDistancesList.Count
					: maxDistancesList.First();

				needToStop = Lc >= 2 * maxValue;

				if (!needToStop)
				{
					pointsList.Add(pointWithMaxDistanse);
					maxDistancesList.Add(maxValue);

					clusterCounter++;
				}
			}

			return pointsList;
		}

		#region Private methods

		/// <summary>
		/// Finds the distanse.
		/// </summary>
		/// <param name="point1">The point1.</param>
		/// <param name="point2">The point2.</param>
		/// <returns></returns>
		private double FindDistanse(Point point1, Point point2) => Math.Sqrt(
			Math.Pow(point2.X - point1.X, 2) + Math.Pow(point2.Y - point1.Y, 2));

		/// <summary>
		/// Finds the distanses.
		/// </summary>
		/// <param name="array">The array.</param>
		/// <param name="recObject">The record object.</param>
		/// <param name="isCenterCalculated"></param>
		/// <returns></returns>
		private IList<double> FindDistanses(IList<RecognizedObject> array, RecognizedObject recObject, bool isCenterCalculated = true)
		{
			var result = new double[array.Count];

			for (int i = 0; i < array.Count; i++)
			{
				result[i] = FindDistanse(
					isCenterCalculated ? recObject.CalculatedCenter : recObject.Center,
					isCenterCalculated ? array[i].CalculatedCenter : array[i].Center);
			}

			return result;
		}

		/// <summary>
		/// Clears the clusters.
		/// </summary>
		/// <param name="listOfClusters">The list of clusters.</param>
		private IList<RecognizedObject> FindNewCenters(IList<Cluster> listOfClusters)
		{
			Point[] newCentres = new Point[listOfClusters.Count];

			for (int i = 0; i < listOfClusters.Count; i++)
			{
				newCentres[i] = new Point(listOfClusters[i].ItemsList.Select(item => item.CalculatedCenter.X).Average(),
					listOfClusters[i].ItemsList.Select(item => item.CalculatedCenter.Y).Average());
			}

			return newCentres.Select(item => new RecognizedObject
			{
				Center = item
			})
				.ToList();
		}

		/// <summary>
		/// Determines whether [is old clusters equal news] [the specified old clusters].
		/// </summary>
		/// <param name="oldClusters">The old clusters.</param>
		/// <param name="newClusters">The new clusters.</param>
		/// <returns>
		///   <c>true</c> if [is old clusters equal news] [the specified old clusters]; otherwise, <c>false</c>.
		/// </returns>
		private bool IsOldClustersEqualNews(IList<Cluster> oldClusters, IList<Cluster> newClusters) =>
			!oldClusters
				.Where((item, index) => item.ItemsList.Count != newClusters[index].ItemsList.Count)
				.Any();

		/// <summary>
		/// Excepts this instance.
		/// </summary>
		/// <returns></returns>
		private IList<RecognizedObject> Except(IList<RecognizedObject> targetList, IList<RecognizedObject> excepted, bool isCenterCalculated = true)
		{
			var copiedTarget = new List<RecognizedObject>(targetList);

			foreach (var recObj in excepted)
			{
				var exceptedObj = copiedTarget
					.FirstOrDefault(item => isCenterCalculated
						? item.CalculatedCenter.Equals(recObj.CalculatedCenter)
						: item.Center.Equals(recObj.Center));

				if (exceptedObj != null)
				{
					copiedTarget.Remove(exceptedObj);
				}
			}

			return copiedTarget;
		}

		#endregion
	}
}