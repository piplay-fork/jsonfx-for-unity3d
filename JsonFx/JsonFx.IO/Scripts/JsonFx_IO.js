/*global JSON, ActiveXObject */
/*
	JsonFx_IO.js
	Ajax & JSON-RPC support

	Created: 2006-11-09-0120
	Modified: 2009-11-28-1519

	Copyright (c)2006-2009 Stephen M. McKamey
	Released under an open-source license: http://jsonfx.net/license
*/

/* dependency checks --------------------------------------------*/

if ("undefined" === typeof JSON) {
	throw new Error("JsonFx.IO requires JSON");
}

/* ----------------------------------------------------*/

(function () {
	// wrapping in anonymous function so that the XHR ID list
	// will be only available as a closure, as this will not
	// modify the global namespace, and it will be shared
	var XHR_OCXs;

	if ("undefined" === typeof XMLHttpRequest) {

		// these IDs are as per MSDN documentation (including case)
		/*string[]*/ XHR_OCXs = !ActiveXObject ?
			[] :
			[
				"Msxml2.XMLHTTP.6.0",
				"Msxml2.XMLHttp.5.0",
				"Msxml2.XMLHttp.4.0",
				"MSXML2.XMLHTTP.3.0",
				"MSXML2.XMLHTTP",
				"Microsoft.XMLHTTP"
			];

		// XMLHttpRequest: augment browser to have "native" XHR
		/*XHR*/ XMLHttpRequest = function() {
			while (XHR_OCXs.length) {
				try {
					return new ActiveXObject(XHR_OCXs[0]);
				} catch (ex) {
					// remove the failed XHR_OCXs for all future requests
					XHR_OCXs.shift();
				}
			}

			// all XHR_OCXs failed		
			return null;
		};
	}
})();

/* ----------------------------------------------------*/

/* namespace JsonFx */
var JsonFx;
if ("undefined" === typeof JsonFx) {
	JsonFx = {};
}

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

/*
	RequestOptions = {
		// HTTP Options
		async : bool,
		method : string,
		headers : Dictionary<string, string>,
		timeout : number,
		params : string,

		// callbacks
		onCreate : function(XMLHttpRequest, context){},
		onSuccess : function(XMLHttpRequest, context){},
		onFailure : function(XMLHttpRequest, context, Error){},
		onTimeout : function(XMLHttpRequest, context, Error){},
		onComplete : function(XMLHttpRequest, context){},

		// callback context
		context : object
	};
*/

/* namespace JsonFx.IO */
JsonFx.IO = {
	/*bool*/ hasAjax: !!new XMLHttpRequest(),

	/* default RequestOptions */
	timeout: 60000,
	onCreate: null,
	onBeginRequest: null,
	onEndRequest: null,
	onSuccess: null,
	onTimeout: null,
	onComplete: null,
	/*RequestOptions*/ onFailure : function(/*XMLHttpRequest|JSON*/ obj, /*object*/ cx, /*error*/ ex) {
		if (ex) {
			var name = ex.name || "Error",
				msg = ex.message || String(ex),
				code = isFinite(ex.code) ? Number(ex.code) : Number(ex.number);

			if (isFinite(code)) {
				name += " ("+code+")";
			}

			(onerror||alert)("Request "+name+":\n\""+msg+"\"",
				obj,
				ex.lineNumber||ex.line||1);
		}
	},

	/*RequestOptions*/ validateOptions: function(/*RequestOptions*/ options) {
		// establish defaults
		if ("object" !== typeof options) {
			options = {};
		}
		if ("boolean" !== typeof options.async) {
			options.async = true;
		}
		if ("string" !== typeof options.method) {
			options.method = "POST";
		} else {
			options.method = options.method.toUpperCase();
		}
		if ("string" !== typeof options.params) {
			options.params = null;
		}
		if ("object" !== typeof options.headers) {
			options.headers = {};
		}
		if (options.method === "POST" &&
			options.params &&
			!options.headers["Content-Type"]) {
			options.headers["Content-Type"] = "application/x-www-form-urlencoded";
		}

		// prevent server from sending 304 Not-Modified response
		// since we don't have a way to access the browser cache
		options.headers["If-Modified-Since"] = "Sun, 1 Jan 1995 00:00:00 GMT";
		options.headers["Cache-Control"] = "no-cache";
		options.headers.Pragma = "no-cache";

		if ("number" !== typeof options.timeout) {
			options.timeout = JsonFx.IO.timeout;// 1 minute
		}
		if ("function" !== typeof options.onCreate) {
			options.onCreate = JsonFx.IO.onCreate;
		}
		if ("function" !== typeof options.onSuccess) {
			options.onSuccess = JsonFx.IO.onSuccess;
		}
		if ("function" !== typeof options.onFailure) {
			options.onFailure = JsonFx.IO.onFailure;
		}
		if ("function" !== typeof options.onTimeout) {
			options.onTimeout = JsonFx.IO.onTimeout;
		}
		if ("function" !== typeof options.onComplete) {
			options.onComplete = JsonFx.IO.onComplete;
		}
		if ("undefined" === typeof options.context) {
			options.context = JsonFx.IO.context;
		}
		return options;
	},

	/*void*/ sendRequest: function(
		/*string*/ url,
		/*RequestOptions*/ options) {

		// ensure defaults
		options = JsonFx.IO.validateOptions(options);

		var xhr = new XMLHttpRequest();

		if (options.onCreate) {
			// create
			options.onCreate.call(this, xhr, options.context);
		}

		if (!xhr) {
			if (options.onFailure) {
				// immediate failure: xhr wasn't created
				options.onFailure.call(this, xhr, options.context, new Error("XMLHttpRequest not supported"));
			}
			if (options.onComplete) {
				// complete
				options.onComplete.call(this, xhr, options.context);
			}
			return;
		}

		var cancel;
		if (options.timeout > 0) {
			// kill off request if takes too long
			cancel = setTimeout(
				function () {
					if (xhr) {
						xhr.onreadystatechange = function(){};
						xhr.abort();
						xhr = null;
					}
					if (options.onTimeout) {
						// timeout-specific handler
						options.onTimeout.call(this, xhr, options.context, new Error("Request Timeout"));
					} else if (options.onFailure) {
						// general-failure handler
						options.onFailure.call(this, xhr, options.context, new Error("Request Timeout"));
					}
					if (options.onComplete) {
						// complete
						options.onComplete.call(this, xhr, options.context);
					}
				}, options.timeout);
		}

		function onRSC() {
			/*
				var readyStates = [
						"uninitialized",
						"loading",
						"loaded",
						"interactive",
						"complete"
					];

				try { document.body.appendChild(document.createTextNode((xhr?readyStates[xhr.readyState]:"null")+";")); } catch (ex) {}
			*/
			var status, ex;
			if (xhr && xhr.readyState === 4 /*complete*/) {

				// stop the timeout
				clearTimeout(cancel);

				// check the status
				status = 0;
				try {
					status = Number(xhr.status);
				} catch (ex2) {
					// Firefox doesn't allow status to be accessed after xhr.abort()
				}

				if (status === 0) {
					// timeout

					// IE reports status zero when aborted
					// Firefox throws exception, which we set to zero
					// options.onTimeout has already been called so do nothing
					// timeout calls onComplete
					return;

				} else if (Math.floor(status/100) === 2) {// 200-299
					// success
					if (options.onSuccess) {
						options.onSuccess.call(this, xhr, options.context);
					}

				} else if (options.onFailure) { // status not 200-299
					// failure
					ex = new Error(xhr.statusText);
					ex.code = status;
					options.onFailure.call(this, xhr, options.context, ex);
				}

				if (options.onComplete) { // all
					// complete
					options.onComplete.call(this, xhr, options.context);
				}
				xhr = null;
			}
		}

		try {
			xhr.onreadystatechange = onRSC;
			xhr.open(options.method, url, options.async);

			if (options.headers) {
				for (var h in options.headers) {
					if (options.headers.hasOwnProperty(h) && options.headers[h]) {
						try {// Opera 8.0.0 doesn't have xhr.setRequestHeader
							xhr.setRequestHeader(h, options.headers[h]);
						} catch (ex) { }
					}
				}
			}

			if (options.method === "POST" && !options.params) {
				options.params = "";
			}
			xhr.send(options.params);

		} catch (ex2) {
			// immediate failure: exception thrown
			if (options.onFailure) {
				options.onFailure.call(this, xhr, options.context, ex2);
			}

		} finally {
			// in case immediately returns?
			onRSC();
		}
	},

	/* JsonRequest ----------------------------------------------------*/

	/*void*/ sendJsonRequest: function (
		/*string*/ restUrl,
		/*RequestOptions*/ options) {

		if ("undefined" !== typeof options &&
			"undefined" !== typeof options.params) {

			// if Content-Type is not specified, then encode as JSON
			if (!options.headers || !options.headers["Content-Type"]) {

				options.params = JSON.stringify(options.params);
				if (!options.headers) {
					options.headers = {};
				}
				options.headers["Content-Type"] = "application/json";
			}
		}

		// ensure defaults
		options = JsonFx.IO.validateOptions(options);

		options.headers.Accept = "application/json, application/jsonml+json";

		var onSuccess = options.onSuccess;
		options.onSuccess = function(/*XMLHttpRequest*/ xhr, /*object*/ context) {

			// decode response as JSON
			var json = xhr ? xhr.responseText : null;
			try {
				json = (json && "string" === typeof json) ?
					JSON.parse(json, JsonFx.jsonReviver) :
					null;

				if ("function" === typeof onSuccess) {
					onSuccess.call(this, json, context);
				}
			} catch (ex) {
				if (options.onFailure) {
					options.onFailure.call(this, xhr, context, ex);
				}
			} finally {
				// free closure references
				onSuccess = options = null;
			}
		};

		var onFailure = null;
		if (options.onFailure) {
			onFailure = options.onFailure;
			options.onFailure = function (/*XMLHttpRequest*/ xhr, /*object*/ context, /*Error*/ ex) {

				onFailure.call(this, (xhr&&xhr.responseText), context, ex);

				// free closure references
				onFailure = null;
			};
		}

		JsonFx.IO.sendRequest.call(this, restUrl, options);
	},

	/* JSON-RPC ----------------------------------------------------*/

	/*string*/ jsonRpcPathEncode: function (/*string*/ rpcMethod, /*object|array*/ rpcParams) {
		var i, enc = encodeURIComponent, rpcUrl = "/";
		if (rpcMethod && rpcMethod !== "system.describe") {
			rpcUrl += enc(rpcMethod);
		}
		if ("object" === typeof rpcParams) {
			rpcUrl += "?";
			if (rpcParams instanceof Array) {
				for (i=0; i<rpcParams.length; i++) {
					if (i > 0) {
						rpcUrl += "&";
					}
					rpcUrl += enc(i);
					rpcUrl += "=";
					rpcUrl += enc(rpcParams[i]);
				}
			} else {
				for (var p in rpcParams) {
					if (rpcParams.hasOwnProperty(p)) {
						rpcUrl += enc(p);
						rpcUrl += "=";
						rpcUrl += enc(rpcParams[p]);
					}
				}
			}
		}
	},

	/*void*/ sendJsonRpc: function(
		/*string*/ rpcUrl,
		/*string*/ rpcMethod,
		/*object|array*/ rpcParams,
		/*RequestOptions*/ options) {

		// ensure defaults
		options = JsonFx.IO.validateOptions(options);

		if (!options.headers.Accept) {
			options.headers.Accept = "application/json, application/jsonml+json";
		}

		// wrap callbacks with RPC layer
		var onSuccess = options.onSuccess;
		var onFailure = options.onFailure;

		// this calls onSuccess with the results of the method (not the RPC wrapper)
		// or it calls onFailure with the error of the method (not the RPC wrapper)
		options.onSuccess = function(/*XMLHttpRequest*/ xhr, /*object*/ cx) {

			var json = xhr ? xhr.responseText : null;
			try {
				json = ("string" === typeof json) ? JSON.parse(json, JsonFx.jsonReviver) : null;

				if (json.error) {
					if (onFailure) {
						onFailure.call(this, json, cx, json.error);
					}
				} else {
					if (onSuccess) {
						onSuccess.call(this, json.result, cx);
					}
				}

			} catch (ex) {
				if (onFailure) {
					onFailure.call(this, json, cx, ex);
				}
			}

			// free closure references
			onFailure = onSuccess = null;
		};

		// this calls onFailure with the RPC response
		options.onFailure = function(/*XMLHttpRequest*/ xhr, /*object*/ cx, /*Error*/ ex) {

			var json = xhr ? xhr.responseText : null;
			try {
				json = (json && "string" === typeof json) ?
					JSON.parse(json, JsonFx.jsonReviver) :
					null;

				if (onFailure) {
					onFailure.call(this, json, cx, ex);
				}
			} catch (ex2) {
				if (onFailure) {
					onFailure.call(this, json, cx, ex?ex:ex2);
				}
			}

			// free closure references
			onFailure = null;
		};

		if ("object" !== typeof rpcParams) {// must be object or array, else wrap in array
			rpcParams = [ rpcParams ];
		}

		var rpcRequest;
		if (options.method === "GET") {
			// GET RPC is encoded as part the URL
			rpcUrl += JsonFx.IO.jsonRpcPathEncode(rpcMethod, rpcParams);

		} else {
			// POST RPC is encoded as a JSON body
			rpcRequest = {
					jsonrpc : "2.0",
					method : rpcMethod,
					params : rpcParams,
					id : new Date().valueOf()
				};

			try {
				// JSON encode request object
				// replacer function patches known bug in IE8's native JSON
				rpcRequest = JSON.stringify(
					rpcRequest,
					function(k, v) { return v === "" ? "" : v; });
			} catch (ex) {
				// if violates JSON, then fail
				if (onFailure) {
					onFailure.call(this, rpcRequest, options.context, ex);
				}
				return;
			}

			options.params = rpcRequest;
			options.headers["Content-Type"] = "application/json";
			options.headers["Content-Length"] = rpcRequest.length;
		}
		JsonFx.IO.sendRequest(rpcUrl, options);
	},

	/*void*/ loadScript : function(/*string*/ url, /*bool*/ dedup) {
		if (!url) {
			return;
		}

		// check if script already exists
		if (dedup) {
			/*elem[]*/ var scripts = document.getElementsByTagName("script");
			for (var i=0; i<scripts.length; i++) {
				// TODO: verify with normalization of URLs
				if (scripts[i].src === url) {
					return;
				}
			}
		}

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
	}
};

/* JsonRpcService ----------------------------------------------------*/

/* Base type for generated JSON Services */

/* Ctor */
JsonFx.IO.Service = function(/*string*/ url) {
	this.address = url||"";
};

/*string*/ JsonFx.IO.Service.appRoot = "";
/*void*/ JsonFx.IO.Service.setAppRoot = function(/*string*/ root) {
	if (!root) {
		JsonFx.IO.Service.appRoot = "";
		return;
	}

	if (root.charAt(root.length-1) === '/') {
		root = root.substr(0, root.length-1);
	}

	JsonFx.IO.Service.appRoot = root;
};

/*event*/ JsonFx.IO.Service.prototype.onBeginRequest = null;

/*event*/ JsonFx.IO.Service.prototype.onEndRequest = null;

/*event*/ JsonFx.IO.Service.prototype.onAddCustomHeaders = null;

/*string*/ JsonFx.IO.Service.prototype.getAddress = function() {
	if (JsonFx.IO.Service.appRoot) {
		return JsonFx.IO.Service.appRoot + this.address;
	} else {
		return this.address;
	}
};

/*void*/ JsonFx.IO.Service.prototype.invoke = function(
	/*string*/ rpcMethod,
	/*object*/ rpcParams,
	/*RequestOptions*/ options) {

	// ensure defaults
	options = JsonFx.IO.validateOptions(options);

	if (this.isDebug) {
		options.timeout = -1;
	}

	var self = this, onComplete = null;
	if ("function" === typeof this.onEndRequest) {
		// intercept onComplete to call onEndRequest
		onComplete = options.onComplete;
		options.onComplete = function(/*JSON*/ json, /*object*/ cx) {
			self.onEndRequest(cx);

			if (onComplete) {
				onComplete(json, cx);
			}

			// free closure references				
			self = onComplete = null;
		};
	}

	if ("function" === typeof this.onAddCustomHeaders) {
		this.onAddCustomHeaders(options.headers);
	}

	if ("function" === typeof this.onBeginRequest) {
		this.onBeginRequest(options.context);
	}

	JsonFx.IO.sendJsonRpc(this.getAddress(), rpcMethod, rpcParams, options);
};

// service description is callable via two methods
/*string*/ JsonFx.IO.Service.prototype["system.describe"] = JsonFx.IO.Service.prototype.$describe =
	function(/*RequestOptions*/ options) {
		this.invoke("system.describe", null, options);
	};
