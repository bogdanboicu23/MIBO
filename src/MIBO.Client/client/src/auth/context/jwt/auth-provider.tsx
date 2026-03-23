import { type ReactNode, useState } from 'react';

import { useMemo, useEffect, useCallback } from 'react';

import { useAxios } from "src/axios/hooks";
import { endpoints } from "src/axios/endpoints";

import { parseUserFromJwt } from "./utils";
import { JWT_STORAGE_KEY } from "./constant";
import { AuthContext } from '../auth-context';

import type { LoginData, AuthState } from "../../types";
import { useRouter } from "@/routes/hooks";

type Props = {
    children: ReactNode;
};

export function AuthProvider({ children }: Props) {
    const [ state, setState ] = useState<AuthState>({ user: undefined, loading: true });
    const { jwt, axiosLogin, setJwt, ensureValidJwt } = useAxios();
    const router = useRouter();
    const login = (request: LoginData) =>
        axiosLogin
            .post(endpoints.auth.login, request)
            .then((response) => {
                const { jwtToken } = response.data;
                localStorage.setItem(JWT_STORAGE_KEY, jwtToken);
                setJwt(jwtToken);
                router.refresh();
            })
            .catch((error) => {
                throw error;
            });


    const checkUserSession = useCallback(async () => {
        try {
            setState((current) => ({ ...current, loading: true }));

            const token = await ensureValidJwt();
            if (!token) {
                setState({ user: undefined, loading: false });
                return;
            }

            const user = parseUserFromJwt(token);
            if (!user) {
                setJwt(undefined);
                setState({ user: undefined, loading: false });
                return;
            }

            setState({
                user,
                loading: false
            });
        } catch {
            setJwt(undefined);
            setState({ user: undefined, loading: false });
        }
    }, [ensureValidJwt, setJwt]);

    useEffect(() => {
        void checkUserSession();
    }, [checkUserSession, jwt]);

    // ----------------------------------------------------------------------

    const checkAuthenticated = state.user ? 'authenticated' : 'unauthenticated';

    const status = state.loading ? 'loading' : checkAuthenticated;

    const memoizedValue = useMemo(
        () => ({
            user: state.user,
            checkUserSession,
            loading: status === 'loading',
            authenticated: status === 'authenticated',
            unauthenticated: status === 'unauthenticated',
            login,
        }),
         
        [checkUserSession, state.user, status]
    );

    return <AuthContext.Provider value={memoizedValue}>{children}</AuthContext.Provider>;
}
