/* Calculator Grammar - Bison/Yacc */
/* Based on classic calculator examples from GNU Bison manual */

%{
#include <stdio.h>
#include <stdlib.h>
#include <math.h>

int yylex(void);
void yyerror(char const *);
%}

%define api.value.type {double}
%token NUM
%left '-' '+'
%left '*' '/'
%precedence NEG   /* negation--unary minus */
%right '^'        /* exponentiation */

%%
input:
  %empty
| input line
;

line:
  '\n'
| exp '\n'  { printf ("%.10g\n", $1); }
;

exp:
  NUM                { $$ = $1;         }
| exp '+' exp        { $$ = $1 + $3;    }
| exp '-' exp        { $$ = $1 - $3;    }
| exp '*' exp        { $$ = $1 * $3;    }
| exp '/' exp        { $$ = $1 / $3;    }
| '-' exp  %prec NEG { $$ = -$2;        }
| exp '^' exp        { $$ = pow ($1, $3); }
| '(' exp ')'        { $$ = $2;         }
;
%%

void yyerror (char const *s)
{
  fprintf (stderr, "%s\n", s);
}

int main (void)
{
  return yyparse ();
}

