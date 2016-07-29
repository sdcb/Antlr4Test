grammar Calc;

root
	: expression
	;

expression
	: '(' expression ')'                           #Parenthesis
	| FUNC '(' expression ')'                      #Function
	| FUNC '(' expression ',' expression ')'       #BinaryFunction
	| expression operator = ('*' | '/') expression #Binary
	| expression operator = ('+' | '-') expression #Binary
	| NUMBER                                       #Number
	;

ADD: '+';
SUB: '-';
MUL: '*';
DIV: '/';

NUMBER
	: '-'? [0-9]+ ('.' [0-9]+)?
	;

FUNC
	: [a-zA-Z]? [a-zA-Z0-9]*
	;

//BINFUNC
//	: [a-zA-Z]? [a-zA-Z0-9]*
//	;

WS
	: [ \r\n\t] + -> channel (HIDDEN)
	;