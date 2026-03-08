import { useEffect, type ReactNode } from "react";
import { ThemeProvider } from "./providers/ThemeProvider";
import { I18nProvider } from "./providers/I18nProvider";
import { Bounce, ToastContainer } from "react-toastify";
import { useTheme } from "../hooks/useTheme.ts";
import { AxiosProvider } from "../axios/axios-provider";
import { AuthProvider } from "@/auth/context/jwt";
import { usePathname } from "@/routes/hooks";

type AppProps = {
    children: ReactNode;
};

export default function App({ children }: AppProps) {
    const { theme } = useTheme();
    useScrollToTop();

    return (
        <AxiosProvider>
            <AuthProvider>
                <ThemeProvider>
                    <I18nProvider>
                        {children}

                        <ToastContainer
                            position="top-right"
                            autoClose={3000}
                            theme={theme}
                            transition={Bounce}
                        />
                    </I18nProvider>
                </ThemeProvider>
            </AuthProvider>
        </AxiosProvider>
    );
}

function useScrollToTop() {
    const pathname = usePathname();

    useEffect(() => {
        window.scrollTo(0, 0);
    }, [pathname]);
}
