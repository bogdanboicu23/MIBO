import { RouterProvider } from "react-router-dom";
import { router } from "./routes";
import { ThemeProvider } from "./providers/ThemeProvider";
import { I18nProvider } from "./providers/I18nProvider";

export default function App() {
    return (
        <ThemeProvider>
            <I18nProvider>
                <RouterProvider router={router} />
            </I18nProvider>
        </ThemeProvider>
    );
}
