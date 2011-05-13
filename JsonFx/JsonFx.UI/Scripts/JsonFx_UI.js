/*global JsonML, jQuery, window */
/*
	JsonFx_UI.js
	DOM utilities

	Created: 2006-11-11-1759
	Modified: 2009-11-07-1038

	Copyright (c)2006-2009 Stephen M. McKamey
	Distributed under an open-source license: http://jsonfx.net/license
*/

/* namespace JsonFx.UI */
var JsonFx;
if ("undefined" === typeof JsonFx) {
	JsonFx = {};
}
if ("undefined" === typeof JsonFx.UI) {
	JsonFx.UI = {};
}

/* dependency checks --------------------------------------------*/

if ("undefined" === typeof JsonML) {
	throw new Error("JsonFx.UI requires JsonML");
}

/* DOM utilities ------------------------------------------------*/

/*bool*/ JsonFx.UI.cancelEvent = function(/*Event*/ evt) {
	evt = evt || window.event;
	if (evt) {
		if (evt.stopPropagation) {
			evt.stopPropagation();
			evt.preventDefault();
		} else {
			try {
				evt.cancelBubble = true;
				evt.returnValue = false;
			} catch (ex) {
				// IE6
			}
		}
	}
	return false;
};

/*void*/ JsonFx.UI.addHandler = function(/*DOM*/ target, /*string*/ name, /*function*/ handler) {
	if ("string" === typeof handler) {
		/*jslint evil:true */
		handler = new Function(handler);
		/*jslint evil:false */
	}

	if ("function" !== typeof handler) {
		return;
	}

	if ("undefined" !== typeof jQuery) {
		jQuery(target).bind(name, handler);
	} else if (target.addEventListener) {
		// DOM Level 2 model for binding events
		target.addEventListener(name, handler, false);
	} else if (target.attachEvent) {
		// IE model for binding events
		target.attachEvent("on"+name, handler);
	} else {
		// DOM Level 0 model for binding events
		var old = target["on"+name];
		target["on"+name] = ("function" !== typeof old) ? handler :
			function(/*Event*/ e) { return (old.call(this, e) !== false) && (handler.call(this, e) !== false); };
	}
};

/*void*/ JsonFx.UI.removeHandler = function(/*DOM*/ target, /*string*/ name, /*function*/ handler) {
	if ("function" !== typeof handler) {
		// DOM Level 0 model for binding events
		target["on"+name] = null;
		return;
	}

	if ("undefined" !== typeof jQuery) {
		jQuery(target).unbind(name, handler);
	} else if (target.addEventListener) {
		// DOM Level 2 model for binding events
		target.removeEventListener(name, handler, false);
	} else if (target.attachEvent) {
		// IE model for binding events
		target.detachEvent("on"+name, handler);
	} else {
		// DOM Level 0 model for binding events
		target["on"+name] = null;
	}
};

/*bool*/ JsonFx.UI.clear = function(/*DOM*/ elem) {
	if (!elem) {
		return;
	}

	// unbind to prevent memory leaks
	if ("undefined" !== typeof JsonFx.Bindings) {
		JsonFx.Bindings.unbind(elem);
	}

	while (elem.lastChild) {
		elem.removeChild(elem.lastChild);
	}
};

/*bool*/ JsonFx.UI.hasClass = function(/*DOM*/ elem, /*string*/ cssClass) {
	if (!elem || !elem.className || !cssClass) {
		return false;
	}
	
	var css = elem.className.split(' ');
	for (var i=0; i < css.length; i++) {
		if (css[i] === cssClass) {
			return true;
		}
	}

	return false;
};

/*void*/ JsonFx.UI.addClass = function(/*DOM*/ elem, /*string*/ cssClass) {
	if (!elem || !cssClass) {
		return;
	}

	elem.className += ' '+cssClass;
};

/*void*/ JsonFx.UI.removeClass = function(/*DOM*/ elem, /*string*/ cssClass) {
	if (!elem || !cssClass) {
		return;
	}

	var css = elem.className.split(' ');
	for (var i=0; i < css.length; i++) {
		if (!css[i] || css[i] === cssClass) {
			css.splice(i, 1);
			i--;
		}
	}

	elem.className = css.join(" ");
};

/*DOM*/ JsonFx.UI.findParent = function(/*DOM*/ elem, /*string*/ cssClass, /*bool*/ skipRoot) {
	if (!cssClass) {
		return null;
	}

	if (skipRoot) {
		elem = elem.parentNode;
	}

	// search up the ancestors
	while (elem) {
		if (JsonFx.UI.hasClass(elem, cssClass)) {
			return elem;
		}

		elem = elem.parentNode;
	}
	return null;
};

/*DOM*/ JsonFx.UI.findChild = function(/*DOM*/ elem, /*string*/ cssClass, /*bool*/ skipRoot) {
	if (!cssClass) {
		return null;
	}

	// breadth-first search of all children
	var i, queue = [];
	
	if (skipRoot) {
		if (elem && elem.childNodes) {
			for (i=0; i<elem.childNodes.length; i++) {
				queue.push(elem.childNodes[i]);
			}
		}
	} else {
		queue.push(elem);
	}

	while (queue.length) {
		elem = queue.shift();
		if (JsonFx.UI.hasClass(elem, cssClass)) {
			return elem;
		}
		if (elem && elem.childNodes) {
			for (i=0; i<elem.childNodes.length; i++) {
				queue.push(elem.childNodes[i]);
			}
		}
	}
	return null;
};

/*DOM*/ JsonFx.UI.findPrev = function(/*DOM*/ elem, /*string*/ cssClass, /*bool*/ skipRoot) {
	if (!cssClass) {
		return null;
	}

	if (skipRoot) {
		elem = elem.previousSibling;
	}

	// search up siblings in order
	while (elem) {
		if (JsonFx.UI.hasClass(elem, cssClass)) {
			return elem;
		}
		elem = elem.previousSibling;
	}
	return null;
};

/*DOM*/ JsonFx.UI.findNext = function(/*DOM*/ elem, /*string*/ cssClass, /*bool*/ skipRoot) {
	if (!cssClass) {
		return null;
	}

	if (skipRoot) {
		elem = elem.nextSibling;
	}

	// search down siblings in order
	while (elem) {
		if (JsonFx.UI.hasClass(elem, cssClass)) {
			return elem;
		}
		elem = elem.nextSibling;
	}
	return null;
};

/* JsonML utilities ---------------------------------------------*/

/* deprecated */
/*DOM*/ JsonFx.UI.bind = function(/*JBST*/ jbst, /*JSON*/ data, /*int*/ index, /*int*/ count) {
	return JsonML.BST(jbst).bind(data, index, count);
};
