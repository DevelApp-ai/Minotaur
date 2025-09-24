/**
 * JavaScript Grammar for Minotaur Embedded Parsing
 * 
 * This grammar defines JavaScript parsing rules for use within HTML <script> tags.
 * It supports modern ES6+ features and provides integration points for HTML context.
 */

@Minotaur
@FormatType: Minotaur
@Version: 1.0.0
@Author: Minotaur Compiler-Compiler System
@Description: JavaScript grammar for embedded HTML parsing with ES6+ support

grammar JavaScript {
    // Grammar metadata and configuration
    @Inheritable: true
    @ContextSensitive: true
    @EnableSymbolTable: true
    @EnableScopeTracking: true
    @EmbeddedLanguage: true
    @ParentContext: HTML
    
    // Start rule for embedded JavaScript
    start: program;
    
    // Program structure
    program: statement_list?;
    statement_list: statement (statement)*;
    
    // Statements
    statement: block_statement
             | variable_statement
             | empty_statement
             | expression_statement
             | if_statement
             | iteration_statement
             | continue_statement
             | break_statement
             | return_statement
             | with_statement
             | labelled_statement
             | switch_statement
             | throw_statement
             | try_statement
             | function_declaration
             | class_declaration
             | import_statement
             | export_statement;
    
    // Block statement
    block_statement: '{' statement_list? '}';
    
    // Variable declarations
    variable_statement: variable_declaration ';'?;
    variable_declaration: ('var' | 'let' | 'const') variable_declaration_list;
    variable_declaration_list: variable_declarator (',' variable_declarator)*;
    variable_declarator: IDENTIFIER ('=' assignment_expression)?;
    
    // Empty statement
    empty_statement: ';';
    
    // Expression statement
    expression_statement: expression ';'?;
    
    // If statement
    if_statement: 'if' '(' expression ')' statement ('else' statement)?;
    
    // Iteration statements
    iteration_statement: do_statement
                       | while_statement
                       | for_statement
                       | for_in_statement
                       | for_of_statement;
    
    do_statement: 'do' statement 'while' '(' expression ')' ';'?;
    while_statement: 'while' '(' expression ')' statement;
    for_statement: 'for' '(' (variable_declaration | expression)? ';' expression? ';' expression? ')' statement;
    for_in_statement: 'for' '(' ('var' | 'let' | 'const')? IDENTIFIER 'in' expression ')' statement;
    for_of_statement: 'for' '(' ('var' | 'let' | 'const')? IDENTIFIER 'of' expression ')' statement;
    
    // Control flow statements
    continue_statement: 'continue' IDENTIFIER? ';'?;
    break_statement: 'break' IDENTIFIER? ';'?;
    return_statement: 'return' expression? ';'?;
    
    // With statement (deprecated but still valid)
    with_statement: 'with' '(' expression ')' statement;
    
    // Labelled statement
    labelled_statement: IDENTIFIER ':' statement;
    
    // Switch statement
    switch_statement: 'switch' '(' expression ')' case_block;
    case_block: '{' case_clauses? '}';
    case_clauses: case_clause+;
    case_clause: ('case' expression | 'default') ':' statement_list?;
    
    // Exception handling
    throw_statement: 'throw' expression ';'?;
    try_statement: 'try' block_statement (catch_clause finally_clause? | finally_clause);
    catch_clause: 'catch' '(' IDENTIFIER ')' block_statement;
    finally_clause: 'finally' block_statement;
    
    // Function declaration
    function_declaration: 'function' IDENTIFIER '(' parameter_list? ')' block_statement;
    parameter_list: parameter (',' parameter)*;
    parameter: IDENTIFIER ('=' assignment_expression)?;
    
    // Arrow functions
    arrow_function: (IDENTIFIER | '(' parameter_list? ')') '=>' (expression | block_statement);
    
    // Class declaration (ES6+)
    class_declaration: 'class' IDENTIFIER ('extends' IDENTIFIER)? class_body;
    class_body: '{' class_element* '}';
    class_element: method_definition | field_definition;
    method_definition: ('static')? ('get' | 'set')? IDENTIFIER '(' parameter_list? ')' block_statement;
    field_definition: ('static')? IDENTIFIER ('=' assignment_expression)? ';'?;
    
    // Import/Export (ES6 modules)
    import_statement: 'import' import_clause 'from' STRING ';'?;
    import_clause: IDENTIFIER | '{' import_specifier_list '}' | IDENTIFIER ',' '{' import_specifier_list '}';
    import_specifier_list: import_specifier (',' import_specifier)*;
    import_specifier: IDENTIFIER ('as' IDENTIFIER)?;
    
    export_statement: 'export' (export_declaration | export_clause 'from' STRING ';'?);
    export_declaration: variable_declaration | function_declaration | class_declaration;
    export_clause: '{' export_specifier_list '}';
    export_specifier_list: export_specifier (',' export_specifier)*;
    export_specifier: IDENTIFIER ('as' IDENTIFIER)?;
    
    // Expressions
    expression: assignment_expression;
    
    assignment_expression: conditional_expression
                         | left_hand_side_expression assignment_operator assignment_expression;
    
    assignment_operator: '=' | '*=' | '/=' | '%=' | '+=' | '-=' | '<<=' | '>>=' | '>>>=' | '&=' | '^=' | '|=';
    
    conditional_expression: logical_or_expression ('?' assignment_expression ':' assignment_expression)?;
    
    logical_or_expression: logical_and_expression ('||' logical_and_expression)*;
    logical_and_expression: bitwise_or_expression ('&&' bitwise_or_expression)*;
    bitwise_or_expression: bitwise_xor_expression ('|' bitwise_xor_expression)*;
    bitwise_xor_expression: bitwise_and_expression ('^' bitwise_and_expression)*;
    bitwise_and_expression: equality_expression ('&' equality_expression)*;
    equality_expression: relational_expression (('==' | '!=' | '===' | '!==') relational_expression)*;
    relational_expression: shift_expression (('<' | '>' | '<=' | '>=' | 'instanceof' | 'in') shift_expression)*;
    shift_expression: additive_expression (('<<' | '>>' | '>>>') additive_expression)*;
    additive_expression: multiplicative_expression (('+' | '-') multiplicative_expression)*;
    multiplicative_expression: unary_expression (('*' | '/' | '%') unary_expression)*;
    
    unary_expression: postfix_expression
                    | ('delete' | 'void' | 'typeof' | '++' | '--' | '+' | '-' | '~' | '!') unary_expression;
    
    postfix_expression: left_hand_side_expression ('++' | '--')?;
    
    left_hand_side_expression: new_expression | call_expression;
    
    new_expression: member_expression | 'new' new_expression;
    
    call_expression: member_expression arguments
                   | call_expression arguments
                   | call_expression '[' expression ']'
                   | call_expression '.' IDENTIFIER;
    
    member_expression: primary_expression
                     | member_expression '[' expression ']'
                     | member_expression '.' IDENTIFIER
                     | 'new' member_expression arguments;
    
    primary_expression: 'this'
                      | IDENTIFIER
                      | literal
                      | array_literal
                      | object_literal
                      | '(' expression ')'
                      | function_expression
                      | arrow_function
                      | template_literal;
    
    // Function expression
    function_expression: 'function' IDENTIFIER? '(' parameter_list? ')' block_statement;
    
    // Arguments
    arguments: '(' argument_list? ')';
    argument_list: assignment_expression (',' assignment_expression)*;
    
    // Literals
    literal: null_literal
           | boolean_literal
           | numeric_literal
           | string_literal
           | regular_expression_literal;
    
    null_literal: 'null';
    boolean_literal: 'true' | 'false';
    numeric_literal: DECIMAL_LITERAL | HEX_LITERAL | OCTAL_LITERAL | BINARY_LITERAL;
    string_literal: STRING;
    regular_expression_literal: REGEX;
    
    // Array literal
    array_literal: '[' element_list? ']';
    element_list: assignment_expression (',' assignment_expression)*;
    
    // Object literal
    object_literal: '{' property_list? '}';
    property_list: property (',' property)*;
    property: property_name ':' assignment_expression
            | 'get' property_name '(' ')' block_statement
            | 'set' property_name '(' IDENTIFIER ')' block_statement
            | IDENTIFIER  // Shorthand property
            | '[' expression ']' ':' assignment_expression;  // Computed property
    property_name: IDENTIFIER | STRING | numeric_literal;
    
    // Template literals (ES6+)
    template_literal: '`' template_element* '`';
    template_element: TEMPLATE_CHARS | '${' expression '}';
    
    // DOM Integration (HTML context awareness)
    @HTMLIntegration {
        // Special handling for document object
        document_reference: 'document' '.' document_method;
        document_method: 'getElementById' '(' STRING ')'
                       | 'getElementsByClassName' '(' STRING ')'
                       | 'getElementsByTagName' '(' STRING ')'
                       | 'querySelector' '(' STRING ')'
                       | 'querySelectorAll' '(' STRING ')'
                       | 'createElement' '(' STRING ')';
        
        // Window object methods
        window_reference: ('window' '.')? window_method;
        window_method: 'alert' '(' expression ')'
                     | 'confirm' '(' expression ')'
                     | 'prompt' '(' expression (',' expression)? ')'
                     | 'setTimeout' '(' expression ',' expression ')'
                     | 'setInterval' '(' expression ',' expression ')';
    }
    
    // Symbol table for JavaScript context
    @SymbolTable {
        variables: Map<String, VariableInfo>;
        functions: Map<String, FunctionInfo>;
        classes: Map<String, ClassInfo>;
        imports: Map<String, ImportInfo>;
        exports: Set<String>;
        scopes: Stack<ScopeInfo>;
        
        // HTML integration symbols
        domReferences: Map<String, DOMElementInfo>;
        eventHandlers: Map<String, EventHandlerInfo>;
    }
    
    @ContextRules {
        // Variable scoping rules
        onVariableDeclaration: {
            addToCurrentScope(variableName, variableInfo);
        }
        
        onFunctionDeclaration: {
            addToCurrentScope(functionName, functionInfo);
            pushScope(functionScope);
        }
        
        onBlockStart: {
            pushScope(blockScope);
        }
        
        onBlockEnd: {
            popScope();
        }
        
        // DOM integration rules
        onDOMReference: {
            validateDOMElement(elementId);
            trackDOMUsage(elementId, operation);
        }
        
        onEventHandler: {
            validateEventType(eventType);
            trackEventHandler(elementId, eventType, handlerFunction);
        }
    }
    
    // Lexical rules
    IDENTIFIER: [a-zA-Z_$][a-zA-Z0-9_$]*;
    
    DECIMAL_LITERAL: [0-9]+ ('.' [0-9]+)? ([eE] [+-]? [0-9]+)?;
    HEX_LITERAL: '0' [xX] [0-9a-fA-F]+;
    OCTAL_LITERAL: '0' [oO] [0-7]+;
    BINARY_LITERAL: '0' [bB] [01]+;
    
    STRING: '"' (~["\\\r\n] | ESCAPE_SEQUENCE)* '"'
          | "'" (~['\\\r\n] | ESCAPE_SEQUENCE)* "'";
    
    ESCAPE_SEQUENCE: '\\' ['"\\bfnrtv]
                   | '\\' [0-7] [0-7]? [0-7]?
                   | '\\x' [0-9a-fA-F] [0-9a-fA-F]
                   | '\\u' [0-9a-fA-F] [0-9a-fA-F] [0-9a-fA-F] [0-9a-fA-F];
    
    REGEX: '/' (~[/\\\r\n] | ESCAPE_SEQUENCE)+ '/' [gimuy]*;
    
    TEMPLATE_CHARS: (~[`$\\] | ESCAPE_SEQUENCE | '$' ~[{])+;
    
    // Comments
    SINGLE_LINE_COMMENT: '//' ~[\r\n]*;
    MULTI_LINE_COMMENT: '/*' .*? '*/';
    
    // Whitespace
    WS: [ \t\r\n]+ -> skip;
    
    // Semantic actions for symbol table management
    @SemanticActions {
        onVariableDeclaration(name: String, type: String, value: Expression) {
            VariableInfo variable = new VariableInfo(name, type, value, getCurrentScope());
            symbolTable.variables.put(name, variable);
        }
        
        onFunctionDeclaration(name: String, parameters: List<Parameter>, body: Statement) {
            FunctionInfo function = new FunctionInfo(name, parameters, body, getCurrentScope());
            symbolTable.functions.put(name, function);
        }
        
        onClassDeclaration(name: String, superClass: String, methods: List<Method>) {
            ClassInfo classInfo = new ClassInfo(name, superClass, methods, getCurrentScope());
            symbolTable.classes.put(name, classInfo);
        }
        
        onDOMElementAccess(elementId: String, operation: String) {
            // Cross-reference with HTML symbol table
            if (htmlSymbolTable.ids.containsKey(elementId)) {
                DOMElementInfo domInfo = new DOMElementInfo(elementId, operation, getCurrentPosition());
                symbolTable.domReferences.put(elementId, domInfo);
            } else {
                warning("DOM element not found in HTML: " + elementId);
            }
        }
        
        onEventHandlerAssignment(elementId: String, eventType: String, handler: String) {
            EventHandlerInfo eventInfo = new EventHandlerInfo(elementId, eventType, handler, getCurrentPosition());
            symbolTable.eventHandlers.put(elementId + ":" + eventType, eventInfo);
        }
    }
    
    // Error recovery rules
    @ErrorRecovery {
        strategy: synchronization;
        syncTokens: [';', '}', '{', 'function', 'var', 'let', 'const', 'if', 'for', 'while'];
        maxErrors: 20;
        
        onMissingSemicolon() {
            // JavaScript automatic semicolon insertion
            insertToken(";");
        }
        
        onMissingBrace(expected: String) {
            warning("Missing " + expected);
            insertToken(expected);
        }
        
        onUnexpectedToken(token: String) {
            warning("Unexpected token: " + token);
            skipToNextSyncToken();
        }
    }
    
    // Performance optimizations
    @Optimizations {
        enableMemoization: true;
        enableLeftRecursionElimination: true;
        enableCommonSubexpressionElimination: true;
        cacheSize: 2000;
        
        // Optimize common JavaScript patterns
        optimizePatterns: [
            "expression*",
            "statement+",
            "IDENTIFIER",
            "assignment_expression",
            "function_declaration"
        ];
        
        // Optimize DOM access patterns
        optimizeDOMPatterns: [
            "document.getElementById",
            "document.querySelector",
            "element.addEventListener"
        ];
    }
    
    // Integration with HTML context
    @HTMLContextIntegration {
        // Validate DOM element references against HTML symbol table
        validateDOMReferences: true;
        
        // Track event handler assignments
        trackEventHandlers: true;
        
        // Provide code completion for HTML elements
        enableHTMLCompletion: true;
        
        // Cross-language refactoring support
        enableCrossLanguageRefactoring: true;
    }
}

