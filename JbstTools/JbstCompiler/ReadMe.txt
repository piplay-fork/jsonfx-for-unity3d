JbstCompiler.exe

A simple console adapter which allows command line compilation of JBST controls.

NOTE: this requires JsonFx to run.  Download the binaries from http://jsonfx.net/download
and place into the /External/JsonFx/ folder.
Updating the JsonFx binaries is just as easy as replacing the DLLs with a new version.

JsonML+BST Template Compiler usage:

JbstCompiler.exe /IN:file [ /OUT:file ] [ /INFO:copyright ] [ /TIME:timeFormat ] [ /WARNING ]

	/IN:		Input File Path
	/OUT:		Output File Path
	/INFO:		Copyright label
	/TIME:		TimeStamp Format
	/WARNING	Syntax issues reported as warnings
	/PRETTY		Pretty-Print the output (default is compact)

Examples:
	JbstCompiler.exe /IN:myTemplate.jbst /OUT:compiled/myTemplate.js /INFO:"(c)2009 My Template, Inc." /TIME:"'Compiled 'yyyy-MM-dd @ HH:mm"
	JbstCompiler.exe /DIR:myTemplates/ /INFO:"(c)2009 My Template, Inc." /TIME:"'Compiled 'yyyy-MM-dd @ HH:mm"
