import { useState } from 'react';
import { motion } from 'framer-motion';
import {
  Download,
  FileImage,
  FileText,
  Code,
  Printer,
  Share2,
  Copy,
  Check,
  X,
} from 'lucide-react';
import { Button } from '@/components/ui/button.jsx';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card.jsx';
import { Badge } from '@/components/ui/badge.jsx';
import { Label } from '@/components/ui/label.jsx';
import { Separator } from '@/components/ui/separator.jsx';
import exportService from '../services/exportService.js';

// Mock Dialog components since they're not available
const Dialog = ({ open, onOpenChange, children }) => {
  if (!open) {
    return null;
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center">
      <div
        className="fixed inset-0 bg-black/50"
        onClick={() => onOpenChange(false)}
      />
      <div className="relative bg-background border border-border rounded-lg shadow-lg max-w-md w-full mx-4">
        {children}
      </div>
    </div>
  );
};

const DialogContent = ({ children }) => <div className="p-6">{children}</div>;
const DialogHeader = ({ children }) => <div className="mb-4">{children}</div>;
const DialogTitle = ({ children }) => <h2 className="text-lg font-semibold">{children}</h2>;
const DialogDescription = ({ children }) => <p className="text-sm text-muted-foreground">{children}</p>;

const ExportDialog = ({ open, onOpenChange, diagram }) => {
  const [selectedFormat, setSelectedFormat] = useState('svg');
  const [isExporting, setIsExporting] = useState(false);
  const [exportSuccess, setExportSuccess] = useState(false);
  const [exportError, setExportError] = useState(null);

  const exportFormats = [
    {
      id: 'svg',
      name: 'SVG',
      description: 'Scalable vector graphics',
      icon: FileImage,
      extension: '.svg',
      mimeType: 'image/svg+xml',
    },
    {
      id: 'png',
      name: 'PNG',
      description: 'Portable network graphics',
      icon: FileImage,
      extension: '.png',
      mimeType: 'image/png',
    },
    {
      id: 'pdf',
      name: 'PDF',
      description: 'Portable document format',
      icon: FileText,
      extension: '.pdf',
      mimeType: 'application/pdf',
    },
    {
      id: 'html',
      name: 'HTML',
      description: 'Interactive web page',
      icon: Code,
      extension: '.html',
      mimeType: 'text/html',
    },
  ];

  const handleExport = async () => {
    if (!diagram) {
      return;
    }

    setIsExporting(true);
    setExportSuccess(false);

    try {
      let exportResult;

      // Use the real export service
      switch (selectedFormat) {
        case 'svg':
          exportResult = await exportService.exportSVG(diagram);
          break;
        case 'png':
          exportResult = await exportService.exportPNG(diagram);
          break;
        case 'pdf':
          // PDF export not fully implemented yet, fallback to generating basic PDF
          exportResult = generatePDFExport(diagram);
          break;
        case 'html':
          exportResult = await exportService.exportHTML(diagram);
          break;
        default:
          throw new Error(`Unsupported export format: ${selectedFormat}`);
      }

      if (exportResult.success) {
        // Download the export result using the service
        exportService.downloadExport(exportResult);
        setExportSuccess(true);
        setTimeout(() => {
          setExportSuccess(false);
          onOpenChange(false);
        }, 2000);
      } else {
        throw new Error(exportResult.error || 'Export failed');
      }

    } catch (error) {
    // eslint-disable-next-line no-console
      console.error('Export failed:', error);
      // Show error to user via UI state
      setExportError(error instanceof Error ? error.message : 'Export failed. Please try again.');
      setTimeout(() => {
        setExportError(null);
      }, 5000); // Clear error after 5 seconds
    } finally {
      setIsExporting(false);
    }
  };

  const generatePDFExport = (diagram) => {
    // Basic PDF export as fallback - this should be replaced with proper PDF generation
    const pdfContent = `%PDF-1.4
1 0 obj
<<
/Type /Catalog
/Pages 2 0 R
>>
endobj

2 0 obj
<<
/Type /Pages
/Kids [3 0 R]
/Count 1
>>
endobj

3 0 obj
<<
/Type /Page
/Parent 2 0 R
/MediaBox [0 0 612 792]
>>
endobj

xref
0 4
0000000000 65535 f 
0000000009 00000 n 
0000000058 00000 n 
0000000115 00000 n 
trailer
<<
/Size 4
/Root 1 0 R
>>
startxref
190
%%EOF`;

    return {
      success: true,
      content: pdfContent,
      format: 'pdf',
      filename: `${diagram.name || 'railroad-diagram'}.pdf`,
      mimeType: 'application/pdf',
    };
  };

  const copyToClipboard = async () => {
    if (!diagram) {
      return;
    }

    try {
      const success = await exportService.copyToClipboard(diagram);
      if (success) {
        setExportSuccess(true);
        setTimeout(() => setExportSuccess(false), 2000);
      }
    } catch (error) {
    // eslint-disable-next-line no-console
      console.error('Failed to copy to clipboard:', error);
    }
  };

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle className="flex items-center">
            <Download className="h-5 w-5 mr-2" />
            Export Diagram
          </DialogTitle>
          <DialogDescription>
            Export your railroad diagram in various formats
          </DialogDescription>
        </DialogHeader>

        <div className="space-y-4">
          {/* Format Selection */}
          <div className="space-y-3">
            <Label className="text-sm font-medium">Export Format</Label>
            <div className="grid grid-cols-2 gap-2">
              {exportFormats.map((format) => {
                const Icon = format.icon;
                const isSelected = selectedFormat === format.id;

                return (
                  <motion.div
                    key={format.id}
                    whileHover={{ scale: 1.02 }}
                    whileTap={{ scale: 0.98 }}
                  >
                    <Card
                      className={`cursor-pointer transition-all duration-200 ${
                        isSelected ? 'ring-2 ring-primary ring-offset-2' : 'hover:shadow-md'
                      }`}
                      onClick={() => setSelectedFormat(format.id)}
                    >
                      <CardContent className="p-3">
                        <div className="flex items-center space-x-2">
                          <Icon className="h-4 w-4" />
                          <div className="flex-1">
                            <div className="font-medium text-sm">{format.name}</div>
                            <div className="text-xs text-muted-foreground">
                              {format.description}
                            </div>
                          </div>
                          {isSelected && (
                            <Check className="h-4 w-4 text-primary" />
                          )}
                        </div>
                      </CardContent>
                    </Card>
                  </motion.div>
                );
              })}
            </div>
          </div>

          {/* Export Options */}
          <div className="space-y-3">
            <Label className="text-sm font-medium">Options</Label>
            <div className="space-y-2">
              <div className="flex items-center justify-between text-sm">
                <span>Include metadata</span>
                <Badge variant="outline">Enabled</Badge>
              </div>
              <div className="flex items-center justify-between text-sm">
                <span>High quality</span>
                <Badge variant="outline">Enabled</Badge>
              </div>
              <div className="flex items-center justify-between text-sm">
                <span>Transparent background</span>
                <Badge variant="outline">
                  {selectedFormat === 'png' ? 'Enabled' : 'N/A'}
                </Badge>
              </div>
            </div>
          </div>

          <Separator />

          {/* Error Display */}
          {exportError && (
            <div className="flex items-center space-x-2 p-3 bg-red-50 border border-red-200 rounded-md">
              <X className="h-4 w-4 text-red-500 flex-shrink-0" />
              <span className="text-sm text-red-700">{exportError}</span>
            </div>
          )}

          {/* Export Actions */}
          <div className="flex space-x-2">
            <Button
              onClick={handleExport}
              disabled={!diagram || isExporting}
              className="flex-1"
            >
              {isExporting ? (
                <>
                  <motion.div
                    animate={{ rotate: 360 }}
                    transition={{ duration: 1, repeat: Infinity, ease: 'linear' }}
                    className="mr-2"
                  >
                    <Download className="h-4 w-4" />
                  </motion.div>
                  Exporting...
                </>
              ) : exportSuccess ? (
                <>
                  <Check className="h-4 w-4 mr-2" />
                  Exported!
                </>
              ) : (
                <>
                  <Download className="h-4 w-4 mr-2" />
                  Export
                </>
              )}
            </Button>

            <Button
              variant="outline"
              onClick={copyToClipboard}
              disabled={!diagram || selectedFormat !== 'svg'}
              title="Copy SVG to clipboard"
            >
              <Copy className="h-4 w-4" />
            </Button>

            <Button
              variant="outline"
              onClick={() => window.print()}
              disabled={!diagram}
              title="Print diagram"
            >
              <Printer className="h-4 w-4" />
            </Button>
          </div>

          {/* Close Button */}
          <Button
            variant="ghost"
            onClick={() => onOpenChange(false)}
            className="w-full"
          >
            <X className="h-4 w-4 mr-2" />
            Close
          </Button>
        </div>
      </DialogContent>
    </Dialog>
  );
};

export default ExportDialog;

