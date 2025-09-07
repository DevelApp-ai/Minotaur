/* Expression Lexer - Flex */
/* Companion lexer for expression.y */

%{
#include "expression.tab.h"
#include <stdlib.h>
%}

%option noyywrap

%%

[0-9]+          { yylval.ival = atoi(yytext); return INTEGER; }
[0-9]+\.[0-9]+  { yylval.dval = atof(yytext); return DOUBLE; }
"+"             { return PLUS; }
"-"             { return MINUS; }
"*"             { return MULTIPLY; }
"/"             { return DIVIDE; }
"("             { return LPAREN; }
")"             { return RPAREN; }
\n              { return NEWLINE; }
[ \t]           { /* ignore whitespace */ }
.               { printf("Unknown character: %s\n", yytext); }

%%

