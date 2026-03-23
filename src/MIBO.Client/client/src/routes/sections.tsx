import type { RouteObject } from 'react-router';

import { lazy, Suspense } from 'react';

import { AuthGuard } from "../auth/guard/auth-guard";
import { GuestGuard } from "../auth/guard/guest-guard";
import { LoadingOverlay } from "@/components/ui";
import ChatPage from "@/pages/ChatPage.tsx";
import SettingsPage from "@/pages/SettingsPage.tsx";
import AppLayout from "@/layouts/AppLayout.tsx";
import { Outlet } from "react-router-dom";


export const Signup = lazy(() => import('@/pages/Signup.tsx'));
export const Login = lazy(() => import('@/pages/Login.tsx'));
export const LandingPage = lazy(() => import('@/pages/LandingPage.tsx'));
const renderFallback = () => (
    <LoadingOverlay show fullscreen />
);

export const routesSection: RouteObject[] = [
    {
        element: (
            <Suspense fallback={renderFallback()}>
                <AppLayout />
            </Suspense>
        ),
        children: [
            { index: true, element: <LandingPage/> },
            {
                path: "/chat",
                element: (
                    <AuthGuard>
                        <ChatPage />
                    </AuthGuard>
                ),
            },
            {
                path: "/settings",
                element: (
                    <AuthGuard>
                        <SettingsPage />
                    </AuthGuard>
                ),
            }
        ],
    },
    {
        path: "auth",
        element: (
            <GuestGuard>
                <Suspense fallback={renderFallback()}>
                    <Outlet />
                </Suspense>
            </GuestGuard>
        ),
        children: [
            {
                path: "login",
                element: <Login />,
            },
            {
                path: "signup",
                element: <Signup />,
            },
        ],
    },
];
