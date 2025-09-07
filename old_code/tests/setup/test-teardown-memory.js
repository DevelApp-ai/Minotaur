// Global test teardown for memory analysis
const fs = require('fs');
const path = require('path');

module.exports = async () => {
  console.log('🔍 Analyzing memory usage...');
  
  // Stop memory monitoring
  if (global.memoryMonitor) {
    clearInterval(global.memoryMonitor);
  }
  
  // Log final checkpoint
  if (global.logMemoryCheckpoint) {
    global.logMemoryCheckpoint('test-teardown');
  }
  
  // Analyze memory checkpoints
  if (global.memoryCheckpoints && global.memoryCheckpoints.length > 0) {
    const checkpoints = global.memoryCheckpoints;
    const first = checkpoints[0];
    const last = checkpoints[checkpoints.length - 1];
    
    console.log('\n📊 Memory Usage Analysis:');
    console.log(`Duration: ${last.timestamp - first.timestamp}ms`);
    console.log(`RSS Growth: ${last.rss - first.rss}MB (${first.rss}MB → ${last.rss}MB)`);
    console.log(`Heap Growth: ${last.heapUsed - first.heapUsed}MB (${first.heapUsed}MB → ${last.heapUsed}MB)`);
    
    // Find peak usage
    const peakRSS = Math.max(...checkpoints.map(c => c.rss));
    const peakHeap = Math.max(...checkpoints.map(c => c.heapUsed));
    console.log(`Peak RSS: ${peakRSS}MB`);
    console.log(`Peak Heap: ${peakHeap}MB`);
    
    // Find largest memory spikes
    let largestSpike = 0;
    let spikeLocation = null;
    
    for (let i = 1; i < checkpoints.length; i++) {
      const spike = checkpoints[i].rss - checkpoints[i - 1].rss;
      if (spike > largestSpike) {
        largestSpike = spike;
        spikeLocation = checkpoints[i].label;
      }
    }
    
    if (largestSpike > 20) {
      console.log(`⚠️  Largest memory spike: +${largestSpike}MB at ${spikeLocation}`);
    }
    
    // Check for memory leaks
    if (checkpoints.length >= 3) {
      const trend = (last.heapUsed - first.heapUsed) / (checkpoints.length - 1);
      if (trend > 5) {
        console.log(`🚨 Potential memory leak detected: +${trend.toFixed(1)}MB per checkpoint`);
      }
    }
    
    // Save detailed log
    const logFile = path.join(__dirname, 'memory-test-log.json');
    fs.writeFileSync(logFile, JSON.stringify({
      summary: {
        duration: last.timestamp - first.timestamp,
        rssGrowth: last.rss - first.rss,
        heapGrowth: last.heapUsed - first.heapUsed,
        peakRSS,
        peakHeap,
        largestSpike,
        spikeLocation
      },
      checkpoints
    }, null, 2));
    
    console.log(`📝 Detailed memory log saved to ${logFile}`);
  }
  
  // Final garbage collection
  if (global.gc) {
    global.gc();
    const finalUsage = process.memoryUsage();
    console.log(`🧹 Final cleanup: RSS ${Math.round(finalUsage.rss / 1024 / 1024)}MB`);
  }
  
  console.log('✅ Memory analysis complete');
};

