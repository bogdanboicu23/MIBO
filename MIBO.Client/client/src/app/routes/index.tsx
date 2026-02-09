import { createBrowserRouter } from "react-router-dom";
import AppLayout from "../../layouts/AppLayout";
import ChatPage from "../../pages/ChatPage";
import SettingsPage from "../../pages/SettingsPage";
import NotFoundPage from "../../pages/NotFoundPage";
import { paths } from "./paths";

export const router = createBrowserRouter([
    {
        element: <AppLayout />,
        children: [
            { path: paths.chat, element: <ChatPage /> },
            { path: paths.settings, element: <SettingsPage /> },
            { path: "*", element: <NotFoundPage /> },
        ],
    },
]);
