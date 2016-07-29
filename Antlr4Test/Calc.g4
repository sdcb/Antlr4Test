grammar Calc;

root
	: (statement EOL)*
	;

statement
	: SYNTAX '=' expression     #Assign
	| SYNTAX '(' expression ')' #Call
	;

expression
	: '(' expression ')'                           #Parenthesis
	| SYNTAX '(' expression ')'                    #Function
	| SYNTAX '(' expression ',' expression ')'     #BinaryFunction
	| expression operator = ('*' | '/') expression #Binary
	| expression operator = ('+' | '-') expression #Binary
	| NUMBER                                       #Number
	| SYNTAX									   #Variable
	;

BlockComment
    :   '/*' .*? '*/'
        -> skip
    ;

LineComment
    :   '//' ~[\r\n]*
        -> skip
    ;

ADD: '+';
SUB: '-';
MUL: '*';
DIV: '/';
EOL: ';';

NUMBER
	: '-'? [0-9]+ ('.' [0-9]+)?
	;

SYNTAX
	: [a-zA-Z]? [a-zA-Z0-9]*
	;

WS
	: [ \r\n\t] + -> channel (HIDDEN)
	;