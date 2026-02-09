import { ThemeProvider } from "./providers/ThemeProvider";
import { I18nProvider } from "./providers/I18nProvider";

export default function App() {
    return (
        <ThemeProvider>
            <I18nProvider>
                application
            </I18nProvider>
        </ThemeProvider>
    );
}
