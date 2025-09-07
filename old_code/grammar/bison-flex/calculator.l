/* Calculator Lexer - Flex */
/* Companion lexer for calculator.y */

%{
#include <stdlib.h>
%}

%option noyywrap

%%
[0-9]+\.?[0-9]*  { yylval = atof(yytext); return NUM; }
[-+*/^()\n]      { return yytext[0]; }
[ \t]            { /* ignore whitespace */ }
.                { /* ignore other characters */ }
%%

