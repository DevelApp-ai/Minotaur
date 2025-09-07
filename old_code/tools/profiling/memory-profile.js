#!/usr/bin/env node

// Memory profiling script for Jest tests
const fs = require('fs');
const path = require('path');

// Track memory usage over time
let memoryLog = [];
let startTime = Date.now();

function logMemory(label = '') {
  const usage = process.memoryUsage();
  const timestamp = Date.now() - startTime;
  
  const entry = {
    timestamp,
    label,
    rss: Math.round(usage.rss / 1024 / 1024), // MB
    heapTotal: Math.round(usage.heapTotal / 1024 / 1024), // MB
    heapUsed: Math.round(usage.heapUsed / 1024 / 1024), // MB
    external: Math.round(usage.external / 1024 / 1024), // MB
    arrayBuffers: Math.round(usage.arrayBuffers / 1024 / 1024) // MB
  };
  
  memoryLog.push(entry);
  console.log(`[${timestamp}ms] ${label} - RSS: ${entry.rss}MB, Heap: ${entry.heapUsed}/${entry.heapTotal}MB`);
  
  return entry;
}

function saveMemoryLog() {
  const logFile = path.join(__dirname, 'memory-log.json');
  fs.writeFileSync(logFile, JSON.stringify(memoryLog, null, 2));
  console.log(`Memory log saved to ${logFile}`);
}

function analyzeMemoryGrowth() {
  if (memoryLog.length < 2) return;
  
  const first = memoryLog[0];
  const last = memoryLog[memoryLog.length - 1];
  
  console.log('\n=== Memory Growth Analysis ===');
  console.log(`Duration: ${last.timestamp}ms`);
  console.log(`RSS Growth: ${last.rss - first.rss}MB`);
  console.log(`Heap Growth: ${last.heapUsed - first.heapUsed}MB`);
  console.log(`External Growth: ${last.external - first.external}MB`);
  
  // Find largest memory spikes
  let maxRss = 0;
  let maxHeap = 0;
  let maxSpike = null;
  
  for (let i = 1; i < memoryLog.length; i++) {
    const current = memoryLog[i];
    const previous = memoryLog[i - 1];
    
    if (current.rss > maxRss) {
      maxRss = current.rss;
    }
    
    if (current.heapUsed > maxHeap) {
      maxHeap = current.heapUsed;
    }
    
    const rssSpike = current.rss - previous.rss;
    if (rssSpike > 50 && (!maxSpike || rssSpike > maxSpike.spike)) {
      maxSpike = {
        timestamp: current.timestamp,
        label: current.label,
        spike: rssSpike,
        rss: current.rss
      };
    }
  }
  
  console.log(`Peak RSS: ${maxRss}MB`);
  console.log(`Peak Heap: ${maxHeap}MB`);
  
  if (maxSpike) {
    console.log(`Largest Memory Spike: +${maxSpike.spike}MB at ${maxSpike.timestamp}ms (${maxSpike.label})`);
  }
  
  // Check for memory leaks (consistent growth)
  const samples = memoryLog.slice(-10); // Last 10 samples
  if (samples.length >= 5) {
    const trend = samples.reduce((acc, curr, idx) => {
      if (idx === 0) return acc;
      return acc + (curr.heapUsed - samples[idx - 1].heapUsed);
    }, 0) / (samples.length - 1);
    
    if (trend > 5) {
      console.log(`⚠️  Potential Memory Leak Detected: +${trend.toFixed(1)}MB/sample trend`);
    }
  }
}

// Set up periodic monitoring
const monitorInterval = setInterval(() => {
  logMemory('periodic');
  
  // Check if we're approaching memory limits
  const current = process.memoryUsage();
  const rssGB = current.rss / 1024 / 1024 / 1024;
  
  if (rssGB > 1.5) {
    console.log(`⚠️  High memory usage: ${rssGB.toFixed(2)}GB RSS`);
  }
  
  if (rssGB > 2.0) {
    console.log(`🚨 Critical memory usage: ${rssGB.toFixed(2)}GB RSS - forcing GC`);
    if (global.gc) {
      global.gc();
      logMemory('after-gc');
    }
  }
}, 5000);

// Handle process exit
process.on('exit', () => {
  clearInterval(monitorInterval);
  logMemory('exit');
  analyzeMemoryGrowth();
  saveMemoryLog();
});

process.on('SIGINT', () => {
  console.log('\nReceived SIGINT, analyzing memory...');
  process.exit(0);
});

process.on('SIGTERM', () => {
  console.log('\nReceived SIGTERM, analyzing memory...');
  process.exit(0);
});

// Export functions for use in tests
module.exports = {
  logMemory,
  analyzeMemoryGrowth,
  saveMemoryLog
};

// Start monitoring
logMemory('start');
console.log('Memory profiling started. Use Ctrl+C to stop and analyze.');

