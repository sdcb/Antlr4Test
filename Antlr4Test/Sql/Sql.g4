grammar Sql;

/*
 * Parser Rules
 */

run
	: predicate
	;

predicate
	: SYNTAX IN '(' expression (',' expression)* ')' #Contains
	| SYNTAX ('=' | '!=') expression                 #SingleOperator
	| SYNTAX ('>' | '<' | '>=' | '<=') expression    #SingleOperator
	| SYNTAX BETWEEN expression AND expression       #Between
	| '(' predicate ')'                              #Parenthesis
	| NOT predicate                                  #Not
	| predicate AND predicate                        #AndOr
	| predicate OR predicate                         #AndOr
	;

expression
	: '(' expression ')'                           #ExpressionParenthesis
	| SYNTAX '(' expression ')'                    #Function
	| SYNTAX '(' expression ',' expression ')'     #BinaryFunction
	| expression operator = ('*' | '/') expression #Binary
	| expression operator = ('+' | '-') expression #Binary
	| NUMBER                                       #Number
	| STRING									   #String
	| DATE                                         #Date
	;

DATE
	: YEAR SEPARATOR MONTH SEPARATOR DAY
	;

fragment DIGIT
	: [0-9]
	;

fragment DAY
	: DIGIT? DIGIT
	;

fragment MONTH
	: DIGIT? DIGIT
	;

fragment YEAR
	: DIGIT DIGIT DIGIT DIGIT
	;

BETWEEN
	: B E T W E E N
	;

SEPARATOR
	: ('-'|'/')
	;

AND
	: A N D
	;

NOT
	: N O T
	;

OR
	: O R
	;

IN
	: I N
	;

SYNTAX
	: [a-zA-Z_] [a-zA-Z0-9_]*
	;

STRING
   : '"' (EscapedChar | RegularChar)* '"'
   ;

fragment RegularChar
	: ~ ["\\]
	;

fragment EscapedChar
	: '\\' ["\\/bfnrt]
	;

WS
	:	' ' -> channel(HIDDEN)
	;

BlockComment
    : '/*' .*? '*/'
      -> skip
    ;

LineComment
    : '//' ~[\r\n]*
      -> skip
    ;

NUMBER
   : '-'? INT '.' [0-9] + EXP? 
   | '-'? INT EXP 
   | '-'? INT
   ;
fragment INT
   : '0' | [1-9] [0-9]*
   ;
// no leading zeros
fragment EXP
   : E [+\-]? INT
   ;

fragment A:('a'|'A');
fragment B:('b'|'B');
fragment C:('c'|'C');
fragment D:('d'|'D');
fragment E:('e'|'E');
fragment F:('f'|'F');
fragment G:('g'|'G');
fragment H:('h'|'H');
fragment I:('i'|'I');
fragment J:('j'|'J');
fragment K:('k'|'K');
fragment L:('l'|'L');
fragment M:('m'|'M');
fragment N:('n'|'N');
fragment O:('o'|'O');
fragment P:('p'|'P');
fragment Q:('q'|'Q');
fragment R:('r'|'R');
fragment S:('s'|'S');
fragment T:('t'|'T');
fragment U:('u'|'U');
fragment V:('v'|'V');
fragment W:('w'|'W');
fragment X:('x'|'X');
fragment Y:('y'|'Y');
fragment Z:('z'|'Z');