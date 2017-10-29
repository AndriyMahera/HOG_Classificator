using System;
using System.Threading.Tasks;

namespace HOG_Classificator.Models
{
	/// <summary>
	/// WorkerContent class
	/// </summary>
	public class WorkerContent
	{
		/// <summary>
		/// Gets or sets the method to execute.
		/// </summary>
		public Action<string, string> MethodToExecute { get; set; }

		/// <summary>
		/// Gets or sets the method to execute.
		/// </summary>
		public Func<string, string, Task> AsyncMethodToExecute { get; set; }

		/// <summary>
		/// Gets or sets the parameter.
		/// </summary>
		public string Parameter { get; set; }


		/// <summary>
		/// Gets or sets the parameter.
		/// </summary>
		public string Parameter2 { get; set; }
	}
}