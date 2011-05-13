/*global JSON */
/*
	JsonFx_History.js
	Ajax history support

	Created: 2006-11-11-1759
	Modified: 2009-06-02-0906

	Copyright (c)2006-2009 Stephen M. McKamey
	Released under an open-source license: http://jsonfx.net/license
*/

/* dependency checks --------------------------------------------*/

if ("undefined" === typeof JSON) {
	throw new Error("JsonFx.History requires JSON");
}

/* namespace JsonFx */
var JsonFx;
if ("undefined" === typeof JsonFx) {
	JsonFx = {};
}

/* Utilities ----------------------------------------------------*/

if ("undefined" === typeof JsonFx.jsonReviver) {
	/*object*/ JsonFx.jsonReviver = function(/*string*/ key, /*object*/ value) {
		var a;
		if ("string" === typeof value) {
			a = /^(\d{4})-(\d{2})-(\d{2})T(\d{2}):(\d{2}):(\d{2}(?:\.\d*)?)(Z)?$/.exec(value);
			if (a) {
				if (a[7]) {
					return new Date(Date.UTC(+a[1], +a[2] - 1, +a[3], +a[4], +a[5], +a[6]));
				} else {
					return new Date(+a[1], +a[2] - 1, +a[3], +a[4], +a[5], +a[6]);
				}
			}
		}
		return value;
	};
}

/* namespace JsonFx.UI */
if ("undefined" === typeof JsonFx.UI) {
	JsonFx.UI = {};
}

/*DOM*/ JsonFx.UI.getIFrameDocument = function(/*DOM*/ elem) {
	if (!elem) {
		return null;
	}

	if ("undefined" !== typeof elem.contentDocument) {
		// W3C
		return elem.contentDocument;
	} else if ("undefined" !== typeof elem.contentWindow) {
		// Microsoft
		return elem.contentWindow.document;
	} else if ("undefined" !== typeof elem.document) {
		// deprecated
		return elem.document;
	}
	// not available
	return elem;
};

/* JsonFx.History -----------------------------------------------*/

JsonFx.History = {
	/*object*/ h: null,

	/*void*/ load: function(/*DOM*/ elem, /*function*/ callback, /*object*/ start, /*string*/ url) {
		if (!elem || "function" !== typeof callback) {
			return;
		}
		var info;
		if (!JsonFx.History.h) {
			// initialization

			JsonFx.History.h =
				{
					elem: elem,
					callback: callback,
					url: url
				};

			info = JsonFx.History.getState(JsonFx.History.h);
			if (info) {
				// previously cached page was reloaded
				callback(info);

			} else if (!elem.onload && start) {
				// IE needs a little help ensuring that
				// initial state is stored in history
				JsonFx.History.h.callback = null;

				// re-save start state
				JsonFx.History.save(start);
				// reconnect callback
				JsonFx.History.h.callback = callback;
			} else if (window.opera) {
				// opera is having issues, so disable history
				JsonFx.History.h.elem = null;
				callback(start);
			}

		} else {
			// onchange

			info = JsonFx.History.getState(JsonFx.History.h) || start;
			if (info && JsonFx.History.h.callback) {
				JsonFx.History.h.callback(info);
			}
		}
	},

	/*object*/ getState: function(/*object*/ h) {

		if (!h) {
			return null;
		}

		var doc = JsonFx.UI.getIFrameDocument(h.elem);
		if (!doc || !doc.location || !doc.body) {
			return null;
		}

		var info = h.url ?
			doc.location.search :
			doc.body.innerHTML;

		if (info && h.url) {
			// strip query char and decode
			info = info.substr(1);
			info = decodeURIComponent(info);
		}
		if (!info) {
			return null;
		}

		try {
			return JSON.parse(info, JsonFx.jsonReviver);
		} catch (ex) {
			// history failed. disable saving
			h.elem = null;
			return null;
		}
	},

	/*bool*/ save: function(/*object*/ info) {

		var h = JsonFx.History.h;
		if (!h) {
			return false;
		}

		var doc = JsonFx.UI.getIFrameDocument(h.elem);
		if (!doc || !doc.location || !doc.write) {
			// error just call method directly
			if ("function" === typeof h.callback) {
				h.callback(info);
			}
			return true;
		}

		// replacer function patches known bug in IE8's native JSON
		info = JSON.stringify(
			info,
			function(k, v) { return v === "" ? "" : v; });

		if (h.url) {
			// encode the serialized object into the query string
			doc.location.href = h.url+'?'+encodeURIComponent(info);
		} else {
			// create a new document containing the serialized object
			doc.open();
			try {
				doc.write(info);
			} finally {
				doc.close();
			}
		}
		return true;
	}
};
