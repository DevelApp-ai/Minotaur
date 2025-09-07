/**
 * Base HTML Grammar for Minotaur
 * 
 * This grammar defines the core HTML parsing rules that will serve as the foundation
 * for the embedded grammar system. It includes modern HTML5 features and provides
 * extension points for embedded languages.
 */

@Minotaur
@FormatType: Minotaur
@Version: 1.0.0
@Author: Minotaur Compiler-Compiler System
@Description: Base HTML grammar with support for embedded language extension points

grammar HTMLBase {
    // Grammar metadata and configuration
    @Inheritable: true
    @ContextSensitive: true
    @EnableSymbolTable: true
    @EnableScopeTracking: true
    
    // Start rule
    start: document;
    
    // Document structure
    document: doctype? html_element;
    
    doctype: '<!DOCTYPE' WS+ 'html' WS* '>';
    
    html_element: '<html' attributes? '>' 
                  head_element? 
                  body_element? 
                  '</html>';
    
    head_element: '<head' attributes? '>' 
                  head_content* 
                  '</head>';
    
    body_element: '<body' attributes? '>' 
                  body_content* 
                  '</body>';
    
    // Head content elements
    head_content: title_element
                | meta_element
                | link_element
                | style_element
                | script_element
                | base_element
                | comment
                | WS;
    
    title_element: '<title' attributes? '>' text_content '</title>';
    meta_element: '<meta' attributes '/>' | '<meta' attributes '>';
    link_element: '<link' attributes '/>' | '<link' attributes '>';
    base_element: '<base' attributes '/>' | '<base' attributes '>';
    
    // Style element (extension point for CSS)
    @ExtensionPoint: CSS
    @ContextSwitch: CSS
    style_element: '<style' attributes? '>' 
                   @EmbeddedContent(CSS) style_content 
                   '</style>';
    
    // Script element (extension point for JavaScript)
    @ExtensionPoint: JavaScript
    @ContextSwitch: JavaScript
    script_element: '<script' attributes? '>' 
                    @EmbeddedContent(JavaScript) script_content 
                    '</script>'
                  | '<script' attributes '/>';
    
    // Body content elements
    body_content: block_element
                | inline_element
                | text_content
                | comment
                | WS;
    
    // Block-level elements
    block_element: div_element
                 | p_element
                 | h1_element | h2_element | h3_element | h4_element | h5_element | h6_element
                 | ul_element | ol_element
                 | table_element
                 | form_element
                 | section_element | article_element | aside_element | nav_element
                 | header_element | footer_element | main_element
                 | blockquote_element | pre_element
                 | hr_element | br_element
                 | script_element
                 | style_element;
    
    // Inline elements
    inline_element: span_element
                  | a_element
                  | img_element
                  | input_element
                  | button_element
                  | label_element
                  | select_element
                  | textarea_element
                  | strong_element | em_element | b_element | i_element
                  | code_element | kbd_element | samp_element | var_element
                  | small_element | sub_element | sup_element
                  | mark_element | del_element | ins_element
                  | time_element | abbr_element | dfn_element;
    
    // Common HTML elements
    div_element: '<div' attributes? '>' element_content* '</div>';
    span_element: '<span' attributes? '>' element_content* '</span>';
    p_element: '<p' attributes? '>' element_content* '</p>';
    
    // Heading elements
    h1_element: '<h1' attributes? '>' element_content* '</h1>';
    h2_element: '<h2' attributes? '>' element_content* '</h2>';
    h3_element: '<h3' attributes? '>' element_content* '</h3>';
    h4_element: '<h4' attributes? '>' element_content* '</h4>';
    h5_element: '<h5' attributes? '>' element_content* '</h5>';
    h6_element: '<h6' attributes? '>' element_content* '</h6>';
    
    // List elements
    ul_element: '<ul' attributes? '>' li_element* '</ul>';
    ol_element: '<ol' attributes? '>' li_element* '</ol>';
    li_element: '<li' attributes? '>' element_content* '</li>';
    
    // Table elements
    table_element: '<table' attributes? '>' table_content* '</table>';
    table_content: thead_element | tbody_element | tfoot_element | tr_element | caption_element;
    thead_element: '<thead' attributes? '>' tr_element* '</thead>';
    tbody_element: '<tbody' attributes? '>' tr_element* '</tbody>';
    tfoot_element: '<tfoot' attributes? '>' tr_element* '</tfoot>';
    tr_element: '<tr' attributes? '>' (td_element | th_element)* '</tr>';
    td_element: '<td' attributes? '>' element_content* '</td>';
    th_element: '<th' attributes? '>' element_content* '</th>';
    caption_element: '<caption' attributes? '>' element_content* '</caption>';
    
    // Form elements
    form_element: '<form' attributes? '>' form_content* '</form>';
    form_content: input_element | textarea_element | select_element | button_element 
                | label_element | fieldset_element | legend_element | element_content;
    
    input_element: '<input' attributes '/>' | '<input' attributes '>';
    textarea_element: '<textarea' attributes? '>' text_content '</textarea>';
    button_element: '<button' attributes? '>' element_content* '</button>';
    label_element: '<label' attributes? '>' element_content* '</label>';
    select_element: '<select' attributes? '>' option_element* '</select>';
    option_element: '<option' attributes? '>' text_content '</option>';
    fieldset_element: '<fieldset' attributes? '>' form_content* '</fieldset>';
    legend_element: '<legend' attributes? '>' element_content* '</legend>';
    
    // Semantic HTML5 elements
    section_element: '<section' attributes? '>' element_content* '</section>';
    article_element: '<article' attributes? '>' element_content* '</article>';
    aside_element: '<aside' attributes? '>' element_content* '</aside>';
    nav_element: '<nav' attributes? '>' element_content* '</nav>';
    header_element: '<header' attributes? '>' element_content* '</header>';
    footer_element: '<footer' attributes? '>' element_content* '</footer>';
    main_element: '<main' attributes? '>' element_content* '</main>';
    
    // Text formatting elements
    strong_element: '<strong' attributes? '>' element_content* '</strong>';
    em_element: '<em' attributes? '>' element_content* '</em>';
    b_element: '<b' attributes? '>' element_content* '</b>';
    i_element: '<i' attributes? '>' element_content* '</i>';
    code_element: '<code' attributes? '>' element_content* '</code>';
    kbd_element: '<kbd' attributes? '>' element_content* '</kbd>';
    samp_element: '<samp' attributes? '>' element_content* '</samp>';
    var_element: '<var' attributes? '>' element_content* '</var>';
    small_element: '<small' attributes? '>' element_content* '</small>';
    sub_element: '<sub' attributes? '>' element_content* '</sub>';
    sup_element: '<sup' attributes? '>' element_content* '</sup>';
    mark_element: '<mark' attributes? '>' element_content* '</mark>';
    del_element: '<del' attributes? '>' element_content* '</del>';
    ins_element: '<ins' attributes? '>' element_content* '</ins>';
    
    // Other elements
    a_element: '<a' attributes? '>' element_content* '</a>';
    img_element: '<img' attributes '/>' | '<img' attributes '>';
    blockquote_element: '<blockquote' attributes? '>' element_content* '</blockquote>';
    pre_element: '<pre' attributes? '>' element_content* '</pre>';
    hr_element: '<hr' attributes? '/>' | '<hr' attributes? '>';
    br_element: '<br' attributes? '/>' | '<br' attributes? '>';
    time_element: '<time' attributes? '>' element_content* '</time>';
    abbr_element: '<abbr' attributes? '>' element_content* '</abbr>';
    dfn_element: '<dfn' attributes? '>' element_content* '</dfn>';
    
    // Element content (recursive)
    element_content: block_element
                   | inline_element
                   | text_content
                   | comment
                   | WS;
    
    // Attributes
    attributes: WS+ attribute (WS+ attribute)*;
    attribute: attribute_name ('=' attribute_value)?;
    attribute_name: IDENTIFIER;
    attribute_value: quoted_string | unquoted_value;
    quoted_string: '"' string_content '"' | "'" string_content "'";
    unquoted_value: [a-zA-Z0-9_-]+;
    
    // Content types
    text_content: (TEXT_CHAR | ENTITY_REF)+;
    style_content: CSS_CONTENT*;  // Will be overridden by CSS grammar
    script_content: JS_CONTENT*;  // Will be overridden by JavaScript grammar
    string_content: (STRING_CHAR | ENTITY_REF)*;
    
    // Comments
    comment: '<!--' COMMENT_CONTENT '-->';
    
    // Symbol table entries for context-sensitive parsing
    @SymbolTable {
        elements: Map<String, ElementInfo>;
        ids: Map<String, ElementInfo>;
        classes: Map<String, Set<ElementInfo>>;
        scripts: List<ScriptInfo>;
        styles: List<StyleInfo>;
    }
    
    @ContextRules {
        // When entering <script> tag, switch to JavaScript context
        onEnterScript: {
            pushContext(JavaScript);
            preserveSymbolTable();
        }
        
        // When exiting </script> tag, return to HTML context
        onExitScript: {
            popContext();
            mergeSymbolTable();
        }
        
        // When entering <style> tag, switch to CSS context
        onEnterStyle: {
            pushContext(CSS);
            preserveSymbolTable();
        }
        
        // When exiting </style> tag, return to HTML context
        onExitStyle: {
            popContext();
            mergeSymbolTable();
        }
    }
    
    // Lexical rules
    IDENTIFIER: [a-zA-Z_][a-zA-Z0-9_-]*;
    TEXT_CHAR: ~[<>&] | ENTITY_REF;
    STRING_CHAR: ~['"&] | ENTITY_REF;
    ENTITY_REF: '&' [a-zA-Z][a-zA-Z0-9]* ';' | '&#' [0-9]+ ';' | '&#x' [0-9a-fA-F]+ ';';
    COMMENT_CONTENT: ~[-] | '-' ~[-] | '--' ~[>];
    CSS_CONTENT: ~[<] | '<' ~[/] | '</' ~[s] | '</s' ~[t] | '</st' ~[y] | '</sty' ~[l] | '</styl' ~[e];
    JS_CONTENT: ~[<] | '<' ~[/] | '</' ~[s] | '</s' ~[c] | '</sc' ~[r] | '</scr' ~[i] | '</scri' ~[p] | '</scrip' ~[t];
    WS: [ \t\r\n]+;
    
    // Semantic actions for symbol table management
    @SemanticActions {
        onElementStart(elementName: String, attributes: Map<String, String>) {
            ElementInfo element = new ElementInfo(elementName, attributes, getCurrentPosition());
            symbolTable.elements.put(generateElementId(), element);
            
            // Track id attribute
            if (attributes.containsKey("id")) {
                symbolTable.ids.put(attributes.get("id"), element);
            }
            
            // Track class attribute
            if (attributes.containsKey("class")) {
                String[] classes = attributes.get("class").split("\\s+");
                for (String className : classes) {
                    symbolTable.classes.computeIfAbsent(className, k -> new HashSet<>()).add(element);
                }
            }
        }
        
        onScriptElement(content: String, attributes: Map<String, String>) {
            ScriptInfo script = new ScriptInfo(content, attributes, getCurrentPosition());
            symbolTable.scripts.add(script);
        }
        
        onStyleElement(content: String, attributes: Map<String, String>) {
            StyleInfo style = new StyleInfo(content, attributes, getCurrentPosition());
            symbolTable.styles.add(style);
        }
    }
    
    // Error recovery rules
    @ErrorRecovery {
        strategy: synchronization;
        syncTokens: ['<', '>', '</', 'DOCTYPE'];
        maxErrors: 10;
        
        onMissingClosingTag(tagName: String) {
            warning("Missing closing tag for: " + tagName);
            insertToken("</" + tagName + ">");
        }
        
        onUnexpectedToken(token: String) {
            warning("Unexpected token: " + token);
            skipToNextSyncToken();
        }
        
        onMalformedAttribute(attrName: String) {
            warning("Malformed attribute: " + attrName);
            skipToNextAttribute();
        }
    }
    
    // Performance optimizations
    @Optimizations {
        enableMemoization: true;
        enableLeftRecursionElimination: true;
        enableCommonSubexpressionElimination: true;
        cacheSize: 1000;
        
        // Optimize common HTML patterns
        optimizePatterns: [
            "div_element*",
            "span_element+", 
            "attributes?",
            "element_content*"
        ];
    }
}

