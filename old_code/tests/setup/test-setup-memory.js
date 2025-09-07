// Global test setup for memory monitoring
module.exports = async () => {
  console.log('🔍 Starting memory-optimized test environment...');
  
  // Force garbage collection if available
  if (global.gc) {
    global.gc();
    console.log('✅ Initial garbage collection completed');
  }
  
  // Set Node.js memory limits
  const memoryUsage = process.memoryUsage();
  console.log(`📊 Initial memory usage: RSS ${Math.round(memoryUsage.rss / 1024 / 1024)}MB, Heap ${Math.round(memoryUsage.heapUsed / 1024 / 1024)}MB`);
  
  // Set up memory monitoring
  global.memoryCheckpoints = [];
  
  global.logMemoryCheckpoint = (label) => {
    const usage = process.memoryUsage();
    const checkpoint = {
      label,
      timestamp: Date.now(),
      rss: Math.round(usage.rss / 1024 / 1024),
      heapUsed: Math.round(usage.heapUsed / 1024 / 1024),
      heapTotal: Math.round(usage.heapTotal / 1024 / 1024)
    };
    global.memoryCheckpoints.push(checkpoint);
    console.log(`📍 ${label}: RSS ${checkpoint.rss}MB, Heap ${checkpoint.heapUsed}/${checkpoint.heapTotal}MB`);
    return checkpoint;
  };
  
  // Log initial checkpoint
  global.logMemoryCheckpoint('test-setup');
  
  // Set up periodic memory monitoring
  global.memoryMonitor = setInterval(() => {
    const usage = process.memoryUsage();
    const rssGB = usage.rss / 1024 / 1024 / 1024;
    
    if (rssGB > 1.0) {
      console.log(`⚠️  High memory usage detected: ${rssGB.toFixed(2)}GB RSS`);
      
      if (rssGB > 1.5 && global.gc) {
        console.log('🧹 Forcing garbage collection...');
        global.gc();
        const afterGC = process.memoryUsage();
        console.log(`✅ After GC: RSS ${Math.round(afterGC.rss / 1024 / 1024)}MB`);
      }
    }
  }, 10000); // Check every 10 seconds
  
  console.log('✅ Memory monitoring setup complete');
};

