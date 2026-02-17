import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import App from "./app/App.tsx";
import { bootstrapPlugins } from "./components/sandbox/uiRuntime/bootstrapPlugins.ts";
import { createBrowserRouter, Outlet, RouterProvider } from "react-router-dom";
import { routesSection } from "@/routes/sections.tsx";

bootstrapPlugins();

const router = createBrowserRouter([
        {
            Component: () => (
                <App>
                    <Outlet />
                </App>
            ),
            children: routesSection,
        },
    ],
);

const root = createRoot(document.getElementById('root')!);

root.render(
    <StrictMode>
        <RouterProvider router={router} />
    </StrictMode>
);