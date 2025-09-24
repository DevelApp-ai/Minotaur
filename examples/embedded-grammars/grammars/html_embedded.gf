/**
 * Embedded HTML Grammar - Composite Grammar with JavaScript and CSS
 * 
 * This grammar demonstrates the power of Minotaur inheritance by composing
 * HTML, JavaScript, and CSS grammars into a single, context-sensitive parser
 * that can handle embedded languages within HTML documents.
 */

@Minotaur
@FormatType: Minotaur
@Version: 1.0.0
@Author: Minotaur Compiler-Compiler System
@Description: Composite HTML grammar with embedded JavaScript and CSS support

@Inherits: HTMLBase, JavaScript, CSS
@InheritanceStrategy: composition
@ConflictResolution: priority_based

grammar HTMLEmbedded {
    // Grammar metadata and configuration
    @Inheritable: true
    @ContextSensitive: true
    @EnableSymbolTable: true
    @EnableScopeTracking: true
    @EnableCrossLanguageValidation: true
    @EnableEmbeddedLanguageSupport: true
    
    // Inheritance configuration
    @InheritanceConfig {
        primaryGrammar: HTMLBase;
        embeddedGrammars: [JavaScript, CSS];
        
        // Context switching rules
        contextSwitchRules: {
            "script_element": {
                enterContext: JavaScript;
                exitContext: HTMLBase;
                preserveSymbolTable: true;
                enableCrossReference: true;
            },
            "style_element": {
                enterContext: CSS;
                exitContext: HTMLBase;
                preserveSymbolTable: true;
                enableCrossReference: true;
            }
        };
        
        // Symbol table merging strategy
        symbolTableMerging: {
            strategy: "hierarchical";
            conflictResolution: "parent_wins";
            enableCrossLanguageReferences: true;
        };
        
        // Rule precedence for conflicts
        rulePrecedence: {
            HTMLBase: 1;
            JavaScript: 2;
            CSS: 3;
        };
    }
    
    // Override start rule to use composite parsing
    start: embedded_document;
    
    // Enhanced document structure with embedded language support
    embedded_document: doctype? embedded_html_element;
    
    embedded_html_element: '<html' attributes? '>' 
                          embedded_head_element? 
                          embedded_body_element? 
                          '</html>';
    
    embedded_head_element: '<head' attributes? '>' 
                          embedded_head_content* 
                          '</head>';
    
    embedded_body_element: '<body' attributes? '>' 
                          embedded_body_content* 
                          '</body>';
    
    // Enhanced head content with embedded language support
    embedded_head_content: title_element
                         | meta_element
                         | link_element
                         | embedded_style_element
                         | embedded_script_element
                         | base_element
                         | comment
                         | WS;
    
    // Enhanced body content with embedded language support
    embedded_body_content: embedded_block_element
                         | embedded_inline_element
                         | text_content
                         | comment
                         | WS;
    
    embedded_block_element: div_element
                          | p_element
                          | h1_element | h2_element | h3_element | h4_element | h5_element | h6_element
                          | ul_element | ol_element
                          | table_element
                          | form_element
                          | section_element | article_element | aside_element | nav_element
                          | header_element | footer_element | main_element
                          | blockquote_element | pre_element
                          | hr_element | br_element
                          | embedded_script_element
                          | embedded_style_element;
    
    embedded_inline_element: span_element
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
    
    // Enhanced style element with CSS parsing
    @ContextSwitch(CSS)
    @InheritFrom(CSS.stylesheet)
    embedded_style_element: '<style' style_attributes? '>' 
                           @EmbeddedContent(CSS) embedded_css_content 
                           '</style>';
    
    style_attributes: attributes;
    
    // CSS content parsed using CSS grammar
    embedded_css_content: @UseGrammar(CSS) css_stylesheet;
    css_stylesheet: CSS.stylesheet;
    
    // Enhanced script element with JavaScript parsing
    @ContextSwitch(JavaScript)
    @InheritFrom(JavaScript.program)
    embedded_script_element: '<script' script_attributes? '>' 
                            @EmbeddedContent(JavaScript) embedded_js_content 
                            '</script>'
                           | '<script' script_attributes '/>';
    
    script_attributes: attributes;
    
    // JavaScript content parsed using JavaScript grammar
    embedded_js_content: @UseGrammar(JavaScript) js_program;
    js_program: JavaScript.program;
    
    // Enhanced symbol table for cross-language integration
    @SymbolTable {
        // HTML symbols (inherited from HTMLBase)
        htmlElements: Map<String, HTMLElementInfo>;
        htmlIds: Map<String, HTMLElementInfo>;
        htmlClasses: Map<String, Set<HTMLElementInfo>>;
        
        // JavaScript symbols (inherited from JavaScript)
        jsVariables: Map<String, JSVariableInfo>;
        jsFunctions: Map<String, JSFunctionInfo>;
        jsClasses: Map<String, JSClassInfo>;
        
        // CSS symbols (inherited from CSS)
        cssSelectors: Map<String, CSSSelectorInfo>;
        cssProperties: Map<String, CSSPropertyInfo>;
        cssCustomProperties: Map<String, CSSCustomPropertyInfo>;
        
        // Cross-language references
        crossReferences: Map<String, CrossReferenceInfo>;
        domBindings: Map<String, DOMBindingInfo>;
        styleBindings: Map<String, StyleBindingInfo>;
        eventBindings: Map<String, EventBindingInfo>;
        
        // Context stack for embedded parsing
        contextStack: Stack<ParsingContext>;
        currentContext: ParsingContext;
    }
    
    // Enhanced context rules for embedded language support
    @ContextRules {
        // HTML context rules (inherited and enhanced)
        onHTMLElementStart: {
            HTMLElementInfo element = createHTMLElement(elementName, attributes);
            symbolTable.htmlElements.put(generateElementId(), element);
            
            // Track ID and class attributes for cross-language validation
            trackElementForCrossLanguageValidation(element);
        }
        
        // Style element context switching
        onStyleElementStart: {
            // Switch to CSS parsing context
            ParsingContext cssContext = new ParsingContext(CSS, getCurrentPosition());
            symbolTable.contextStack.push(symbolTable.currentContext);
            symbolTable.currentContext = cssContext;
            
            // Initialize CSS symbol table
            initializeCSSSymbolTable();
        }
        
        onStyleElementEnd: {
            // Merge CSS symbols with HTML context
            mergeCSSSymbolsWithHTML();
            
            // Validate CSS selectors against HTML elements
            validateCSSSelectorsAgainstHTML();
            
            // Return to HTML parsing context
            symbolTable.currentContext = symbolTable.contextStack.pop();
        }
        
        // Script element context switching
        onScriptElementStart: {
            // Switch to JavaScript parsing context
            ParsingContext jsContext = new ParsingContext(JavaScript, getCurrentPosition());
            symbolTable.contextStack.push(symbolTable.currentContext);
            symbolTable.currentContext = jsContext;
            
            // Initialize JavaScript symbol table
            initializeJavaScriptSymbolTable();
        }
        
        onScriptElementEnd: {
            // Merge JavaScript symbols with HTML context
            mergeJavaScriptSymbolsWithHTML();
            
            // Validate JavaScript DOM references against HTML elements
            validateJavaScriptDOMReferencesAgainstHTML();
            
            // Return to HTML parsing context
            symbolTable.currentContext = symbolTable.contextStack.pop();
        }
        
        // Cross-language validation rules
        onCSSElementSelector: {
            validateCSSElementSelectorAgainstHTML(elementName);
            createCrossReference("CSS_ELEMENT_SELECTOR", elementName, getCurrentPosition());
        }
        
        onCSSIdSelector: {
            validateCSSIdSelectorAgainstHTML(idName);
            createCrossReference("CSS_ID_SELECTOR", idName, getCurrentPosition());
        }
        
        onCSSClassSelector: {
            validateCSSClassSelectorAgainstHTML(className);
            createCrossReference("CSS_CLASS_SELECTOR", className, getCurrentPosition());
        }
        
        onJavaScriptDOMReference: {
            validateJavaScriptDOMReferenceAgainstHTML(elementId);
            createCrossReference("JS_DOM_REFERENCE", elementId, getCurrentPosition());
        }
        
        onJavaScriptEventHandler: {
            validateJavaScriptEventHandlerAgainstHTML(elementId, eventType);
            createEventBinding(elementId, eventType, handlerFunction);
        }
    }
    
    // Enhanced semantic actions for cross-language integration
    @SemanticActions {
        // HTML element processing with cross-language awareness
        onHTMLElementDeclaration(elementName: String, attributes: Map<String, String>) {
            HTMLElementInfo element = new HTMLElementInfo(elementName, attributes, getCurrentPosition());
            symbolTable.htmlElements.put(generateElementId(), element);
            
            // Track for CSS validation
            if (attributes.containsKey("id")) {
                symbolTable.htmlIds.put(attributes.get("id"), element);
            }
            
            if (attributes.containsKey("class")) {
                String[] classes = attributes.get("class").split("\\s+");
                for (String className : classes) {
                    symbolTable.htmlClasses.computeIfAbsent(className, k -> new HashSet<>()).add(element);
                }
            }
            
            // Track event handlers for JavaScript validation
            trackEventHandlersForJavaScriptValidation(element, attributes);
        }
        
        // CSS processing with HTML validation
        onCSSRuleDeclaration(selector: String, declarations: List<CSSDeclaration>) {
            CSSSelectorInfo selectorInfo = new CSSSelectorInfo(selector, declarations, getCurrentPosition());
            symbolTable.cssSelectors.put(selector, selectorInfo);
            
            // Validate selector against HTML elements
            validateCSSSelector(selector);
            
            // Create style bindings
            createStyleBindings(selector, declarations);
        }
        
        // JavaScript processing with HTML validation
        onJavaScriptFunctionDeclaration(name: String, parameters: List<Parameter>, body: Statement) {
            JSFunctionInfo function = new JSFunctionInfo(name, parameters, body, getCurrentPosition());
            symbolTable.jsFunctions.put(name, function);
            
            // Check for DOM manipulation in function body
            analyzeDOMManipulationInFunction(function);
        }
        
        onJavaScriptDOMAccess(method: String, elementId: String) {
            // Validate DOM element exists in HTML
            if (!symbolTable.htmlIds.containsKey(elementId)) {
                warning("JavaScript references non-existent HTML element: " + elementId);
            } else {
                DOMBindingInfo binding = new DOMBindingInfo(method, elementId, getCurrentPosition());
                symbolTable.domBindings.put(elementId + ":" + method, binding);
            }
        }
        
        // Cross-language reference creation
        onCrossLanguageReference(sourceLanguage: String, targetLanguage: String, 
                                referenceName: String, referenceType: String) {
            CrossReferenceInfo crossRef = new CrossReferenceInfo(
                sourceLanguage, targetLanguage, referenceName, referenceType, getCurrentPosition()
            );
            symbolTable.crossReferences.put(referenceName + ":" + referenceType, crossRef);
        }
        
        // Event binding creation
        onEventHandlerBinding(elementId: String, eventType: String, handlerFunction: String) {
            EventBindingInfo eventBinding = new EventBindingInfo(
                elementId, eventType, handlerFunction, getCurrentPosition()
            );
            symbolTable.eventBindings.put(elementId + ":" + eventType, eventBinding);
        }
    }
    
    // Enhanced error recovery for embedded languages
    @ErrorRecovery {
        strategy: context_aware_synchronization;
        
        // Context-specific sync tokens
        htmlSyncTokens: ['<', '>', '</', 'DOCTYPE'];
        cssSyncTokens: [';', '}', '{', '@media', '@keyframes'];
        jsSyncTokens: [';', '}', '{', 'function', 'var', 'let', 'const'];
        
        maxErrors: 25;
        
        // HTML-specific error recovery
        onHTMLError: {
            if (getCurrentContext() == HTML) {
                syncToTokens(htmlSyncTokens);
            }
        }
        
        // CSS-specific error recovery
        onCSSError: {
            if (getCurrentContext() == CSS) {
                syncToTokens(cssSyncTokens);
                // Try to return to HTML context if CSS parsing fails completely
                if (getErrorCount() > 5) {
                    warning("Too many CSS errors, returning to HTML context");
                    forceContextSwitch(HTML);
                }
            }
        }
        
        // JavaScript-specific error recovery
        onJavaScriptError: {
            if (getCurrentContext() == JavaScript) {
                syncToTokens(jsSyncTokens);
                // Try to return to HTML context if JavaScript parsing fails completely
                if (getErrorCount() > 5) {
                    warning("Too many JavaScript errors, returning to HTML context");
                    forceContextSwitch(HTML);
                }
            }
        }
        
        // Context switching error recovery
        onContextSwitchError: {
            warning("Failed to switch parsing context, staying in current context");
            // Don't switch context, continue with current parser
        }
        
        // Cross-language validation errors
        onCrossLanguageValidationError: {
            warning("Cross-language validation failed: " + getErrorMessage());
            // Continue parsing but mark as validation error
        }
    }
    
    // Performance optimizations for embedded parsing
    @Optimizations {
        enableMemoization: true;
        enableLeftRecursionElimination: true;
        enableCommonSubexpressionElimination: true;
        cacheSize: 3000;
        
        // Context-specific optimizations
        htmlOptimizations: [
            "element_content*",
            "attributes?",
            "text_content"
        ];
        
        cssOptimizations: [
            "selector_list",
            "declaration_list",
            "expression"
        ];
        
        jsOptimizations: [
            "statement_list",
            "expression",
            "function_declaration"
        ];
        
        // Cross-language optimization
        crossLanguageOptimizations: [
            "symbol_table_lookup",
            "context_switching",
            "cross_reference_validation"
        ];
        
        // Enable parallel parsing for independent sections
        enableParallelParsing: true;
        parallelSections: ["style_element", "script_element"];
    }
    
    // Validation framework for embedded languages
    @ValidationFramework {
        // HTML validation rules
        htmlValidation: {
            validateElementNesting: true;
            validateAttributeValues: true;
            validateRequiredAttributes: true;
            validateDeprecatedElements: true;
        }
        
        // CSS validation rules
        cssValidation: {
            validatePropertyNames: true;
            validatePropertyValues: true;
            validateSelectorSyntax: true;
            validateMediaQueries: true;
            validateCustomProperties: true;
        }
        
        // JavaScript validation rules
        jsValidation: {
            validateSyntax: true;
            validateVariableDeclarations: true;
            validateFunctionDeclarations: true;
            validateScopeRules: true;
            validateDOMReferences: true;
        }
        
        // Cross-language validation rules
        crossLanguageValidation: {
            validateCSSSelectorsAgainstHTML: true;
            validateJavaScriptDOMReferencesAgainstHTML: true;
            validateEventHandlerBindings: true;
            validateStyleApplications: true;
            validateCustomPropertyUsage: true;
        }
        
        // Performance validation
        performanceValidation: {
            detectUnusedCSS: true;
            detectUnusedJavaScript: true;
            detectInlineStyleOveruse: true;
            detectInlineScriptOveruse: true;
        }
    }
    
    // Integration with Minotaur AI Agent System
    @AIAgentIntegration {
        // Provide rich context information for AI agents
        contextSharing: {
            shareHTMLStructure: true;
            shareCSSRules: true;
            shareJavaScriptFunctions: true;
            shareCrossLanguageReferences: true;
        }
        
        // Enable AI-assisted refactoring across languages
        refactoringSupport: {
            enableCrossLanguageRefactoring: true;
            enableStyleExtraction: true;
            enableScriptExtraction: true;
            enableComponentExtraction: true;
        }
        
        // Code completion support
        codeCompletion: {
            htmlElementCompletion: true;
            cssPropertyCompletion: true;
            jsAPICompletion: true;
            crossLanguageCompletion: true;
        }
        
        // Real-time validation for AI agents
        realTimeValidation: {
            enableLiveValidation: true;
            provideSuggestions: true;
            highlightErrors: true;
            showCrossLanguageImpact: true;
        }
    }
    
    // Export configuration for compiler-compiler
    @ExportConfiguration {
        // Target languages for parser generation
        targetLanguages: ["C", "C++", "Java", "C#", "Python", "JavaScript", "Rust", "Go", "WebAssembly"];
        
        // Language-specific optimizations
        languageOptimizations: {
            C: {
                enableDirectCodedScanners: true;
                enablePerfectHashKeywordLookup: true;
                enableCacheFriendlyDataStructures: true;
            },
            "C++": {
                enableTemplateMetaprogramming: true;
                enableRAII: true;
                enableMoveSemantics: true;
            },
            Java: {
                enableJVMOptimization: true;
                enableConcurrentCollections: true;
            },
            "C#": {
                enableDotNetOptimization: true;
                enableValueTypes: true;
            },
            Python: {
                enableInterpreterOptimization: true;
                enableBuiltinDataStructures: true;
            },
            JavaScript: {
                enableModernEngineOptimization: true;
                enableES6Features: true;
            },
            Rust: {
                enableZeroCostAbstractions: true;
                enableOwnershipSystem: true;
            },
            Go: {
                enableGoroutineSupport: true;
                enableGCOptimization: true;
            },
            WebAssembly: {
                enableLinearMemoryManagement: true;
                enableJavaScriptInterop: true;
            }
        };
        
        // Cross-language consistency
        ensureCrossLanguageConsistency: true;
        generateUnifiedAPI: true;
        enablePerformanceBenchmarking: true;
    }
}

