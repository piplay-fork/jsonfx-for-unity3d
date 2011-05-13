/*
	JsonFx_Lang.js
	client-size globalization support

	Created: 2008-09-04-0845
	Modified: 2009-03-07-1044

	Copyright (c)2006-2009 Stephen M. McKamey
	Distributed under an open-source license: http://jsonfx.net/license
*/

/* namespace JsonFx.Lang */
var JsonFx;
if ("undefined" === typeof JsonFx) {
	JsonFx = {};
}
if ("undefined" === typeof JsonFx.Lang) {

	/* ctor */
	JsonFx.Lang = function() {
		// current culture
		var lang = "";

		// internal lookup table
		var rsrc = {};
		
		// global Resources object		
		var rsrcG = {};

		// normalize key
		/*string*/ function normKey(/*string*/ k) {
			k = k.replace(/^\s+|\s+$/g, "");
			k = k.replace(/\s+,|,\s+/g, ",");
			k = k.toLowerCase();
			return k;
		}

		/*void*/ function build(/*string*/ k, /*string*/ v) {
			if (!k) {
				return;
			}

			// add to internal lookup
			rsrc[normKey(k)] = v;

			// build out global Resources object
			k = k.split(',', 2);
			
			if (!k || k.length < 2) {
				return;
			}

			if (!rsrcG.hasOwnProperty(k)) {
				rsrcG[k[0]] = {};
			}
			
			rsrcG[k[0]][k[1]] = v;
		}

		/*void*/ this.add = function(/*object*/ r, /*string*/ c) {
			if (!r) {
				return;
			}

			if ("string" === typeof c) {
				lang = c;
			}

			// merge in the new values
			for (var k in r) {
				if (r.hasOwnProperty(k)) {
					build(k, r[k]);
				}
			}
		};

		/*object*/ this.get = function(/*string*/ k) {
			if ("string" !== typeof k) {
				k = "";
			}

			k = normKey(k);

			return rsrc.hasOwnProperty(k) ? rsrc[k] : "$$"+k+"$$";
		};

		/*void*/ this.getLang = function() {
			return lang;
		};

		// get as global Resources object
		/*object*/ this.getAll = function() {
			return rsrcG;
		};
	};

	/*singleton, destroy the ctor*/
	JsonFx.Lang = new JsonFx.Lang();

	// uncomment to access like .NET Resources object
//	var Resources = JsonFx.Lang.getAll();
}
