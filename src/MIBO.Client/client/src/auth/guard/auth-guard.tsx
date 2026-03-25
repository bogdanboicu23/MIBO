import { useState, useEffect, type ReactNode } from 'react';
import { useLocation } from 'react-router-dom';

import { paths } from 'src/routes/paths';
import { useRouter } from 'src/routes/hooks';

import { useAuthContext } from '../hooks';
import { LoadingOverlay } from "../../components/ui";


type AuthGuardProps = {
    children: ReactNode;
};

export function AuthGuard({ children }: AuthGuardProps) {
    const router = useRouter();
    const location = useLocation();

    const { authenticated, loading } = useAuthContext();

    const [isChecking, setIsChecking] = useState<boolean>(true);

    const createRedirectPath = (currentPath: string) => {
        const returnTo = `${location.pathname}${location.search}${location.hash}`;
        const queryString = new URLSearchParams({ returnTo }).toString();
        return `${currentPath}?${queryString}`;
    };

    const checkPermissions = async (): Promise<void> => {
        if (loading) {
            return;
        }

        if (!authenticated) {
            const redirectPath = createRedirectPath(paths.auth.login);

            router.replace(redirectPath);

            return;
        }

        setIsChecking(false);
    };

    useEffect(() => {
        checkPermissions();
         
    }, [authenticated, loading, location.pathname, location.search, location.hash]);

    if (isChecking) {
        return (
            <LoadingOverlay show fullscreen />
        );
    }

    return <>{children}</>;
}
