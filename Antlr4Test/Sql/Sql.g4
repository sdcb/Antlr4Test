grammar Sql;

import Expression;

/*
 * Parser Rules
 */

program
	: predicate
	;

predicate
	: '(' predicate ')'                                             #Parenthesis
	| SYNTAX operator = ('=' | '!=') expression                     #Equal
	| SYNTAX operator = ('IN' | 'NOT IN') '(' (expression ',')* ')' #Contains
	| SYNTAX operator = ('>' | '<' | '>=' | '<=') expression        #Compair
	| SYNTAX 'BETWEEN' expression 'AND' expression                  #Between
	| predicate 'AND' predicate                                     #And
	| predicate 'OR' predicate                                      #Or
	;