import React from 'react';
import { createRoot } from 'react-dom/client';
import App from './App.jsx';

const style = document.createElement('style');
style.textContent = 'html,body,#root{width:100%;height:100%;margin:0;overflow:hidden;}';
document.head.appendChild(style);

createRoot(document.getElementById('root')).render(
  <React.StrictMode>
    <App />
  </React.StrictMode>
);
