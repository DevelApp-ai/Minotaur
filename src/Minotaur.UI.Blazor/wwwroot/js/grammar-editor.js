// Grammar Editor JavaScript Support
// Provides enhanced functionality for the grammar editor including cursor tracking and completion

window.getCursorPosition = (element) => {
    if (!element) return 0;
    try {
        return element.selectionStart || 0;
    } catch {
        return 0;
    }
};

window.setCursorPosition = (element, position) => {
    if (!element) return;
    try {
        element.setSelectionRange(position, position);
    } catch {
        // Silently fail
    }
};

window.focusElement = (element) => {
    if (!element) return;
    try {
        element.focus();
    } catch {
        // Silently fail
    }
};

window.insertTextAtCursor = (element, text) => {
    if (!element) return;
    try {
        const start = element.selectionStart;
        const end = element.selectionEnd;
        const value = element.value;
        
        element.value = value.substring(0, start) + text + value.substring(end);
        element.setSelectionRange(start + text.length, start + text.length);
        
        // Trigger input event to notify Blazor
        element.dispatchEvent(new Event('input', { bubbles: true }));
    } catch {
        // Silently fail
    }
};

window.getSelectedText = (element) => {
    if (!element) return '';
    try {
        const start = element.selectionStart;
        const end = element.selectionEnd;
        return element.value.substring(start, end);
    } catch {
        return '';
    }
};

// Enhanced editor functionality
class GrammarEditorEnhancer {
    constructor(textareaElement, highlightElement) {
        this.textarea = textareaElement;
        this.highlight = highlightElement;
        this.setupScrollSync();
        this.setupKeyboardHandlers();
    }
    
    setupScrollSync() {
        if (!this.textarea || !this.highlight) return;
        
        this.textarea.addEventListener('scroll', () => {
            this.highlight.scrollTop = this.textarea.scrollTop;
            this.highlight.scrollLeft = this.textarea.scrollLeft;
        });
    }
    
    setupKeyboardHandlers() {
        if (!this.textarea) return;
        
        this.textarea.addEventListener('keydown', (e) => {
            // Handle Tab key for indentation
            if (e.key === 'Tab') {
                e.preventDefault();
                this.insertTab();
            }
            // Handle Ctrl+/ for comment toggle
            else if (e.key === '/' && e.ctrlKey) {
                e.preventDefault();
                this.toggleComment();
            }
            // Handle Ctrl+Space for manual completion
            else if (e.key === ' ' && e.ctrlKey) {
                e.preventDefault();
                this.triggerCompletion();
            }
        });
    }
    
    insertTab() {
        const start = this.textarea.selectionStart;
        const end = this.textarea.selectionEnd;
        const value = this.textarea.value;
        
        // Check if we have selection (indent/outdent block)
        if (start !== end) {
            this.indentBlock();
        } else {
            // Insert tab at cursor
            this.textarea.value = value.substring(0, start) + '    ' + value.substring(end);
            this.textarea.setSelectionRange(start + 4, start + 4);
            this.textarea.dispatchEvent(new Event('input', { bubbles: true }));
        }
    }
    
    indentBlock() {
        const start = this.textarea.selectionStart;
        const end = this.textarea.selectionEnd;
        const value = this.textarea.value;
        
        // Find line boundaries
        const beforeSelection = value.substring(0, start);
        const selection = value.substring(start, end);
        const afterSelection = value.substring(end);
        
        const lineStart = beforeSelection.lastIndexOf('\n') + 1;
        const lineEnd = end + afterSelection.indexOf('\n');
        
        // Get the lines to indent
        const lines = value.substring(lineStart, lineEnd === -1 ? value.length : lineEnd).split('\n');
        const indentedLines = lines.map(line => '    ' + line);
        
        // Replace the content
        const newValue = value.substring(0, lineStart) + 
                        indentedLines.join('\n') + 
                        value.substring(lineEnd === -1 ? value.length : lineEnd);
        
        this.textarea.value = newValue;
        this.textarea.setSelectionRange(start + 4, end + (indentedLines.length * 4));
        this.textarea.dispatchEvent(new Event('input', { bubbles: true }));
    }
    
    toggleComment() {
        const start = this.textarea.selectionStart;
        const end = this.textarea.selectionEnd;
        const value = this.textarea.value;
        
        // Find current line
        const beforeCursor = value.substring(0, start);
        const lineStart = beforeCursor.lastIndexOf('\n') + 1;
        const lineEnd = value.indexOf('\n', end);
        const currentLine = value.substring(lineStart, lineEnd === -1 ? value.length : lineEnd);
        
        let newLine;
        let cursorOffset = 0;
        
        // Check if line is already commented
        if (currentLine.trim().startsWith('//')) {
            // Remove comment
            newLine = currentLine.replace(/^(\s*)\/\/\s?/, '$1');
            cursorOffset = -3;
        } else {
            // Add comment
            const leadingWhitespace = currentLine.match(/^\s*/)[0];
            newLine = leadingWhitespace + '// ' + currentLine.substring(leadingWhitespace.length);
            cursorOffset = 3;
        }
        
        // Replace the line
        const newValue = value.substring(0, lineStart) + 
                        newLine + 
                        value.substring(lineEnd === -1 ? value.length : lineEnd);
        
        this.textarea.value = newValue;
        this.textarea.setSelectionRange(start + cursorOffset, end + cursorOffset);
        this.textarea.dispatchEvent(new Event('input', { bubbles: true }));
    }
    
    triggerCompletion() {
        // Dispatch custom event for Blazor to handle
        this.textarea.dispatchEvent(new CustomEvent('manual-completion', { bubbles: true }));
    }
}

// Auto-bracket completion
window.setupAutoBrackets = (element) => {
    if (!element) return;
    
    const brackets = {
        '(': ')',
        '[': ']',
        '{': '}',
        '"': '"',
        "'": "'"
    };
    
    element.addEventListener('keydown', (e) => {
        const key = e.key;
        if (brackets[key]) {
            const start = element.selectionStart;
            const end = element.selectionEnd;
            const value = element.value;
            
            // Don't auto-complete if we already have the closing bracket next
            if (key === '"' || key === "'") {
                if (value[start] === key) {
                    e.preventDefault();
                    element.setSelectionRange(start + 1, start + 1);
                    return;
                }
            }
            
            e.preventDefault();
            
            const selectedText = value.substring(start, end);
            const newValue = value.substring(0, start) + 
                           key + selectedText + brackets[key] + 
                           value.substring(end);
            
            element.value = newValue;
            element.setSelectionRange(start + 1, start + 1 + selectedText.length);
            element.dispatchEvent(new Event('input', { bubbles: true }));
        }
    });
};

// Line number synchronization
window.syncLineNumbers = (textareaElement, lineNumberContainer) => {
    if (!textareaElement || !lineNumberContainer) return;
    
    const updateLineNumbers = () => {
        const lines = textareaElement.value.split('\n');
        const lineCount = lines.length;
        
        let html = '';
        for (let i = 1; i <= lineCount; i++) {
            html += `<div class="line-number">${i}</div>`;
        }
        
        lineNumberContainer.innerHTML = html;
    };
    
    // Update on input and scroll
    textareaElement.addEventListener('input', updateLineNumbers);
    textareaElement.addEventListener('scroll', () => {
        lineNumberContainer.scrollTop = textareaElement.scrollTop;
    });
    
    // Initial update
    updateLineNumbers();
};

// Export for global access
window.GrammarEditorEnhancer = GrammarEditorEnhancer;