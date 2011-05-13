using System;

namespace JbstOnline.Models
{
	public class CompilationError
	{
		public string Message { get; set; }

		public int Line { get; set; }

		public int Col { get; set; }
	}
}
