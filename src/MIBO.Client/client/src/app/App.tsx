import { ThemeProvider } from "./providers/ThemeProvider";
import { I18nProvider } from "./providers/I18nProvider";
import { Bounce, ToastContainer } from "react-toastify";
import { useTheme } from "../hooks/useTheme.ts";
import { AxiosProvider } from "../axios/axios-provider";
import { AuthProvider } from "@/auth/context/jwt";
import { usePathname } from "@/routes/hooks";
import { type ReactNode, useEffect } from "react";

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
                            hideProgressBar={false}
                            newestOnTop={false}
                            closeOnClick={false}
                            rtl={false}
                            pauseOnFocusLoss
                            draggable
                            pauseOnHover
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

    return null;
}