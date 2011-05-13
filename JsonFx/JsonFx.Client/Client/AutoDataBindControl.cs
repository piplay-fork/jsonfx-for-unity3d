using System;
using System.Reflection;
using System.Web.UI;

namespace JsonFx.Client
{
	/// <summary>
	/// Control which will call DataBind if any properties are set containing databinding expressions.
	/// </summary>
	/// <remarks>
	/// http://stackoverflow.com/questions/1417028/
	/// </remarks>
	public class AutoDataBindControl : Control
	{
		#region Constants

		private static readonly object EventDataBinding;

		#endregion Constants

		#region Fields

		private bool needsDataBinding = false;

		#endregion Fields

		#region Init

		/// <summary>
		/// CCtor
		/// </summary>
		static AutoDataBindControl()
		{
			try
			{
				FieldInfo field = typeof(Control).GetField(
					"EventDataBinding",
					BindingFlags.NonPublic|BindingFlags.Static);

				if (field != null)
				{
					AutoDataBindControl.EventDataBinding = field.GetValue(null);
				}
			}
			catch { }

			if (AutoDataBindControl.EventDataBinding == null)
			{
				// effectively disables the auto-binding feature
				AutoDataBindControl.EventDataBinding = new object();
			}
		}

		#endregion Init

		#region Event Handlers

		protected override void DataBind(bool raiseOnDataBinding)
		{
			base.DataBind(raiseOnDataBinding);

			// flag that databinding has taken place
			this.needsDataBinding = false;
		}

		protected override void OnInit(EventArgs e)
		{
			base.OnInit(e);

			// check for the presence of DataBinding event handler
			if (this.HasEvents())
			{
				EventHandler handler = this.Events[AutoDataBindControl.EventDataBinding] as EventHandler;
				if (handler != null)
				{
					// flag that databinding is needed
					this.needsDataBinding = true;

					this.Page.PreRenderComplete += new EventHandler(this.OnPreRenderComplete);
				}
			}
		}

		private void OnPreRenderComplete(object sender, EventArgs e)
		{
			// DataBind only when needed
			if (this.needsDataBinding)
			{
				this.DataBind();
			}
		}

		#endregion Event Handlers
	}
}