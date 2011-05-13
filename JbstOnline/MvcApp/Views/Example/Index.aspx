<%@ Page Language="C#" Inherits="System.Web.Mvc.ViewPage" %>
<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml" xml:lang="<%= System.Globalization.CultureInfo.CurrentCulture.TwoLetterISOLanguageName %>">
<head runat="server">
	<meta http-equiv="Content-Type" content="application/xhtml+xml; charset=UTF-8" />

	<title>Online JBST Compiler - Example</title>

	<%-- one tag to include all the style sheets --%>
	<JsonFx:ResourceInclude runat="server" SourceUrl="~/Styles/Styles.merge" />
</head>
<body>

	<%-- one tag to include all the client scripts --%>
	<JsonFx:ResourceInclude runat="server" SourceUrl="~/Scripts/Example.merge" />

<div class="main">
	<h1 class="title">Generate dynamic Ajax controls from intuitive declarative templates.</h1>
	<div class="step">
		<h2>Design your own template.</a></h2>
		<div class="buttons">
			<a href="/" class="button button-large">&laquo; Back</a>
		</div>
	</div>

	<div class="step">
		<h2>Add your control script.</h2>
		<p class="instructions">Add your JBST control script and the JBST support script to your webpage.</p>

		<p class="instructions"></p>
		<div class="code-example">
			<pre class="brush:html;tab-size:4">
				&lt;script type="text/javascript" src="/scripts/jbst.js"&gt;&lt;/script&gt;
				&lt;script type="text/javascript" src="/scripts/Foo.MyZebraList.js"&gt;&lt;/script&gt;
			</pre>
		</div>

	<div class="step">
		<h2>Bind your data.</h2>
		<p class="instructions">Binding to a JBST control is as easy as it gets. Here's an example of binding Foo.MyZebraList to data:</p>

		<div class="code-example">
			<pre class="brush:js;tab-size:4">
				function showExample() {
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
				}
			</pre>
		</div>
		<script type="text/javascript">

			function showExample() {
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

				GA.track("#showExample");
			}

		</script>
		<div class="buttons">
			<a href="#showExample" class="button button-large" style="font-weight:bold" onclick="showExample();return false;">Show example!</a>
		</div>
	</div>
	<p class="copyright">Copyright &copy;2006-2010 Stephen M. McKamey. Powered by <a href="http://starterkit.jsonfx.net/jbst">JsonFx.NET</a> (v<%= JsonFx.About.Fx.Version %>).</p>
</div>

<JsonFx:ResourceInclude runat="server" SourceUrl="~/Scripts/GA.js" defer="defer" />

</body>
</html>
