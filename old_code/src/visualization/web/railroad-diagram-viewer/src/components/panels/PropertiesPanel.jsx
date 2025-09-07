import { motion, AnimatePresence } from 'framer-motion';
import {
  Info,
  Code,
  GitBranch,
  Target,
  Layers,
  FileText,
  Hash,
  Type,
  Clock,
  BarChart3,
} from 'lucide-react';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card.jsx';
import { Badge } from '@/components/ui/badge.jsx';
import { Separator } from '@/components/ui/separator.jsx';
import { Label } from '@/components/ui/label.jsx';

const PropertiesPanel = ({ selectedElement, diagram, grammar }) => {
  if (!selectedElement && !diagram && !grammar) {
    return (
      <div className="space-y-4">
        <Card>
          <CardHeader>
            <CardTitle className="text-lg flex items-center">
              <Info className="h-5 w-5 mr-2" />
              Properties
            </CardTitle>
            <CardDescription>
              Select an element to view its properties
            </CardDescription>
          </CardHeader>
        </Card>
      </div>
    );
  }

  return (
    <div className="space-y-4">
      {/* Selected Element Properties */}
      <AnimatePresence>
        {selectedElement && (
          <motion.div
            initial={{ opacity: 0, y: 20 }}
            animate={{ opacity: 1, y: 0 }}
            exit={{ opacity: 0, y: -20 }}
          >
            <Card>
              <CardHeader>
                <CardTitle className="text-lg flex items-center">
                  <Target className="h-5 w-5 mr-2" />
                  Selected Element
                </CardTitle>
              </CardHeader>
              <CardContent className="space-y-3">
                <div className="space-y-2">
                  <div className="flex items-center justify-between">
                    <Label className="text-sm">Type</Label>
                    <Badge variant="secondary">{selectedElement.type}</Badge>
                  </div>

                  <div className="flex items-center justify-between">
                    <Label className="text-sm">Name</Label>
                    <span className="text-sm font-mono">{selectedElement.name}</span>
                  </div>

                  {selectedElement.content && (
                    <div className="space-y-1">
                      <Label className="text-sm">Content</Label>
                      <div className="p-2 bg-muted rounded text-sm font-mono">
                        {selectedElement.content}
                      </div>
                    </div>
                  )}

                  <Separator />

                  <div className="grid grid-cols-2 gap-2 text-sm">
                    <div>
                      <Label className="text-xs text-muted-foreground">X Position</Label>
                      <div className="font-mono">{selectedElement.x || 0}px</div>
                    </div>
                    <div>
                      <Label className="text-xs text-muted-foreground">Y Position</Label>
                      <div className="font-mono">{selectedElement.y || 0}px</div>
                    </div>
                    <div>
                      <Label className="text-xs text-muted-foreground">Width</Label>
                      <div className="font-mono">{selectedElement.width || 120}px</div>
                    </div>
                    <div>
                      <Label className="text-xs text-muted-foreground">Height</Label>
                      <div className="font-mono">{selectedElement.height || 40}px</div>
                    </div>
                  </div>
                </div>
              </CardContent>
            </Card>
          </motion.div>
        )}
      </AnimatePresence>

      {/* Diagram Information */}
      {diagram && (
        <Card>
          <CardHeader>
            <CardTitle className="text-lg flex items-center">
              <Layers className="h-5 w-5 mr-2" />
              Diagram Info
            </CardTitle>
          </CardHeader>
          <CardContent className="space-y-3">
            <div className="space-y-2">
              <div className="flex items-center justify-between">
                <Label className="text-sm">Name</Label>
                <span className="text-sm font-medium">{diagram.name}</span>
              </div>

              <div className="flex items-center justify-between">
                <Label className="text-sm">Elements</Label>
                <Badge variant="outline">{diagram.elements?.length || 0}</Badge>
              </div>

              <div className="flex items-center justify-between">
                <Label className="text-sm">Connections</Label>
                <Badge variant="outline">{diagram.connections?.length || 0}</Badge>
              </div>

              <Separator />

              <div className="grid grid-cols-2 gap-2 text-sm">
                <div>
                  <Label className="text-xs text-muted-foreground">Width</Label>
                  <div className="font-mono">{diagram.bounds?.width || 0}px</div>
                </div>
                <div>
                  <Label className="text-xs text-muted-foreground">Height</Label>
                  <div className="font-mono">{diagram.bounds?.height || 0}px</div>
                </div>
              </div>
            </div>
          </CardContent>
        </Card>
      )}

      {/* Grammar Information */}
      {grammar && (
        <Card>
          <CardHeader>
            <CardTitle className="text-lg flex items-center">
              <FileText className="h-5 w-5 mr-2" />
              Grammar Info
            </CardTitle>
          </CardHeader>
          <CardContent className="space-y-3">
            <div className="space-y-2">
              <div className="flex items-center justify-between">
                <Label className="text-sm">Name</Label>
                <span className="text-sm font-medium">{grammar.name}</span>
              </div>

              <div className="flex items-center justify-between">
                <Label className="text-sm">Format</Label>
                <Badge variant="secondary">{grammar.format}</Badge>
              </div>

              {grammar.metadata && (
                <>
                  <Separator />

                  <div className="space-y-2">
                    <div className="flex items-center justify-between">
                      <Label className="text-sm flex items-center">
                        <Hash className="h-3 w-3 mr-1" />
                        Rules
                      </Label>
                      <Badge variant="outline">{grammar.metadata.rules}</Badge>
                    </div>

                    <div className="flex items-center justify-between">
                      <Label className="text-sm flex items-center">
                        <Type className="h-3 w-3 mr-1" />
                        Terminals
                      </Label>
                      <Badge variant="outline">{grammar.metadata.terminals}</Badge>
                    </div>

                    {grammar.metadata.inheritance && (
                      <div className="flex items-center justify-between">
                        <Label className="text-sm flex items-center">
                          <GitBranch className="h-3 w-3 mr-1" />
                          Inheritance
                        </Label>
                        <Badge variant="default">Enabled</Badge>
                      </div>
                    )}

                    {grammar.metadata.contextSensitive && (
                      <div className="flex items-center justify-between">
                        <Label className="text-sm flex items-center">
                          <Target className="h-3 w-3 mr-1" />
                          Context-Sensitive
                        </Label>
                        <Badge variant="default">Enabled</Badge>
                      </div>
                    )}

                    <Separator />

                    <div className="grid grid-cols-2 gap-2 text-sm">
                      <div>
                        <Label className="text-xs text-muted-foreground">Size</Label>
                        <div className="font-mono">{grammar.metadata.size || 0} chars</div>
                      </div>
                      <div>
                        <Label className="text-xs text-muted-foreground">Lines</Label>
                        <div className="font-mono">{grammar.metadata.lines || 0}</div>
                      </div>
                    </div>
                  </div>
                </>
              )}
            </div>
          </CardContent>
        </Card>
      )}

      {/* Statistics */}
      {(diagram || grammar) && (
        <Card>
          <CardHeader>
            <CardTitle className="text-lg flex items-center">
              <BarChart3 className="h-5 w-5 mr-2" />
              Statistics
            </CardTitle>
          </CardHeader>
          <CardContent className="space-y-3">
            <div className="space-y-2">
              {diagram && (
                <>
                  <div className="flex items-center justify-between">
                    <Label className="text-sm">Element Types</Label>
                    <div className="flex space-x-1">
                      {['start', 'end', 'terminal', 'non-terminal'].map(type => {
                        const count = diagram.elements?.filter(el => el.type === type).length || 0;
                        return count > 0 ? (
                          <Badge key={type} variant="outline" className="text-xs">
                            {type}: {count}
                          </Badge>
                        ) : null;
                      })}
                    </div>
                  </div>
                </>
              )}

              {grammar && grammar.metadata && (
                <>
                  <div className="flex items-center justify-between">
                    <Label className="text-sm">Complexity</Label>
                    <Badge variant={
                      grammar.metadata.rules > 20 ? 'destructive' :
                        grammar.metadata.rules > 10 ? 'default' : 'secondary'
                    }>
                      {grammar.metadata.rules > 20 ? 'High' :
                        grammar.metadata.rules > 10 ? 'Medium' : 'Low'}
                    </Badge>
                  </div>

                  <div className="flex items-center justify-between">
                    <Label className="text-sm">Features</Label>
                    <div className="flex space-x-1">
                      {grammar.metadata.inheritance && (
                        <Badge variant="outline" className="text-xs">Inheritance</Badge>
                      )}
                      {grammar.metadata.contextSensitive && (
                        <Badge variant="outline" className="text-xs">Context</Badge>
                      )}
                    </div>
                  </div>
                </>
              )}
            </div>
          </CardContent>
        </Card>
      )}
    </div>
  );
};

export default PropertiesPanel;

