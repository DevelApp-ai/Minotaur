// Add missing type definitions
declare global {
  interface CodePosition {
    line: number;
    column: number;
    offset: number;
  }

  namespace NodeJS {
    interface Timeout {}
  }
}

export {};
