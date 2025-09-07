/**
 * TypeScript declarations for Electron API exposed through preload script
 */
declare global {
  interface Window {
    electron: {
      fileSystem: {
        // eslint-disable-next-line max-len
        saveFile: (content: string, defaultPath?: string, filters?: Array<{name: string, extensions: string[]}>) => Promise<{success: boolean, filePath?: string}>;
        // eslint-disable-next-line max-len
        openFile: (defaultPath?: string, filters?: Array<{name: string, extensions: string[]}>) => Promise<{success: boolean, filePath?: string, content?: string}>;
      };
      parser: {
        // eslint-disable-next-line max-len
        exportParser: (grammar: any, outputDir?: string) => Promise<{success: boolean, filePath?: string, error?: string}>;
      };
      app: {
        getVersion: () => string;
      };
    };
  }
}

export {};
