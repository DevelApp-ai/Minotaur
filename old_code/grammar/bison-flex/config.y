/* Configuration File Grammar - Bison/Yacc */
/* Parser for INI-style configuration files */

%{
#include <stdio.h>
#include <stdlib.h>
#include <string.h>

int yylex(void);
void yyerror(const char *s);

typedef struct {
    char *section;
    char *key;
    char *value;
} config_entry_t;

%}

%union {
    char *str;
}

%token <str> IDENTIFIER STRING
%token SECTION_START SECTION_END
%token EQUALS NEWLINE COMMENT

%%

config_file:
    /* empty */
    | config_file config_item
    ;

config_item:
    section
    | assignment
    | comment_line
    | empty_line
    ;

section:
    SECTION_START IDENTIFIER SECTION_END NEWLINE {
        printf("Section: [%s]\n", $2);
        free($2);
    }
    ;

assignment:
    IDENTIFIER EQUALS STRING NEWLINE {
        printf("  %s = %s\n", $1, $3);
        free($1);
        free($3);
    }
    | IDENTIFIER EQUALS IDENTIFIER NEWLINE {
        printf("  %s = %s\n", $1, $3);
        free($1);
        free($3);
    }
    ;

comment_line:
    COMMENT NEWLINE
    ;

empty_line:
    NEWLINE
    ;

%%

void yyerror(const char *s) {
    fprintf(stderr, "Parse error: %s\n", s);
}

int main() {
    printf("Configuration File Parser\n");
    printf("Parsing configuration...\n");
    return yyparse();
}

