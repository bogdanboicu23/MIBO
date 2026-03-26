import { useState, useEffect, type ReactNode } from 'react';

import { normalizeReturnTo } from "src/routes/paths";

import { useAuthContext } from '../hooks';
import { useRouter, useSearchParams } from "../../routes/hooks";
import { LoadingOverlay } from "../../components/ui";

type GuestGuardProps = {
    children: ReactNode;
};

export function GuestGuard({ children }: GuestGuardProps) {
    const { loading, authenticated } = useAuthContext();
    const router = useRouter();
    const searchParams = useSearchParams();

    const [isChecking, setIsChecking] = useState<boolean>(true);

    const checkPermissions = async (): Promise<void> => {
        if (loading) {
            return;
        }

        if (authenticated) {
            router.replace(normalizeReturnTo(searchParams.get('returnTo')));
            return;
        }

        setIsChecking(false);
    };

    useEffect(() => {
        checkPermissions();
         
    }, [authenticated, loading, searchParams]);

    if (isChecking) {
        return (
            <LoadingOverlay show fullscreen />
        );
    }

    return <>{children}</>;
}
