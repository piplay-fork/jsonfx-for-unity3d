using System;

public partial class _Default : System.Web.UI.Page 
{
    protected void Page_Load(object sender, EventArgs e)
    {
		this.PageData["Example.myData.RenderTime"] = DateTime.Now;
		this.PageData["Example.myData.ServerName"] = this.Server.MachineName;
		this.PageData["Example.myData.JsonFxVersion"] = JsonFx.About.Fx.Version;
	}

	protected override void OnPreRenderComplete(EventArgs e)
	{
		base.OnPreRenderComplete(e);

		// improve the Yslow rating
		JsonFx.Handlers.ResourceHandler.EnableStreamCompression(this.Context);
	}

	protected override void OnError(EventArgs e)
	{
		// remove compression
		JsonFx.Handlers.ResourceHandler.DisableStreamCompression(this.Context);

		base.OnError(e);
	}
}
