lexer grammar CommonLexer;

SYNTAX
	: [a-zA-Z_] [a-zA-Z0-9_]*
	;

ADD: '+';
SUB: '-';
MUL: '*';
DIV: '/';

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
   : [Ee] [+\-]? INT
   ;