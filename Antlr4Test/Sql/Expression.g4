grammar Expression;

import CommonLexer;

/*
 * Parser Rules
 */

expression
	: '(' expression ')'                           #ExpressionParenthesis
	| SYNTAX '(' expression ')'                    #Function
	| SYNTAX '(' expression ',' expression ')'     #BinaryFunction
	| expression operator = ('*' | '/') expression #Binary
	| expression operator = ('+' | '-') expression #Binary
	| NUMBER                                       #Number
	| STRING									   #String
	;