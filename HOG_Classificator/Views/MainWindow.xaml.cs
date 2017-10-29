using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using HOG_Classificator.Enums;
using HOG_Classificator.Helpers;
using HOG_Classificator.Infrastructure;
using HOG_Classificator.Models;
using MessageBox = System.Windows.MessageBox;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace HOG_Classificator.Views
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		#region Private fields

		/// <summary>
		/// The worker
		/// </summary>
		private BackgroundWorker _worker;

		/// <summary>
		/// The hog processor
		/// </summary>
		private readonly HogProcessor _hogProcessor;

		/// <summary>
		/// The image processot
		/// </summary>
		private readonly ImageProcessor _imageProcessor;

		#endregion

		/// <summary>
		/// Initializes a new instance of the <see cref="MainWindow"/> class.
		/// </summary>
		public MainWindow()
		{

			InitializeComponent();
			InitializeWorker();
			_hogProcessor = new HogProcessor();
			_imageProcessor = new ImageProcessor();
			_hogProcessor.ReportProgress += ReportProgress;
			_imageProcessor.ReportProgress += ReportProgress;
		}

		/// <summary>
		/// Opens the file.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
		private async void OpenFile(object sender, RoutedEventArgs e)
		{
			OpenFileDialog openFileDialog = new OpenFileDialog
			{
				Filter = "Images|*.bmp;*.dib;*.rle;*.jpg;*.png",
				FilterIndex = 1,
				Multiselect = false
			};

			var userClickedOk = openFileDialog.ShowDialog();

			if (userClickedOk == true)
			{
				var path = openFileDialog.FileName;
				BitmapSource image = new BitmapImage(new Uri(path, UriKind.RelativeOrAbsolute));

				var recognizedObject = (await _hogProcessor.AllPassesOfWindow(image, 64)).SelectMany(item => item).OrderByDescending(item => item.Percentage).ToList();
			}
		}

		/// <summary>
		/// Preprocesses the specified sender.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
		private void Preprocess(object sender, RoutedEventArgs e)
		{
			FolderBrowserDialog fbd = new FolderBrowserDialog();

			if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
			{
				var path = fbd.SelectedPath;
				SessionStorage.SetGrayscaleCoef(SliderBlack.Value, SliderWhite.Value);

				WorkerContent workerContent = new WorkerContent
				{
					MethodToExecute = _imageProcessor.ConvertImages,
					Parameter = path,
					Parameter2 = path.Contains(Constants.HUMAN_TITLE)
						? Constants.PRE_FOLDER_HUMAN
						: Constants.PRE_FOLDER_BACKGROUND
				};

				_worker.RunWorkerAsync(workerContent);
			}
		}

		/// <summary>
		/// Trains the specified sender.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
		private void AddToDatabase(object sender, RoutedEventArgs e)
		{
			FolderBrowserDialog fbd = new FolderBrowserDialog();

			if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
			{
				SessionStorage.SetGrayscaleCoef(SliderBlack.Value, SliderWhite.Value);
				var path = fbd.SelectedPath;

				WorkerContent workerContent = new WorkerContent
				{
					MethodToExecute = _hogProcessor.AddTrainingSamplesToDatabase,
					Parameter = path,
					Parameter2 = path.Contains(Constants.HUMAN_TITLE)
					? Constants.DATABASE_PATH_HUMAN
					: Constants.DATABASE_PATH_BACKGROUND
				};

				_worker.RunWorkerAsync(workerContent);
			}
		}

		/// <summary>
		/// Crops the specified sender.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
		private void Crop(object sender, RoutedEventArgs e)
		{
			FolderBrowserDialog fbd = new FolderBrowserDialog();

			if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
			{
				//var annotationPath = fbd.SelectedPath;

				//fbd = new FolderBrowserDialog();

				//if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
				//{
				//	var imagePath = fbd.SelectedPath;

				//	ImageProcessor.CropImages(annotationPath, imagePath, Constants.PRE_FOLDER_CROPPED);

				SessionStorage.SetGrayscaleCoef(SliderBlack.Value, SliderWhite.Value);
				var path = fbd.SelectedPath;
				_imageProcessor.CutImagesOnSlices(path, Constants.PRE_FOLDER_CROPPED);
			}


		}

		/// <summary>
		/// Initializes the worker.
		/// </summary>
		private void InitializeWorker()
		{
			_worker = new BackgroundWorker
			{
				WorkerReportsProgress = true
			};

			_worker.DoWork += worker_DoWork;
			_worker.RunWorkerCompleted += worker_RunWorkerCompleted;
		}


		/// <summary>
		/// Handles the DoWork event of the worker control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="DoWorkEventArgs"/> instance containing the event data.</param>
		private void worker_DoWork(object sender, DoWorkEventArgs e)
		{
			WorkerContent workerContent = (WorkerContent)e.Argument;

			//workerContent.AsyncMethodToExecute?.Invoke(workerContent.Parameter, workerContent.Parameter2);

			workerContent.MethodToExecute?.Invoke(workerContent.Parameter, workerContent.Parameter2);
		}

		/// <summary>
		/// Handles the RunWorkerCompleted event of the worker control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="RunWorkerCompletedEventArgs"/> instance containing the event data.</param>
		private void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			MessageBox.Show("Completed");
		}

		/// <summary>
		/// Reports the progress.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The e.</param>
		private void ReportProgress(object sender, ProgressArgs e)
		{
			Dispatcher.Invoke(() =>
			{
				ProgressBar.Value = e.Percentage;
			});
		}

		/// <summary>
		/// Trains the specified sender.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
		private void Train(object sender, RoutedEventArgs e)
		{
			WorkerContent workerContent = new WorkerContent
			{
				MethodToExecute = _hogProcessor.Train,
				Parameter = Constants.DATABASE_PATH_HUMAN,
				Parameter2 = Constants.DATABASE_PATH_BACKGROUND
			};

			_worker.RunWorkerAsync(workerContent);
		}
	}
}