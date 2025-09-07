import React from 'react';
import ReactDOM from 'react-dom/client';
import App from './components/App';
import './index.css';

// Initialize i18n
import './i18n/config/i18n';

const root = ReactDOM.createRoot(
  document.getElementById('root') as HTMLElement,
);

root.render(
  <React.StrictMode>
    <App />
  </React.StrictMode>,
);
