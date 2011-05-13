/*global JsonFx, $, SyntaxHighlighter */

JsonFx.UA.setCssUserAgent();

var JbstEditor;
if ("undefined" === typeof JbstEditor) {
	JbstEditor = {};
}

JbstEditor.refresh = function(elem, delay) {
	if (delay < 0) {
		delay = 0;
	}

	var timer = 0;

	function update() {
		timer = 0;

		var display = $("div#jbst-display");
		if (!display.length) {
			display = $("<div id='jbst-display' />");
			// simulate insertAfter(...)
			elem.parentNode.insertBefore(display[0], elem.nextSibling);
			display.bind("click", function(){ $(elem).focus(); });
		}

		display.empty().append(
			$("<pre class='brush:jbst;html-script:true' />").text( elem.innerText || elem.value )
		);

		SyntaxHighlighter.highlight();
	}

	setTimeout(update, 0);

	return function() {
		if (timer) {
			clearTimeout(timer);
		}

		timer = setTimeout(update, delay);
	};
};

JbstEditor.allowTabs = function(evt) {
	evt = evt || event;

	if (evt.keyCode !== 0x09 ||
		evt.altKey ||
		evt.ctrlKey ||
		evt.metaKey) {
		return;
	}

	var val = (this.innerText || this.value),
		start = this.selectionStart,
		end = this.selectionEnd;

	if ("string" !== typeof val ||
		"number" !== typeof start ||
		"number" !== typeof end) {
		return false;
	}

	if (evt.shiftKey) {
		if ('\t' !== val.substr(start-1, 1)) {
			return false;
		}

		this.value = val.substr(0, start-1) + val.substr(end);
		this.selectionStart = start-1;
		this.selectionEnd = end-1;
	} else {
		this.value = val.substr(0, start) + '\t' + val.substr(end);
		this.selectionStart = start+1;
		this.selectionEnd = end+1;
	}
	
	return false;
};

JbstEditor.generate = function() {
	var editor = document.getElementById("jbst-editor"),
		val = (editor.innerText || editor.value);

	var btn = this,
		url = "/compiler";
	JsonFx.IO.sendJsonRequest(
		url,
		{
			params: val,
			method: "POST",
			headers: {
				"Content-Type": "text/plain"
			},
			onSuccess: function(/*JSON*/ results) {
				GA.track(url+"#success");
				results = JbstOnline.CompileSuccess.bind(results);
				var display = $("#compilation-results");
				if (display.length) {
					display.replaceWith( results );
				} else {
					$(btn).after( results );
				}
			},
			onFailure: function(/*JSON*/ obj, /*object*/ cx, /*error*/ ex) {
				GA.track(url+"#failure");
				var results;
				try {
					results = JSON.parse(obj);
				} catch(ex) {}

				if (!results || !results.errors)
				{
					if (!GA.track(url+"#fatal")) {
						alert("Unexpected error:\r\n"+obj);
					}
					return;
				}

				results = JbstOnline.CompileError.bind(results);
				var display = $("#compilation-results");
				if (display.length) {
					display.replaceWith( results );
				} else {
					$(btn).after( results );
				}
			},
			onComplete: function() {
				// TODO: focus attention on the results panel
			}
		});
};

JsonFx.Bindings.add(
	"textarea#jbst-editor",
	function(elem) {
		$(elem)
			.autogrow()
			.bind("change", JbstEditor.refresh(elem, 0))
			.bind("keydown", JbstEditor.allowTabs)
			.bind("focus", function() { $(document.body).addClass("editing"); })
			.bind("blur", function() { $(document.body).removeClass("editing"); });
	});
