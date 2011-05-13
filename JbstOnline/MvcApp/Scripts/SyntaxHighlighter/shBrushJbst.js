/* JBST brush for SyntaxHighlighter */

SyntaxHighlighter.brushes.JBST = function() {

	/** <%@ ... %> tags */
	var jbstDeclarationTags = /^[@][\s\S]*?$/gm;

	/** <%$ ... %> tags */
	var jbstExtensionTags = /^[$][\s\S]*?$/gm;

	/** <% %> and <script></script> tags */
	var jbstScriptTags = {
		left: /((&lt;|<)%)|(&lt;|<)\s*script.*?(&gt;|>)/gi,
		right: /%(&gt;|>)|((&lt;|<)\/\s*script\s*(&gt;|>))/gi
	};

	/** <%-- --%> tags */
	var multiLineJbstComments = /(^--[\s\S]*?--$)/gm;

	var keywords =
		// ES5 literals
		"null false true " +

		// ES5 keywords
		"break case catch continue debugger default delete do else finally for function " +
		"if in instanceof new return switch this throw try typeof var void while with";

	var intrinsics =
		// ES5 intrinsic objects
		"Array Boolean Date Error Function JSON Math Number Object RegExp String";

	this.regexList = [
		{ regex: SyntaxHighlighter.regexLib.singleLineCComments,	css: "comments" },	// one line comments
		{ regex: SyntaxHighlighter.regexLib.multiLineCComments,		css: "comments" },	// multiline comments
		{ regex: multiLineJbstComments,								css: "comments" },	// multiline comments
		{ regex: jbstDeclarationTags,								css: "color3" },	// JBST extensions
		{ regex: jbstExtensionTags,									css: "color2" },	// JBST extensions
		{ regex: SyntaxHighlighter.regexLib.doubleQuotedString,		css: "string" },	// double quoted strings
		{ regex: SyntaxHighlighter.regexLib.singleQuotedString,		css: "string" },	// single quoted strings
		{ regex: new RegExp(this.getKeywords(keywords), 'gm'),		css: "keyword" },	// keywords
		{ regex: new RegExp(this.getKeywords(intrinsics), 'gm'),	css: "constants" }	// intrinsic objects
	];

	this.forHtmlScript(jbstScriptTags);
};

SyntaxHighlighter.brushes.JBST.prototype	= new SyntaxHighlighter.Highlighter();
SyntaxHighlighter.brushes.JBST.aliases		= ['jbst'];
