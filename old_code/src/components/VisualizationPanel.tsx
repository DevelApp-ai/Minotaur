/*
 * Copyright 2025 DevelApp.ai
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

import React from 'react';
import { ParseTreeViewer } from './ParseTreeViewer';
import { GrammarGraphViewer } from './GrammarGraphViewer';
import { RailroadDiagramViewer } from './RailroadDiagramViewer';

interface VisualizationPanelProps {
  parseTree: any;
  grammarCode: string;
  sourceCode: string;
}

export const VisualizationPanel: React.FC<VisualizationPanelProps> = ({
  parseTree,
  grammarCode,
  sourceCode,
}) => {
  const [activeView, setActiveView] = React.useState<'parseTree' | 'grammarGraph' | 'railroadDiagram'>('parseTree');

  return (
    <div className="visualization-panel">
      <div className="visualization-tabs">
        <button
          className={activeView === 'parseTree' ? 'active' : ''}
          onClick={() => setActiveView('parseTree')}
        >
          Parse Tree
        </button>
        <button
          className={activeView === 'grammarGraph' ? 'active' : ''}
          onClick={() => setActiveView('grammarGraph')}
        >
          Grammar Graph
        </button>
        <button
          className={activeView === 'railroadDiagram' ? 'active' : ''}
          onClick={() => setActiveView('railroadDiagram')}
        >
          Railroad Diagram
        </button>
      </div>

      <div className="visualization-container">
        {activeView === 'parseTree' && (
          <ParseTreeViewer parseTree={parseTree} />
        )}
        {activeView === 'grammarGraph' && (
          <GrammarGraphViewer grammarCode={grammarCode} />
        )}
        {activeView === 'railroadDiagram' && (
          <RailroadDiagramViewer
            grammarCode={grammarCode}
            options={{
              theme: 'default',
              layout: {
                direction: 'horizontal',
              },
              includeInheritance: true,
              includeContextSensitive: true,
              includeInteractivity: true,
            }}
    // eslint-disable-next-line no-console
            onError={(error) => console.error('Railroad diagram error:', error)}
    // eslint-disable-next-line no-console
            onGenerated={(result) => console.log('Railroad diagram generated:', result)}
          />
        )}
      </div>

      <div className="visualization-info">
        <div className="view-description">
          {activeView === 'parseTree' && (
            <p>Parse tree shows the hierarchical structure of parsed source code.</p>
          )}
          {activeView === 'grammarGraph' && (
            <p>Grammar graph displays grammar rules and their relationships using ReactFlow.</p>
          )}
          {activeView === 'railroadDiagram' && (
            // eslint-disable-next-line max-len
            <p>Railroad diagram visualizes grammar rules as railroad tracks with Minotaur&apos;s advanced rendering engine.</p>
          )}
        </div>

        {parseTree && (
          <div className="parse-status success">
            Parse completed successfully
          </div>
        )}
        {!parseTree && (
          <div className="parse-status waiting">
            No parse result available. Parse your code first.
          </div>
        )}
      </div>
    </div>
  );
};
