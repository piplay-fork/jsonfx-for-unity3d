<%@ Page Language="C#" Title="Home Page" MasterPageFile="~/Views/Shared/Site.Master" Inherits="System.Web.Mvc.ViewPage<MyApp.Models.HomeViewModel>" %>

<asp:Content ID="C" ContentPlaceHolderID="Content" runat="server">

	<%-- example showing how to directly add to the page a JBST control bound to view data --%>
	<%= Jbst.Bind("Example.congrats", this.Model)%>
	<p>See <a href="http://help.jsonfx.net/instructions">http://help.jsonfx.net/instructions</a> for JsonFx customizations to Visual Studio.</p>

</asp:Content>
