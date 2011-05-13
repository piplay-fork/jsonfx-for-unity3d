/*global _gat */

/* namespace GA */
var GA;
if ("undefined" === typeof GA) {
	GA = {};
}

/*bool*/ GA.init = function(/*string*/ id) {
	if ("undefined" !== typeof _gat &&
		!document.location.port || document.location.port === 80) {
		GA.gat = _gat._getTracker(id);
		GA.gat._trackPageview();
		return true;
	}
	return false;
};

/*bool*/ GA.track = function(/*string*/ url) {
	var loc = document.location;
	if (url && "undefined" !== typeof GA.gat) {
		if (url.indexOf('#') === 0) {
			url = loc.href+url;
		} 
		var domain = loc.protocol+"//"+loc.host;
		url = url.replace(domain, "");
		GA.gat._trackPageview(url);
		return true;
	}
	return false;
};

if (document.location.hostname.toLowerCase().indexOf("jbst.net") >= 0) {
	setTimeout(function() {
		var url = "http://www.google-analytics.com/ga.js";

		if (!document.body) {
			/*jslint evil:true */
			document.write('<' + 'script src="' + url + '" type="text/javascript"><' + '/script>');
			/*jslint evil:false */
		} else {
			// create a new script request
			var elem = document.createElement("script");
			elem.setAttribute("type","text/javascript");
			elem.setAttribute("src", url);

			// append to body
			document.body.appendChild(elem);
		}

		setTimeout(function() {
			GA.init("UA-1294169-19");
		}, 100);
	}, 0);
}
