using System;
using System.Web;

using JsonFx.Handlers;

namespace JbstOnline.Models
{
	/// <summary>
	/// View-Model for Home/Index
	/// </summary>
	public class HomeViewModel
	{
		#region Init

		/// <summary>
		/// Ctor
		/// </summary>
		public HomeViewModel()
		{
		}

		/// <summary>
		/// Ctor
		/// </summary>
		/// <param name="source"></param>
		/// <param name="support"></param>
		public HomeViewModel(string source, IOptimizedResult support)
		{
			this.SampleSource = HttpUtility.HtmlEncode(source).Trim();
			this.CompactSize = FormatFileSize(support.Compacted.Length);
			this.FullSize = FormatFileSize(support.PrettyPrinted.Length);
			this.GzipSize = FormatFileSize(support.Gzipped.Length);
			this.DeflateSize = FormatFileSize(support.Deflated.Length);
		}

		#endregion Init

		#region Properties

		public string SampleSource
		{
			get;
			set;
		}

		public string CompactSize
		{
			get;
			set;
		}

		public string FullSize
		{
			get;
			set;
		}

		public string GzipSize
		{
			get;
			set;
		}

		public string DeflateSize
		{
			get;
			set;
		}

		#endregion Properties

		#region Utility Methods

		private string FormatFileSize(int bytes)
		{
			const decimal BytesPerKilo = 1024m;
			const decimal BytesPerMega = 1024m * BytesPerKilo;
			const decimal BytesPerGiga = 1024m * BytesPerMega;
			const decimal BytesPerTera = 1024m * BytesPerGiga;

			if (bytes < BytesPerMega)
			{
				return (bytes / BytesPerKilo).ToString("0.0")+" KB";
			}

			if (bytes < BytesPerGiga)
			{
				return (bytes / BytesPerMega).ToString("0.0")+" MB";
			}

			if (bytes < BytesPerTera)
			{
				return (bytes / BytesPerGiga).ToString("0.0")+" GB";
			}

			return (bytes / BytesPerTera).ToString("0.0")+" TB";
		}

		#endregion Utility Methods
	}
}
