/*global JSON */

/*
	it is a best practice to not clutter the global namespace
	creating top level objects which contain variables and functions
	allows us to simulate namespaces
*/

JsonFx.UA.setCssUserAgent();

/* namespace Example */
var Example;
if ("undefined" === typeof Example) {
	Example = {};
}

/*void*/ Example.asyncError = function (/*object*/ result, /*object*/ cx, /*Error*/ ex) {
	// just display raw error results
	alert( JSON.stringify(ex, null, "\t") );
};

/*void*/ Example.callJsonRpc = function(/*DOM*/ elem) {
	if (!elem) {
		return;
	}

	// this is a parameter to the service method
	// this will go up to server and come back populated
	var info = {
		Number: Math.PI
	};

	// these are the options for the Ajax request
	var options = {
		onSuccess: Example.results.success, // defined in Results.jbst
		onFailure: Example.asyncError,
		context: { elem: elem, foo: "bar" }
	};

	// call JSON-RPC service proxy objects with the
	// method args in order and add an options object at the end
	Example.Service.getInfo(
		info,
		options);

	// when the request completes, the appropriate callback will
	// get called with the return value and the context object
};

/*void*/ Example.callRestAction = function(/*DOM*/ elem) {
	if (!elem) {
		return;
	}

	// this is the parameter to the controller action
	// this will go up to server and come back populated
	var info = {
		Number: Math.PI
	};

	// these are the options for the Ajax request
	var options = {
		params: info,
		onSuccess: Example.results.success, // defined in Results.jbst
		onFailure: Example.asyncError,
		context: { elem: elem, foo: "bar" }
	};

	// call the REST Controller action with the
	// method args in order and add an options object at the end
	JsonFx.IO.sendJsonRequest(
		"/test/getInfo",
		options);

	// when the request completes, the appropriate callback will
	// get called with the return value and the context object
};
