<%@ Page Language="C#" MasterPageFile="~/Views/Shared/Site.Master" Title="Online JBST Compiler" Inherits="System.Web.Mvc.ViewPage<JbstOnline.Models.HomeViewModel>" %>

<asp:Content ID="C" ContentPlaceHolderID="Content" runat="server">

<div class="step step-1">
	<h1 class="title">Generate dynamic Ajax controls from intuitive declarative templates.</h1>
	<h2>1. Design your template (click to edit).</h2>
	<p class="instructions">For more JBST syntax details, see: <a href="http://starterkit.jsonfx.net/jbst" target="_blank">http://starterkit.jsonfx.net/jbst</a></p>

	<textarea id="jbst-editor"><%= this.Model.SampleSource %></textarea>
</div>

<div class="step step-2">
	<h2>2. Compile to JavaScript.</h2>
	<p class="instructions">This produces a JavaScript object which can easily be bound to data e.g. <code>Foo.MyZebraList.bind(data);</code>.</p>

	<div class="buttons">
		<a href="#compile" class="button button-large" onclick="JbstEditor.generate.call(this);return false;">Generate&hellip;</a>
	</div>
</div>

<div class="step step-3">
	<h2>3. Download the JBST runtime.</h2>
	<p class="instructions">This is the only additional script needed to dynamically bind your control in the browser. It is only needed once, no matter how many different JBST controls you create.</p>

	<div class="buttons">
		<a href="/compiler/scripts" class="button button-large">Full Source - <%= this.Model.FullSize %></a>
		<a href="/compiler/compacted" class="button button-large">Compacted - <%= this.Model.CompactSize %></a>
		<b>[ GZip - <%= this.Model.GzipSize %> ] [ Deflate - <%= this.Model.DeflateSize %> ]</b>
	</div>
</div>

<div class="step step-4">
	<h2>4. Done!</h2>
	<p class="instructions">Add your JBST control script and the support script to your webpage. Binding to a JBST control is as easy as it gets. Here's an example usage:</p>

	<div class="code-example">
		<pre class="brush:js;tab-size:4">
			// get some data
			var myData = 
				{
					title: "Hello world",
					description: "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Nam lacinia consequat diam, ac auctor eros suscipit et. Donec rhoncus bibendum scelerisque.",
					timestamp: new Date(),
					children: [
						{ label: "The first item", selected: false },
						{ label: "Another child item", selected: false },
						{ label: "And again", selected: false },
						{ label: "Final item", selected: false }
					]
				};

			// randomly mark one as selected
			myData.children[Math.floor(Math.random() * myData.children.length)].selected = true;

			// bind the control to your data
			var myList = Foo.MyZebraList.bind(myData);

			// insert into the page
			document.body.appendChild(myList);
		</pre>
	</div>

	<div class="buttons">
		<a href="/example" class="button button-large" style="font-weight:bold">See the example in action &raquo;</a>
		<a href="http://jsonfx.googlecode.com/" class="button button-large" target="_blank">Get the source for this app &raquo;</a>
	</div>
	<p class="copyright">Copyright &copy;2006-2010 Stephen M. McKamey. Powered by <a href="http://starterkit.jsonfx.net/jbst">JsonFx.NET</a> (v<%= JsonFx.About.Fx.Version %>).</p>
</div>

</asp:Content>
