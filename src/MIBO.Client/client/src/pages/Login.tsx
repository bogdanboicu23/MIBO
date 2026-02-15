import { useMemo, useState } from "react";
import { z } from "zod";
import Turnstile from "react-turnstile";

import { Button, Card, Divider, Input } from "@/components/ui";
import { ThemeToggle } from "@/components/chat/ThemeToggle.tsx";
import { RouterLink } from "@/routes/components";
import { paths } from "@/routes/paths.ts";
import { useRouter } from "@/routes/hooks";
import { useAuthContext } from "@/auth/hooks";
import type { LoginData } from "@/auth/types.ts";
import { useTheme } from "@/hooks/useTheme.ts";

const schema = z.object({
    email: z.string().trim().min(3, "Introdu email (minim 3 caractere)."),
    password: z.string().min(8, "Parola trebuie să aibă minim 8 caractere."),
});

export default function Login() {
    const router = useRouter();
    const { login } = useAuthContext();
    const { theme } = useTheme();

    const [form, setForm] = useState<LoginData>({ email: "", password: "" });
    const [errors, setErrors] = useState<Record<string, string>>({});
    const [busy, setBusy] = useState(false);
    const [serverError, setServerError] = useState<string | null>(null);

    const [tsToken, setTsToken] = useState<string | null>(null);

    const canSubmit = useMemo(() => {
        return !!form.email.trim() && !!form.password && !!tsToken && !busy;
    }, [form.email, form.password, tsToken, busy]);

    function validate() {
        const parsed = schema.safeParse(form);
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

        if (!tsToken) {
            setServerError("Confirmă captcha înainte de autentificare.");
            return;
        }

        if (!validate()) return;

        setBusy(true);
        try {
            await login({ email: form.email, password: form.password, turnstileToken: tsToken });

            setTsToken(null);

            router.push(paths.root);
        } catch (err: any) {
            setServerError(err?.message ?? "Autentificare eșuată. Încearcă din nou.");
            setTsToken(null);
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
                            <div className="text-sm text-zinc-500 dark:text-zinc-400">Bine ai revenit</div>
                            <h1 className="text-2xl font-semibold tracking-tight">Login</h1>
                        </div>
                        <ThemeToggle />
                    </div>

                    <Card className="p-5 sm:p-6">
                        <form onSubmit={onSubmit} className="space-y-4">
                            <div>
                                <label className="mb-1.5 block text-sm font-medium text-zinc-700 dark:text-zinc-300">
                                    Email
                                </label>
                                <Input
                                    value={form.email}
                                    onChange={(e) => {
                                        setForm({ ...form, email: e.target.value });
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
                                    value={form.password}
                                    onChange={(e) => {
                                        setForm({ ...form, password: e.target.value });
                                        setErrors({ ...errors, password: "" });
                                        setServerError(null);
                                    }}
                                    placeholder="••••••••"
                                    autoComplete="current-password"
                                />
                                {errors.password ? (
                                    <div className="mt-1 text-xs text-red-600 dark:text-red-400">{errors.password}</div>
                                ) : null}
                            </div>

                            {/* Turnstile */}
                            <div className="pt-1">
                                <div className="flex w-full justify-center">
                                    <Turnstile
                                        sitekey={import.meta.env.VITE_TURNSTILE_SITE_KEY}
                                        onVerify={(t) => setTsToken(t)}
                                        onExpire={() => setTsToken(null)}
                                        onError={() => setTsToken(null)}
                                        theme={theme === "dark" ? "dark" : "light"}
                                    />
                                </div>

                                {!tsToken ? (
                                    <div className="mt-2 text-center text-xs text-zinc-500 dark:text-zinc-400">
                                        Completează captcha pentru a activa butonul de login.
                                    </div>
                                ) : null}
                            </div>

                            {serverError ? (
                                <div
                                    className="rounded-lg border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-700 dark:border-red-900/40 dark:bg-red-950/30 dark:text-red-300">
                                    {serverError}
                                </div>
                            ) : null}

                            <Button type="submit" className="w-full" disabled={!canSubmit}>
                                {busy ? "Se autentifică..." : "Intră în cont"}
                            </Button>

                            <div className="flex items-center justify-between text-sm">
                                <RouterLink
                                    href={paths.auth.forgotPassword}
                                    className="text-zinc-600 hover:text-zinc-900 dark:text-zinc-400 dark:hover:text-zinc-100"
                                >
                                    Ai uitat parola?
                                </RouterLink>

                                <span className="text-zinc-500 dark:text-zinc-400">
                                    Nu ai cont?{" "}
                                    <RouterLink
                                        href={paths.auth.signup}
                                        className="font-medium text-zinc-900 hover:underline dark:text-zinc-100"
                                    >
                                Sign up
                              </RouterLink>
                            </span>
                            </div>

                            <Divider />

                            <div className="text-xs text-zinc-500 dark:text-zinc-400">
                                Prin login ești de acord cu termenii și politica de confidențialitate.
                            </div>
                        </form>
                    </Card>
                </div>
            </div>
        </div>
    );
}
