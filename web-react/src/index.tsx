import React from 'react';
import ReactDOM from 'react-dom/client';
import './index.css';
import App from './App';

// Completely suppress ResizeObserver errors (harmless browser warnings)
// This must run before React mounts
if (typeof window !== 'undefined') {
  // Suppress error overlay
  const suppressedErrors = ['ResizeObserver loop', 'ResizeObserver loop completed'];
  
  const originalError = window.onerror;
  window.onerror = function(message, source, lineno, colno, error) {
    if (suppressedErrors.some(e => String(message).includes(e))) {
      return true;
    }
    if (originalError) {
      return originalError.apply(this, arguments as any);
    }
    return false;
  };

  // Suppress unhandled rejection
  window.addEventListener('error', (event) => {
    if (suppressedErrors.some(e => event.message?.includes(e))) {
      event.stopImmediatePropagation();
      event.preventDefault();
    }
  }, true);

  // Suppress console.error
  const originalConsoleError = console.error;
  console.error = (...args) => {
    if (args.some(arg => suppressedErrors.some(e => String(arg).includes(e)))) {
      return;
    }
    originalConsoleError.apply(console, args);
  };
}

const root = ReactDOM.createRoot(
  document.getElementById('root') as HTMLElement
);

root.render(
  <React.StrictMode>
    <App />
  </React.StrictMode>
);
