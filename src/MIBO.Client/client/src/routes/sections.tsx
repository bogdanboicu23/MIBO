import type { RouteObject } from 'react-router';

import { lazy, Suspense } from 'react';

// import { AuthGuard } from "../auth/guard/auth-guard";
import { GuestGuard } from "../auth/guard/guest-guard";
import { LoadingOverlay } from "@/components/ui";
import ChatPage from "@/pages/ChatPage.tsx";
import AppLayout from "@/layouts/AppLayout.tsx";


export const Signup = lazy(() => import('@/pages/Signup.tsx'));
export const Login = lazy(() => import('@/pages/Login.tsx'));

const renderFallback = () => (
    <LoadingOverlay show fullscreen />
);

export const routesSection: RouteObject[] = [
        {
            element: (
                // <AuthGuard>
                <Suspense fallback={renderFallback()}>
                    <AppLayout />
                </Suspense>
                // </AuthGuard>
            ),
            children: [
                { index: true, element: <ChatPage /> },
            ],
        },
        {
            path: 'auth',
            children:
                [
                    {
                        path: 'login',
                        element: (
                            <GuestGuard>
                                <Login />
                            </GuestGuard>
                        ),
                    },
                    {
                        path: 'signup',
                        element: (
                            <GuestGuard>
                                <Signup />
                            </GuestGuard>
                        ),
                    },
                ],
        }
    ]
;
