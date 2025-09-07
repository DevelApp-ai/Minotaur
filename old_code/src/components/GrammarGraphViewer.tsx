import React from 'react';
import ReactFlow, { Background, Controls, Node, Edge } from 'react-flow-renderer';

interface GrammarGraphViewerProps {
  grammarCode: string;
}

export const GrammarGraphViewer: React.FC<GrammarGraphViewerProps> = ({ grammarCode }) => {
  if (!grammarCode) {
    return <div className="grammar-graph-empty">No grammar available</div>;
  }

  // Parse grammar code and convert to graph
  const { nodes, edges } = parseGrammarToGraph(grammarCode);

  return (
    <div className="grammar-graph-viewer" style={{ height: '500px', width: '100%' }}>
      <ReactFlow
        nodes={nodes}
        edges={edges}
        fitView
        attributionPosition="bottom-right"
      >
        <Background />
        <Controls />
      </ReactFlow>
    </div>
  );
};

// Helper function to parse grammar and convert to graph
function parseGrammarToGraph(grammarCode: string): { nodes: Node[], edges: Edge[] } {
  const nodes: Node[] = [];
  const edges: Edge[] = [];

  // Simple grammar parser
  const lines = grammarCode.split('\n');
  let grammarName = 'Grammar';
  let nodeId = 0;

  // Extract grammar name
  for (const line of lines) {
    if (line.startsWith('Grammar:')) {
      grammarName = line.substring('Grammar:'.length).trim();
      break;
    }
  }

  // Add grammar node
  nodes.push({
    id: 'grammar',
    type: 'default',
    data: { label: grammarName },
    position: { x: 0, y: 0 },
    style: { background: '#f5f5f5', border: '1px solid #ddd', borderRadius: '5px' },
  });

  // Parse productions
  const productions = new Map<string, string[]>();
  const nonTerminals = new Set<string>();
  const terminals = new Set<string>();

  for (const line of lines) {
    if (line.includes('::=')) {
      const parts = line.split('::=');
      if (parts.length === 2) {
        const leftSide = parts[0].trim();
        const rightSide = parts[1].trim();

        // Extract non-terminal name
        const nonTerminalMatch = leftSide.match(/<([^>]+)>/);
        if (nonTerminalMatch) {
          const nonTerminal = nonTerminalMatch[1];
          nonTerminals.add(nonTerminal);

          // Parse right side
          const elements = rightSide.split(/\s+/);
          const rightElements: string[] = [];

          for (const element of elements) {
            if (element === '|') {
              // Skip alternation symbol
              continue;
            }

            if (element.startsWith('<') && element.endsWith('>')) {
              // Non-terminal
              const nt = element.substring(1, element.length - 1);
              nonTerminals.add(nt);
              rightElements.push(nt);
            } else if (element.startsWith('"') && element.endsWith('"')) {
              // Terminal (literal)
              const terminal = element.substring(1, element.length - 1);
              terminals.add(terminal);
              rightElements.push(`"${terminal}"`);
            } else if (element.startsWith('/') && element.endsWith('/')) {
              // Terminal (regex)
              const regex = element;
              terminals.add(regex);
              rightElements.push(regex);
            }
          }

          if (!productions.has(nonTerminal)) {
            productions.set(nonTerminal, []);
          }
          productions.get(nonTerminal)?.push(...rightElements);
        }
      }
    }
  }

  // Create nodes for non-terminals
  let y = 100;
  for (const nonTerminal of nonTerminals) {
    nodes.push({
      id: `nt-${nonTerminal}`,
      type: 'default',
      data: { label: `<${nonTerminal}>` },
      position: { x: -200, y },
      style: { background: '#e6f7ff', border: '1px solid #91d5ff', borderRadius: '5px' },
    });

    // Connect grammar to non-terminal
    edges.push({
      id: `edge-grammar-${nonTerminal}`,
      source: 'grammar',
      target: `nt-${nonTerminal}`,
      type: 'smoothstep',
    });

    y += 80;
  }

  // Create nodes for terminals
  y = 100;
  for (const terminal of terminals) {
    nodes.push({
      id: `t-${nodeId++}`,
      type: 'default',
      data: { label: terminal },
      position: { x: 200, y },
      style: { background: '#f6ffed', border: '1px solid #b7eb8f', borderRadius: '5px' },
    });
    y += 80;
  }

  // Create edges for productions
  for (const [nonTerminal, rightElements] of productions.entries()) {
    for (const element of rightElements) {
      let targetId: string;

      if (element.startsWith('"') && element.endsWith('"')) {
        // Find terminal node
        const terminal = element;
        const terminalNode = nodes.find(n => n.data.label === terminal);
        if (!terminalNode) {
          continue;
        }
        targetId = terminalNode.id;
      } else if (element.startsWith('/') && element.endsWith('/')) {
        // Find regex terminal node
        const regex = element;
        const regexNode = nodes.find(n => n.data.label === regex);
        if (!regexNode) {
          continue;
        }
        targetId = regexNode.id;
      } else {
        // Non-terminal
        targetId = `nt-${element}`;
      }

      edges.push({
        id: `edge-${nonTerminal}-${nodeId++}`,
        source: `nt-${nonTerminal}`,
        target: targetId,
        type: 'smoothstep',
      });
    }
  }

  return { nodes, edges };
}
