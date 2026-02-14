import { type ReactNode, useState } from 'react';

import { jwtDecode } from "jwt-decode";
import { useMemo, useEffect, useCallback } from 'react';

import { useAxios } from "src/axios/hooks";
import { endpoints } from "src/axios/endpoints";

import { JWT_STORAGE_KEY } from "./constant";
import { AuthContext } from '../auth-context';

import type { UserType, LoginData, AuthState } from "../../types";

type Props = {
    children: ReactNode;
};

export function AuthProvider({ children }: Props) {
    const [ state, setState ] = useState<AuthState>({ user: undefined, loading: true });
    const { jwt, axiosLogin, setJwt } = useAxios();
    const login = (request: LoginData) =>
        axiosLogin
            .post(endpoints.auth.login, request)
            .then((response) => {
                const { accessToken } = response.data;
                sessionStorage.setItem(JWT_STORAGE_KEY, accessToken);
                setJwt(accessToken);
            })
            .catch((error) => {
                throw error;
            });


    const checkUserSession = useCallback(async () => {
        try {
            if (!jwt) {
                setState({ user: undefined, loading: false });
                return;
            }
            const decoded = jwtDecode<UserType>(jwt);
            setState({
                user: {
                    role: decoded.role,
                },
                loading: false
            });
        } catch {
            setState({ user: undefined, loading: false });
        }
    }, [setState, jwt]);

    useEffect(() => {
        checkUserSession();
         
    }, []);

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