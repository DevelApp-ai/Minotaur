const path = require('path');

module.exports = {
  webpack: {
    configure: (webpackConfig) => {
      // Add fallbacks for Node.js modules that don't work in the browser
      webpackConfig.resolve.fallback = {
        ...webpackConfig.resolve.fallback,
        "fs": false,
        "path": "path-browserify",
        "crypto": "crypto-browserify",
        "stream": "stream-browserify",
        "util": "util",
        "buffer": "buffer",
        "process": "process/browser",
        "os": "os-browserify/browser",
        "url": "url",
        "querystring": "querystring-es3",
        "http": "stream-http",
        "https": "https-browserify",
        "net": false,
        "tls": false,
        "child_process": false
      };

      // Add plugins for polyfills
      const webpack = require('webpack');
      webpackConfig.plugins.push(
        new webpack.ProvidePlugin({
          process: 'process/browser',
          Buffer: ['buffer', 'Buffer'],
        })
      );

      // Additional module resolution settings for CI compatibility
      webpackConfig.resolve.symlinks = false;
      webpackConfig.resolve.modules = [
        path.resolve(__dirname, 'node_modules'),
        'node_modules'
      ];
      
      return webpackConfig;
    },
  },
};

