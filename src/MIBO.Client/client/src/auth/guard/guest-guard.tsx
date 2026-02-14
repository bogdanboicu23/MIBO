import { useState, useEffect, type ReactNode } from 'react';

import { paths } from "src/routes/paths";

import { useAuthContext } from '../hooks';
import { useRouter } from "../../routes/hooks";
import { LoadingOverlay } from "../../components/ui";

type GuestGuardProps = {
    children: ReactNode;
};

export function GuestGuard({ children }: GuestGuardProps) {
    const { loading, authenticated } = useAuthContext();
    const router = useRouter();

    // const searchParams = useSearchParams();
    // const returnTo = searchParams.get('returnTo') || '';

    const [isChecking, setIsChecking] = useState<boolean>(true);

    const checkPermissions = async (): Promise<void> => {
        if (loading) {
            return;
        }

        if (authenticated) {
            // Redirect authenticated users to the returnTo path
            // Using `window.location.href` instead of `router.replace` to avoid unnecessary re-rendering
            // that might be caused by the AuthGuard component
            // window.location.href = paths.dashboard + paths.order.list;
            router.push(paths.root);
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