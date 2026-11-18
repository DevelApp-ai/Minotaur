// Case-study configuration DSL for the issue #127 parser-runtime benchmark.
// Mirrors the article's Appendix A.1 configuration DSL: sections, key=value
// settings, string/number/bare-word values and comma-separated arrays.
grammar ConfigDsl;

config_file : section+ EOF ;

section : LBRACKET WORD RBRACKET setting* ;

setting : WORD EQUALS value ;

value : STRING
      | NUMBER
      | WORD
      | array ;

array : LBRACKET RBRACKET
      | LBRACKET value_list RBRACKET ;

value_list : value (COMMA value)* ;

LBRACKET  : '[' ;
RBRACKET  : ']' ;
COMMA     : ',' ;
EQUALS    : '=' ;
STRING    : '"' (~["\r\n] | '\\' .)* '"' ;
NUMBER    : '-'? [0-9]+ ('.' [0-9]+)? ;
WORD      : [a-zA-Z_] [a-zA-Z0-9_.\-]* ;
COMMENT   : '#' ~[\r\n]* -> skip ;
WS        : [ \t\r\n]+ -> skip ;
