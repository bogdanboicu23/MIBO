import { useMemo, useState } from "react";
import { z } from "zod";
import { Button, Card, Divider, Input } from "@/components/ui";
import { ThemeToggle } from "@/components/chat/ThemeToggle.tsx";
import { RouterLink } from "@/routes/components";
import { paths } from "@/routes/paths.ts";
import { useRouter } from "@/routes/hooks";

import type { UserType } from "@/auth/types";
import { useAxios } from "@/axios/hooks";
import { endpoints } from "@/axios/endpoints.ts";

const passwordHint = "Parola trebuie să aibă minim 8 caractere și să conțină litere mari, mici și cifre.";

const schema = z
    .object({
        username: z
            .string()
            .trim()
            .min(3, "Username-ul trebuie să aibă minim 3 caractere.")
            .max(30, "Username-ul trebuie să aibă maxim 30 caractere.")
            .regex(/^[a-zA-Z0-9._-]+$/, "Folosește doar litere/cifre și . _ -"),
        email: z.string().trim().email("Email invalid."),
        firstName: z.string().trim().min(2, "Prenumele trebuie să aibă minim 2 caractere."),
        lastName: z.string().trim().min(2, "Numele trebuie să aibă minim 2 caractere."),
        password: z
            .string()
            .min(8, passwordHint)
            .regex(/[a-z]/, passwordHint)
            .regex(/[A-Z]/, passwordHint)
            .regex(/[0-9]/, passwordHint),
        confirmPassword: z.string().min(1, "Confirmă parola."),
    })
    .refine((v) => v.password === v.confirmPassword, {
        message: "Parolele nu coincid.",
        path: ["confirmPassword"],
    });

type SignupForm = UserType & { password: string; confirmPassword: string };

export default function Signup() {
    const router = useRouter();
    const { api } = useAxios();

    const [user, setUser] = useState<UserType>({
        username: "",
        email: "",
        firstName: "",
        lastName: "",
    });

    const [passwords, setPasswords] = useState({ password: "", confirmPassword: "" });

    const [errors, setErrors] = useState<Record<string, string>>({});
    const [busy, setBusy] = useState(false);
    const [serverError, setServerError] = useState<string | null>(null);

    const canSubmit = useMemo(() => {
        return (
            !!user.username.trim() &&
            !!user.email.trim() &&
            !!user.firstName.trim() &&
            !!user.lastName.trim() &&
            !!passwords.password &&
            !!passwords.confirmPassword &&
            !busy
        );
    }, [user, passwords, busy]);

    function validate() {
        const payload: SignupForm = { ...user, ...passwords };
        const parsed = schema.safeParse(payload);

        if (parsed.success) {
            setErrors({});
            return true;
        }

        const next: Record<string, string> = {};
        for (const issue of parsed.error.issues) {
            const key = String(issue.path[0] ?? "form");
            if (!next[key]) next[key] = issue.message;
        }
        setErrors(next);
        return false;
    }

    async function onSubmit(e: React.FormEvent) {
        e.preventDefault();
        setServerError(null);

        if (!validate()) return;

        setBusy(true);
        try {
            await api.post(endpoints.auth.signup, {
                ...user,
                password: passwords.password,
            });
            router.push(paths.auth.login);
        } catch (err: any) {
            setServerError(err?.message ?? "Crearea contului a eșuat. Încearcă din nou.");
        } finally {
            setBusy(false);
        }
    }

    return (
        <div className="min-h-screen w-full bg-zinc-50 text-zinc-900 dark:bg-zinc-950 dark:text-zinc-100">
            <div className="mx-auto flex min-h-screen max-w-6xl items-center justify-center px-4 py-10">
                <div className="w-full max-w-md">
                    <div className="mb-6 flex items-center justify-between">
                        <div>
                            <div className="text-sm text-zinc-500 dark:text-zinc-400">Creează cont nou</div>
                            <h1 className="text-2xl font-semibold tracking-tight">Sign up</h1>
                        </div>
                        <ThemeToggle />
                    </div>

                    <Card className="p-5 sm:p-6">
                        <form onSubmit={onSubmit} className="space-y-4">
                            <div className="grid gap-4 sm:grid-cols-2">
                                <div className="sm:col-span-1">
                                    <label
                                        className="mb-1.5 block text-sm font-medium text-zinc-700 dark:text-zinc-300">
                                        Prenume
                                    </label>
                                    <Input
                                        value={user.firstName}
                                        onChange={(e) => {
                                            setUser({ ...user, firstName: e.target.value });
                                            setErrors({ ...errors, firstName: "" });
                                            setServerError(null);
                                        }}
                                        placeholder="Prenume"
                                        autoComplete="given-name"
                                    />
                                    {errors.firstName ? (
                                        <div
                                            className="mt-1 text-xs text-red-600 dark:text-red-400">{errors.firstName}</div>
                                    ) : null}
                                </div>

                                <div className="sm:col-span-1">
                                    <label
                                        className="mb-1.5 block text-sm font-medium text-zinc-700 dark:text-zinc-300">
                                        Nume
                                    </label>
                                    <Input
                                        value={user.lastName}
                                        onChange={(e) => {
                                            setUser({ ...user, lastName: e.target.value });
                                            setErrors({ ...errors, lastName: "" });
                                            setServerError(null);
                                        }}
                                        placeholder="Nume"
                                        autoComplete="family-name"
                                    />
                                    {errors.lastName ? (
                                        <div
                                            className="mt-1 text-xs text-red-600 dark:text-red-400">{errors.lastName}</div>
                                    ) : null}
                                </div>
                            </div>

                            <div>
                                <label className="mb-1.5 block text-sm font-medium text-zinc-700 dark:text-zinc-300">
                                    Username
                                </label>
                                <Input
                                    value={user.username}
                                    onChange={(e) => {
                                        setUser({ ...user, username: e.target.value });
                                        setErrors({ ...errors, username: "" });
                                        setServerError(null);
                                    }}
                                    placeholder="username"
                                    autoComplete="username"
                                />
                                {errors.username ? (
                                    <div className="mt-1 text-xs text-red-600 dark:text-red-400">{errors.username}</div>
                                ) : null}
                            </div>

                            <div>
                                <label className="mb-1.5 block text-sm font-medium text-zinc-700 dark:text-zinc-300">
                                    Email
                                </label>
                                <Input
                                    type="email"
                                    value={user.email}
                                    onChange={(e) => {
                                        setUser({ ...user, email: e.target.value });
                                        setErrors({ ...errors, email: "" });
                                        setServerError(null);
                                    }}
                                    placeholder="example@mail.com"
                                    autoComplete="email"
                                />
                                {errors.email ? (
                                    <div className="mt-1 text-xs text-red-600 dark:text-red-400">{errors.email}</div>
                                ) : null}
                            </div>

                            <div>
                                <label className="mb-1.5 block text-sm font-medium text-zinc-700 dark:text-zinc-300">
                                    Parolă
                                </label>
                                <Input
                                    type="password"
                                    value={passwords.password}
                                    onChange={(e) => {
                                        setPasswords({ ...passwords, password: e.target.value });
                                        setErrors({ ...errors, password: "" });
                                        setServerError(null);
                                    }}
                                    placeholder="••••••••"
                                    autoComplete="new-password"
                                />
                                {
                                    errors.password && (
                                        <div className="mt-1 text-xs text-red-600 dark:text-red-400">{errors.password}</div>
                                    )
                                }
                            </div>

                            <div>
                                <label className="mb-1.5 block text-sm font-medium text-zinc-700 dark:text-zinc-300">
                                    Confirmă parola
                                </label>
                                <Input
                                    type="password"
                                    value={passwords.confirmPassword}
                                    onChange={(e) => {
                                        setPasswords({ ...passwords, confirmPassword: e.target.value });
                                        setErrors({ ...errors, confirmPassword: "" });
                                        setServerError(null);
                                    }}
                                    placeholder="••••••••"
                                    autoComplete="new-password"
                                />
                                {errors.confirmPassword ? (
                                    <div
                                        className="mt-1 text-xs text-red-600 dark:text-red-400">{errors.confirmPassword}</div>
                                ) : null}
                            </div>

                            {serverError ? (
                                <div
                                    className="rounded-lg border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-700 dark:border-red-900/40 dark:bg-red-950/30 dark:text-red-300">
                                    {serverError}
                                </div>
                            ) : null}

                            <Button type="submit" className="w-full" disabled={!canSubmit}>
                                {busy ? "Se creează contul..." : "Creează cont"}
                            </Button>

                            <div className="text-sm text-zinc-500 dark:text-zinc-400">
                                Ai deja cont?{" "}
                                <RouterLink
                                    href={paths.auth.login}
                                    className="font-medium text-zinc-900 hover:underline dark:text-zinc-100"
                                >
                                    Login
                                </RouterLink>
                            </div>

                            <Divider />

                            <div className="text-xs text-zinc-500 dark:text-zinc-400">
                                Prin crearea contului confirmi că ești de acord cu termenii și politica de
                                confidențialitate.
                            </div>
                        </form>
                    </Card>
                </div>
            </div>
        </div>
    );
}