// Case-study multi-language template DSL for the issue #127 parser-runtime
// benchmark. Mirrors the article's case study 3: text with {{ expression }}
// holes plus <style>, <script> and {{sql}} ... {{/sql}} embedded blocks.
grammar TemplateDsl;

template : part+ EOF ;

part : SQL_BLOCK
     | STYLE_BLOCK
     | SCRIPT_BLOCK
     | EXPR
     | TEXT ;

SQL_BLOCK    : '{{sql}}' .*? '{{/sql}}' ;
STYLE_BLOCK  : '<style>' .*? '</style>' ;
SCRIPT_BLOCK : '<script>' .*? '</script>' ;
EXPR         : '{{' ~[}\r\n]+ '}}' ;
TEXT         : ~[ \t\r\n{<]+ ;
WS           : [ \t\r\n]+ -> skip ;
