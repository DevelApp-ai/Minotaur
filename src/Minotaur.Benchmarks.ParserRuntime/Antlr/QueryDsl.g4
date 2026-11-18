// Case-study analytics query DSL for the issue #127 parser-runtime benchmark.
// Mirrors the article's case study 2: SELECT ... FROM ... WHERE ... WINDOW.
grammar QueryDsl;

query : SELECT field FROM source WHERE WORD EQUALS value WINDOW NUMBER unit EOF ;

field  : WORD ;
source : WORD
       | LPAREN WORD RPAREN ;
value  : STRING
       | NUMBER
       | WORD ;
unit   : WORD ;

SELECT  : 'select' ;
FROM    : 'from' ;
WHERE   : 'where' ;
WINDOW  : 'window' ;
LPAREN  : '(' ;
RPAREN  : ')' ;
EQUALS  : '=' ;
STRING  : '"' (~["\r\n] | '\\' .)* '"' ;
NUMBER  : '-'? [0-9]+ ('.' [0-9]+)? ;
WORD    : [a-zA-Z_] [a-zA-Z0-9_.\-]* ;
WS      : [ \t\r\n]+ -> skip ;
