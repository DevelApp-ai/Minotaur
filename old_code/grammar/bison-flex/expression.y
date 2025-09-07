/* Expression Grammar - Bison/Yacc */
/* Simple infix expression evaluator */

%{
#include <stdio.h>
#include <stdlib.h>

int yylex(void);
void yyerror(const char *s);
%}

%union {
    int ival;
    double dval;
}

%token <ival> INTEGER
%token <dval> DOUBLE
%token PLUS MINUS MULTIPLY DIVIDE
%token LPAREN RPAREN
%token NEWLINE

%type <dval> expression term factor

%left PLUS MINUS
%left MULTIPLY DIVIDE
%right UMINUS

%%

program:
    /* empty */
    | program statement
    ;

statement:
    NEWLINE
    | expression NEWLINE { printf("Result: %g\n", $1); }
    ;

expression:
    term
    | expression PLUS term      { $$ = $1 + $3; }
    | expression MINUS term     { $$ = $1 - $3; }
    ;

term:
    factor
    | term MULTIPLY factor      { $$ = $1 * $3; }
    | term DIVIDE factor        { 
        if ($3 == 0) {
            yyerror("Division by zero");
            $$ = 0;
        } else {
            $$ = $1 / $3;
        }
    }
    ;

factor:
    INTEGER                     { $$ = (double)$1; }
    | DOUBLE                    { $$ = $1; }
    | LPAREN expression RPAREN  { $$ = $2; }
    | MINUS factor %prec UMINUS { $$ = -$2; }
    ;

%%

void yyerror(const char *s) {
    fprintf(stderr, "Error: %s\n", s);
}

int main() {
    printf("Simple Expression Calculator\n");
    printf("Enter expressions (Ctrl+D to exit):\n");
    return yyparse();
}

