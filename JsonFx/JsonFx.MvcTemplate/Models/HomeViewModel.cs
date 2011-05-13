using System;

namespace MyApp.Models
{
	public class HomeViewModel
	{
		public DateTime RenderTime
		{
			get;
			set;
		}

		public string ServerName
		{
			get;
			set;
		}

		public Version JsonFxVersion
		{
			get;
			set;
		}
	}
}
