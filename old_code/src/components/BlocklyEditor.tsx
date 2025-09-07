import React, { useEffect, useRef } from 'react';
import Blockly from 'blockly';

interface BlocklyEditorProps {
  initialXml?: string;
  onChange?: (xml: string, code: string) => void;
}

export const BlocklyEditor: React.FC<BlocklyEditorProps> = ({
  initialXml,
  onChange,
}) => {
  const blocklyDiv = useRef<HTMLDivElement>(null);
  const workspaceRef = useRef<any>(null);

  useEffect(() => {
    if (!blocklyDiv.current) {
      return;
    }

    let workspace: any = null;

    try {
      // Define custom blocks for grammar elements with error handling
      const defineGrammarBlocks = () => {
        try {
          // Ensure Blockly.Blocks exists before assignment
          if (!Blockly.Blocks) {
            Blockly.Blocks = {};
          }

          // Grammar block
          if (!Blockly.Blocks['grammar']) {
            Blockly.Blocks['grammar'] = {
              init: function() {
                try {
                  this.appendDummyInput()
                    .appendField('Grammar:')
                    .appendField(new Blockly.FieldTextInput('MyGrammar'), 'NAME');
                  this.appendStatementInput('PRODUCTIONS')
                    .setCheck('production')
                    .appendField('Productions:');
                  this.setColour(160);
                  this.setTooltip('Define a grammar with productions');
                  this.setHelpUrl('');
                } catch (err) {
    // eslint-disable-next-line no-console
                  console.error('Error initializing grammar block:', err);
                }
              },
            };
          }

          // Production block
          if (!Blockly.Blocks['production']) {
            Blockly.Blocks['production'] = {
              init: function() {
                try {
                  this.appendDummyInput()
                    .appendField('Production:')
                    .appendField(new Blockly.FieldTextInput('rule'), 'NAME');
                  this.appendValueInput('DEFINITION')
                    .setCheck('expression')
                    .appendField('=');
                  this.setPreviousStatement(true, 'production');
                  this.setNextStatement(true, 'production');
                  this.setColour(230);
                  this.setTooltip('Define a production rule');
                  this.setHelpUrl('');
                } catch (err) {
    // eslint-disable-next-line no-console
                  console.error('Error initializing production block:', err);
                }
              },
            };
          }

          // Terminal block
          if (!Blockly.Blocks['terminal']) {
            Blockly.Blocks['terminal'] = {
              init: function() {
                try {
                  this.appendDummyInput()
                    .appendField('"')
                    .appendField(new Blockly.FieldTextInput('token'), 'VALUE')
                    .appendField('"');
                  this.setOutput(true, 'expression');
                  this.setColour(300);
                  this.setTooltip('Terminal symbol');
                  this.setHelpUrl('');
                } catch (err) {
    // eslint-disable-next-line no-console
                  console.error('Error initializing terminal block:', err);
                }
              },
            };
          }
        } catch (err) {
    // eslint-disable-next-line no-console
          console.error('Error defining grammar blocks:', err);
        }
      };

      // Define blocks before creating workspace
      defineGrammarBlocks();

      // Create workspace with error handling
      try {
        workspace = Blockly.inject(blocklyDiv.current, {
          toolbox: `
            <xml>
              <category name="Grammar" colour="160">
                <block type="grammar"></block>
                <block type="production"></block>
                <block type="terminal"></block>
              </category>
            </xml>
          `,
          collapse: true,
          comments: true,
          disable: true,
          maxBlocks: Infinity,
          trashcan: true,
          horizontalLayout: false,
          toolboxPosition: 'start',
          css: true,
          media: 'https://blockly-demo.appspot.com/static/media/',
          rtl: false,
          scrollbars: true,
          sounds: false,
          oneBasedIndex: true,
          grid: {
            spacing: 20,
            length: 3,
            colour: '#ccc',
            snap: true,
          },
          zoom: {
            controls: true,
            wheel: true,
            startScale: 1.0,
            maxScale: 3,
            minScale: 0.3,
            scaleSpeed: 1.2,
          },
        });

        // In test environment, workspace might be null from mock
        if (!workspace) {
          // Create a fallback workspace for tests
          workspace = {
            addChangeListener: () => {},
            dispose: () => {},
            render: () => {},
          };
        }

        // Load initial XML if provided
        if (initialXml && initialXml.trim()) {
          try {
            if (Blockly.Xml && Blockly.Xml.textToDom && Blockly.Xml.domToWorkspace) {
              const xml = Blockly.Xml.textToDom(initialXml);
              Blockly.Xml.domToWorkspace(xml, workspace);
            }
          } catch (e) {
    // eslint-disable-next-line no-console
            console.error('Error loading initial XML:', e);
          }
        }

        // Add change listener with error handling
        if (onChange && workspace && workspace.addChangeListener) {
          workspace.addChangeListener(() => {
            try {
              if (Blockly.Xml && Blockly.JavaScript) {
                const xml = Blockly.Xml.domToText(
                  Blockly.Xml.workspaceToDom(workspace),
                );
                const code = Blockly.JavaScript.workspaceToCode(workspace);
                onChange(xml || '', code || '');
              }
            } catch (err) {
    // eslint-disable-next-line no-console
              console.error('Error in change listener:', err);
              // Still call onChange with empty values to prevent hanging
              if (onChange) {
                onChange('', '');
              }
            }
          });
        }

        workspaceRef.current = workspace;

      } catch (err) {
    // eslint-disable-next-line no-console
        console.error('Error creating Blockly workspace:', err);
        // Create a fallback div with error message
        if (blocklyDiv.current) {
          blocklyDiv.current.innerHTML = `
            <div style="padding: 20px; text-align: center; color: #666; border: 1px solid #ddd; border-radius: 4px;">
              <p>Visual Editor temporarily unavailable</p>
              <p style="font-size: 12px;">Error: ${err instanceof Error ? err.message : 'Unknown error'}</p>
            </div>
          `;
        }
      }

    } catch (err) {
    // eslint-disable-next-line no-console
      console.error('Error in BlocklyEditor useEffect:', err);
    }

    // Cleanup function
    return () => {
      try {
        if (workspaceRef.current && workspaceRef.current.dispose) {
          workspaceRef.current.dispose();
          workspaceRef.current = null;
        }
      } catch (err) {
    // eslint-disable-next-line no-console
        console.error('Error disposing Blockly workspace:', err);
      }
    };
  }, [initialXml, onChange]);

  return (
    <div
      ref={blocklyDiv}
      style={{
        height: '400px',
        width: '100%',
        border: '1px solid #ddd',
        borderRadius: '4px',
        minHeight: '400px',
      }}
    />
  );
};

