/* Configuration File Lexer - Flex */
/* Companion lexer for config.y */

%{
#include "config.tab.h"
#include <string.h>
#include <stdlib.h>
%}

%option noyywrap

%%

\[                      { return SECTION_START; }
\]                      { return SECTION_END; }
=                       { return EQUALS; }
\n                      { return NEWLINE; }
#.*                     { return COMMENT; }
;.*                     { return COMMENT; }

\"[^\"]*\"              { 
                          yylval.str = strdup(yytext + 1);  /* skip opening quote */
                          yylval.str[strlen(yylval.str) - 1] = '\0';  /* remove closing quote */
                          return STRING; 
                        }

[a-zA-Z_][a-zA-Z0-9_]*  { 
                          yylval.str = strdup(yytext); 
                          return IDENTIFIER; 
                        }

[0-9]+                  { 
                          yylval.str = strdup(yytext); 
                          return IDENTIFIER; 
                        }

[ \t]+                  { /* ignore whitespace */ }

.                       { printf("Unknown character: %c\n", yytext[0]); }

%%

