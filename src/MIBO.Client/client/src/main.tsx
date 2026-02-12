import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import App from "./app/App.tsx";
import { bootstrapPlugins } from "./components/sandbox/uiRuntime/bootstrapPlugins.ts";

bootstrapPlugins();

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <App />
  </StrictMode>,
)
