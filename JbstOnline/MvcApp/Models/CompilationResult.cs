using System;
using System.Collections.Generic;

namespace JbstOnline.Models
{
	public class CompilationResult
	{
		public string key { get; set; }

		public string name { get; set; }

		public string source { get; set; }

		public string pretty { get; set; }

		public string compacted { get; set; }

		public List<CompilationError> errors { get; set; }
	}
}
