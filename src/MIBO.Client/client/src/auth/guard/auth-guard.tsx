import { useState, useEffect, type ReactNode } from 'react';

import { paths } from 'src/routes/paths';
import { useRouter, usePathname } from 'src/routes/hooks';

import { useAuthContext } from '../hooks';
import { LoadingOverlay } from "../../components/ui";


type AuthGuardProps = {
    children: ReactNode;
};

export function AuthGuard({ children }: AuthGuardProps) {
    const router = useRouter();
    const pathname = usePathname();

    const { authenticated, loading } = useAuthContext();

    const [isChecking, setIsChecking] = useState<boolean>(true);

    const createRedirectPath = (currentPath: string) => {
        const queryString = new URLSearchParams({ returnTo: pathname }).toString();
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
         
    }, [authenticated, loading]);

    if (isChecking) {
        return (
            <LoadingOverlay show fullscreen />
        );
    }

    return <>{children}</>;
}