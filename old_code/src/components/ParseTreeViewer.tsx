import React from 'react';
import ReactFlow, { Background, Controls, Node, Edge } from 'react-flow-renderer';

interface ParseTreeViewerProps {
  parseTree: any;
}

export const ParseTreeViewer: React.FC<ParseTreeViewerProps> = ({ parseTree }) => {
  if (!parseTree) {
    return <div className="parse-tree-empty">No parse tree</div>;
  }

  // Convert parse tree to ReactFlow nodes and edges
  const { nodes, edges } = convertParseTreeToFlow(parseTree);

  return (
    <div className="parse-tree-viewer" style={{ height: '500px', width: '100%' }}>
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

// Helper function to convert parse tree to ReactFlow format
function convertParseTreeToFlow(parseTree: any): { nodes: Node[], edges: Edge[] } {
  const nodes: Node[] = [];
  const edges: Edge[] = [];
  let nodeId = 0;

  function processNode(node: any, parentId: string | null, x: number, y: number): string {
    const id = `node-${nodeId++}`;

    nodes.push({
      id,
      type: 'default',
      data: {
        label: node.value ? `${node.name}: ${node.value}` : node.name,
      },
      position: { x, y },
    });

    if (parentId) {
      edges.push({
        id: `edge-${parentId}-${id}`,
        source: parentId,
        target: id,
        type: 'smoothstep',
      });
    }

    if (node.children && node.children.length > 0) {
      const childWidth = 200;
      const startX = x - ((node.children.length - 1) * childWidth) / 2;

      node.children.forEach((child: any, index: number) => {
        const childX = startX + index * childWidth;
        processNode(child, id, childX, y + 100);
      });
    }

    return id;
  }

  // Start with the root node
  processNode(parseTree.tree, null, 0, 0);

  return { nodes, edges };
}
