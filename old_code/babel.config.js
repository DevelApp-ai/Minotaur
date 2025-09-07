module.exports = {
  presets: [
    ['@babel/preset-env', {
      targets: {
        node: 'current',
      },
    }],
    ['@babel/preset-react', {
      runtime: 'automatic',
    }],
    '@babel/preset-typescript',
  ],
  plugins: [
    '@babel/plugin-proposal-class-properties',
    '@babel/plugin-proposal-private-methods',
    '@babel/plugin-transform-private-property-in-object',
  ],
  env: {
    test: {
      presets: [
        ['@babel/preset-env', {
          targets: {
            node: 'current',
          },
        }],
        ['@babel/preset-react', {
          runtime: 'automatic',
        }],
        '@babel/preset-typescript',
      ],
    },
  },
};

