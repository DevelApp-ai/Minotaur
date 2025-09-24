/**
 * CSS Grammar for Minotaur Embedded Parsing
 * 
 * This grammar defines CSS parsing rules for use within HTML <style> tags.
 * It supports modern CSS3+ features and provides integration points for HTML context.
 */

@Minotaur
@FormatType: Minotaur
@Version: 1.0.0
@Author: Minotaur Compiler-Compiler System
@Description: CSS grammar for embedded HTML parsing with CSS3+ support

grammar CSS {
    // Grammar metadata and configuration
    @Inheritable: true
    @ContextSensitive: true
    @EnableSymbolTable: true
    @EnableScopeTracking: true
    @EmbeddedLanguage: true
    @ParentContext: HTML
    
    // Start rule for embedded CSS
    start: stylesheet;
    
    // Stylesheet structure
    stylesheet: (charset_rule | import_rule | namespace_rule | media_rule | page_rule | font_face_rule | keyframes_rule | supports_rule | rule_set | WS | COMMENT)*;
    
    // At-rules
    charset_rule: '@charset' STRING ';';
    import_rule: '@import' (STRING | url_function) media_query_list? ';';
    namespace_rule: '@namespace' IDENTIFIER? (STRING | url_function) ';';
    
    // Media queries
    media_rule: '@media' media_query_list '{' rule_set* '}';
    media_query_list: media_query (',' media_query)*;
    media_query: ('only' | 'not')? media_type ('and' media_expression)*
               | media_expression ('and' media_expression)*;
    media_type: IDENTIFIER;
    media_expression: '(' media_feature (':' expression)? ')';
    media_feature: IDENTIFIER;
    
    // Page rules
    page_rule: '@page' page_selector? '{' declaration_list '}';
    page_selector: ':' ('left' | 'right' | 'first' | 'blank') | IDENTIFIER;
    
    // Font face rules
    font_face_rule: '@font-face' '{' declaration_list '}';
    
    // Keyframes rules
    keyframes_rule: '@keyframes' IDENTIFIER '{' keyframe_rule* '}';
    keyframe_rule: keyframe_selector '{' declaration_list '}';
    keyframe_selector: ('from' | 'to' | PERCENTAGE) (',' ('from' | 'to' | PERCENTAGE))*;
    
    // Supports rules (CSS3)
    supports_rule: '@supports' supports_condition '{' rule_set* '}';
    supports_condition: supports_negation | supports_conjunction | supports_disjunction | supports_declaration_condition;
    supports_negation: 'not' supports_condition;
    supports_conjunction: supports_condition 'and' supports_condition;
    supports_disjunction: supports_condition 'or' supports_condition;
    supports_declaration_condition: '(' declaration ')';
    
    // Rule sets
    rule_set: selector_list '{' declaration_list '}';
    
    // Selectors
    selector_list: selector (',' selector)*;
    selector: simple_selector_sequence (combinator simple_selector_sequence)*;
    
    combinator: '+' | '~' | '>' | WS;
    
    simple_selector_sequence: (type_selector | universal_selector)? 
                             (hash_selector | class_selector | attribute_selector | pseudo_selector)*;
    
    type_selector: IDENTIFIER | namespace_prefix? IDENTIFIER;
    universal_selector: namespace_prefix? '*';
    namespace_prefix: (IDENTIFIER | '*')? '|';
    
    hash_selector: '#' IDENTIFIER;
    class_selector: '.' IDENTIFIER;
    
    attribute_selector: '[' IDENTIFIER (attribute_operator (IDENTIFIER | STRING))? ']';
    attribute_operator: '=' | '~=' | '|=' | '^=' | '$=' | '*=';
    
    pseudo_selector: pseudo_class | pseudo_element;
    pseudo_class: ':' IDENTIFIER ('(' (IDENTIFIER | STRING | NUMBER | expression) ')')?;
    pseudo_element: '::' IDENTIFIER | ':' ('before' | 'after' | 'first-line' | 'first-letter');
    
    // Declarations
    declaration_list: declaration? (';' declaration?)*;
    declaration: property ':' expression priority?;
    property: IDENTIFIER;
    priority: '!' WS* 'important';
    
    // Expressions and values
    expression: term (operator term)*;
    operator: '/' | ',' | WS;
    
    term: unary_operator? (NUMBER | PERCENTAGE | LENGTH | EMS | EXS | ANGLE | TIME | FREQ | RESOLUTION)
        | STRING
        | IDENTIFIER
        | url_function
        | function_call
        | hexcolor
        | rgb_function
        | rgba_function
        | hsl_function
        | hsla_function
        | calc_function
        | var_function;
    
    unary_operator: '-' | '+';
    
    // Functions
    url_function: 'url(' (STRING | URL) ')';
    function_call: IDENTIFIER '(' expression_list? ')';
    expression_list: expression (',' expression)*;
    
    // Color functions
    rgb_function: 'rgb(' NUMBER ',' NUMBER ',' NUMBER ')';
    rgba_function: 'rgba(' NUMBER ',' NUMBER ',' NUMBER ',' NUMBER ')';
    hsl_function: 'hsl(' NUMBER ',' PERCENTAGE ',' PERCENTAGE ')';
    hsla_function: 'hsla(' NUMBER ',' PERCENTAGE ',' PERCENTAGE ',' NUMBER ')';
    
    // CSS3+ functions
    calc_function: 'calc(' calc_expression ')';
    calc_expression: calc_term (calc_operator calc_term)*;
    calc_term: NUMBER | PERCENTAGE | LENGTH | '(' calc_expression ')';
    calc_operator: '+' | '-' | '*' | '/';
    
    var_function: 'var(' '--' IDENTIFIER (',' expression)? ')';
    
    // Colors
    hexcolor: '#' [0-9a-fA-F] [0-9a-fA-F] [0-9a-fA-F] ([0-9a-fA-F] [0-9a-fA-F] [0-9a-fA-F])?;
    
    // HTML Integration (context awareness)
    @HTMLIntegration {
        // Validate selectors against HTML elements
        element_selector_validation: {
            validateElementExists(elementName);
            trackElementUsage(elementName, selectorType);
        }
        
        // Validate ID selectors against HTML IDs
        id_selector_validation: {
            validateIdExists(idName);
            trackIdUsage(idName, selectorType);
        }
        
        // Validate class selectors against HTML classes
        class_selector_validation: {
            validateClassExists(className);
            trackClassUsage(className, selectorType);
        }
        
        // Track CSS custom properties
        custom_property_tracking: {
            trackCustomProperty(propertyName, value);
            validateCustomPropertyUsage(propertyName);
        }
    }
    
    // Symbol table for CSS context
    @SymbolTable {
        selectors: Map<String, SelectorInfo>;
        properties: Map<String, PropertyInfo>;
        customProperties: Map<String, CustomPropertyInfo>;
        mediaQueries: List<MediaQueryInfo>;
        keyframes: Map<String, KeyframesInfo>;
        fontFaces: List<FontFaceInfo>;
        
        // HTML integration symbols
        elementReferences: Map<String, ElementReferenceInfo>;
        idReferences: Map<String, IdReferenceInfo>;
        classReferences: Map<String, ClassReferenceInfo>;
    }
    
    @ContextRules {
        // Selector validation rules
        onElementSelector: {
            validateElementInHTML(elementName);
            addElementReference(elementName, selectorInfo);
        }
        
        onIdSelector: {
            validateIdInHTML(idName);
            addIdReference(idName, selectorInfo);
        }
        
        onClassSelector: {
            validateClassInHTML(className);
            addClassReference(className, selectorInfo);
        }
        
        // Property validation rules
        onPropertyDeclaration: {
            validatePropertyName(propertyName);
            validatePropertyValue(propertyName, value);
            addPropertyInfo(propertyName, value, selectorContext);
        }
        
        // Custom property rules
        onCustomPropertyDeclaration: {
            addCustomProperty(propertyName, value);
        }
        
        onCustomPropertyUsage: {
            validateCustomPropertyExists(propertyName);
            trackCustomPropertyUsage(propertyName);
        }
        
        // Media query rules
        onMediaQuery: {
            validateMediaFeatures(mediaFeatures);
            addMediaQueryInfo(mediaQuery);
        }
    }
    
    // Lexical rules
    IDENTIFIER: [-]? [a-zA-Z_] [a-zA-Z0-9_-]*;
    
    NUMBER: [0-9]+ ('.' [0-9]+)?;
    PERCENTAGE: NUMBER '%';
    LENGTH: NUMBER ('px' | 'em' | 'rem' | 'ex' | 'ch' | 'vw' | 'vh' | 'vmin' | 'vmax' | 'cm' | 'mm' | 'in' | 'pt' | 'pc');
    EMS: NUMBER 'em';
    EXS: NUMBER 'ex';
    ANGLE: NUMBER ('deg' | 'rad' | 'grad' | 'turn');
    TIME: NUMBER ('s' | 'ms');
    FREQ: NUMBER ('Hz' | 'kHz');
    RESOLUTION: NUMBER ('dpi' | 'dpcm' | 'dppx');
    
    STRING: '"' (~["\\\r\n] | ESCAPE_SEQUENCE)* '"'
          | "'" (~['\\\r\n] | ESCAPE_SEQUENCE)* "'";
    
    URL: ~[ \t\r\n\f"'()\\]+;
    
    ESCAPE_SEQUENCE: '\\' [0-9a-fA-F] [0-9a-fA-F]? [0-9a-fA-F]? [0-9a-fA-F]? [0-9a-fA-F]? [0-9a-fA-F]? (' ' | '\t' | '\r' | '\n' | '\f')?
                   | '\\' ~[0-9a-fA-F\r\n\f];
    
    // Comments
    COMMENT: '/*' .*? '*/';
    
    // Whitespace
    WS: [ \t\r\n\f]+ -> skip;
    
    // Semantic actions for symbol table management
    @SemanticActions {
        onSelectorDeclaration(selector: String, specificity: int) {
            SelectorInfo selectorInfo = new SelectorInfo(selector, specificity, getCurrentPosition());
            symbolTable.selectors.put(selector, selectorInfo);
            
            // Parse selector components for HTML validation
            parseSelectorComponents(selector);
        }
        
        onPropertyDeclaration(property: String, value: String, selector: String) {
            PropertyInfo propertyInfo = new PropertyInfo(property, value, selector, getCurrentPosition());
            symbolTable.properties.put(property + "@" + selector, propertyInfo);
            
            // Validate property against CSS specification
            validateCSSProperty(property, value);
        }
        
        onCustomPropertyDeclaration(name: String, value: String) {
            CustomPropertyInfo customProp = new CustomPropertyInfo(name, value, getCurrentPosition());
            symbolTable.customProperties.put(name, customProp);
        }
        
        onElementSelectorReference(elementName: String) {
            // Cross-reference with HTML symbol table
            if (htmlSymbolTable.elements.containsKey(elementName)) {
                ElementReferenceInfo elementRef = new ElementReferenceInfo(elementName, getCurrentPosition());
                symbolTable.elementReferences.put(elementName, elementRef);
            } else {
                warning("CSS selector references non-existent HTML element: " + elementName);
            }
        }
        
        onIdSelectorReference(idName: String) {
            // Cross-reference with HTML symbol table
            if (htmlSymbolTable.ids.containsKey(idName)) {
                IdReferenceInfo idRef = new IdReferenceInfo(idName, getCurrentPosition());
                symbolTable.idReferences.put(idName, idRef);
            } else {
                warning("CSS selector references non-existent HTML ID: " + idName);
            }
        }
        
        onClassSelectorReference(className: String) {
            // Cross-reference with HTML symbol table
            if (htmlSymbolTable.classes.containsKey(className)) {
                ClassReferenceInfo classRef = new ClassReferenceInfo(className, getCurrentPosition());
                symbolTable.classReferences.put(className, classRef);
            } else {
                warning("CSS selector references non-existent HTML class: " + className);
            }
        }
        
        onMediaQueryDeclaration(mediaQuery: String) {
            MediaQueryInfo mediaInfo = new MediaQueryInfo(mediaQuery, getCurrentPosition());
            symbolTable.mediaQueries.add(mediaInfo);
        }
        
        onKeyframesDeclaration(name: String, keyframes: List<Keyframe>) {
            KeyframesInfo keyframesInfo = new KeyframesInfo(name, keyframes, getCurrentPosition());
            symbolTable.keyframes.put(name, keyframesInfo);
        }
    }
    
    // Error recovery rules
    @ErrorRecovery {
        strategy: synchronization;
        syncTokens: [';', '}', '{', '@media', '@keyframes', '@font-face', '@import'];
        maxErrors: 15;
        
        onMissingSemicolon() {
            warning("Missing semicolon in CSS declaration");
            insertToken(";");
        }
        
        onMissingBrace(expected: String) {
            warning("Missing " + expected + " in CSS rule");
            insertToken(expected);
        }
        
        onInvalidProperty(property: String) {
            warning("Invalid CSS property: " + property);
            skipToNextDeclaration();
        }
        
        onInvalidValue(property: String, value: String) {
            warning("Invalid value '" + value + "' for property '" + property + "'");
            skipToNextDeclaration();
        }
        
        onUnexpectedToken(token: String) {
            warning("Unexpected token in CSS: " + token);
            skipToNextSyncToken();
        }
    }
    
    // Performance optimizations
    @Optimizations {
        enableMemoization: true;
        enableLeftRecursionElimination: true;
        enableCommonSubexpressionElimination: true;
        cacheSize: 1500;
        
        // Optimize common CSS patterns
        optimizePatterns: [
            "selector_list",
            "declaration_list",
            "expression",
            "IDENTIFIER",
            "NUMBER"
        ];
        
        // Optimize selector parsing
        optimizeSelectorPatterns: [
            "simple_selector_sequence",
            "class_selector",
            "id_selector",
            "type_selector"
        ];
        
        // Optimize property validation
        optimizePropertyValidation: [
            "color_properties",
            "layout_properties",
            "typography_properties"
        ];
    }
    
    // CSS property validation
    @PropertyValidation {
        // Color properties
        color_properties: ['color', 'background-color', 'border-color', 'outline-color'];
        color_values: ['transparent', 'currentColor', 'inherit', 'initial', 'unset'];
        
        // Layout properties
        layout_properties: ['display', 'position', 'float', 'clear', 'visibility'];
        display_values: ['none', 'block', 'inline', 'inline-block', 'flex', 'grid', 'table'];
        position_values: ['static', 'relative', 'absolute', 'fixed', 'sticky'];
        
        // Typography properties
        typography_properties: ['font-family', 'font-size', 'font-weight', 'font-style', 'text-align'];
        font_weight_values: ['normal', 'bold', 'bolder', 'lighter', '100', '200', '300', '400', '500', '600', '700', '800', '900'];
        text_align_values: ['left', 'right', 'center', 'justify', 'start', 'end'];
        
        // Box model properties
        box_model_properties: ['margin', 'padding', 'border', 'width', 'height'];
        
        // Animation properties
        animation_properties: ['animation', 'transition', 'transform'];
        
        // Flexbox properties
        flexbox_properties: ['flex', 'flex-direction', 'flex-wrap', 'justify-content', 'align-items'];
        
        // Grid properties
        grid_properties: ['grid', 'grid-template', 'grid-area', 'grid-column', 'grid-row'];
    }
    
    // Integration with HTML context
    @HTMLContextIntegration {
        // Validate selectors against HTML elements
        validateSelectors: true;
        
        // Track CSS usage for HTML elements
        trackElementStyling: true;
        
        // Provide code completion for HTML elements and attributes
        enableHTMLCompletion: true;
        
        // Cross-language refactoring support
        enableCrossLanguageRefactoring: true;
        
        // CSS specificity calculation
        calculateSpecificity: true;
        
        // Unused CSS detection
        detectUnusedCSS: true;
    }
}

