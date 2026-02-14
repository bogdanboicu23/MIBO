import { useMemo, useState } from "react";
import { z } from "zod";
import { Button, Card, Divider, Input } from "@/components/ui";
import { ThemeToggle } from "@/components/chat/ThemeToggle.tsx";
import { RouterLink } from "@/routes/components";
import { paths } from "@/routes/paths.ts";
import { useRouter } from "@/routes/hooks";


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

export default function Signup() {
    const router = useRouter();

    const [form, setForm] = useState({
        username: "",
        email: "",
        password: "",
        confirmPassword: "",
    });

    const [errors, setErrors] = useState<Record<string, string>>({});
    const [busy, setBusy] = useState(false);
    const [serverError, setServerError] = useState<string | null>(null);

    const canSubmit = useMemo(() => {
        return (
            !!form.username.trim() &&
            !!form.email.trim() &&
            !!form.password &&
            !!form.confirmPassword &&
            !busy
        );
    }, [form, busy]);

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

        if (!validate()) return;

        setBusy(true);
        try {
            // TODO: integrare API signup
            await new Promise((r) => setTimeout(r, 650));
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
                            <div>
                                <label className="mb-1.5 block text-sm font-medium text-zinc-700 dark:text-zinc-300">
                                    Username
                                </label>
                                <Input
                                    value={form.username}
                                    onChange={(e) => {
                                        setForm({ ...form, username: e.target.value });
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
                                    autoComplete="new-password"
                                />
                                {errors.password && (
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
                                    value={form.confirmPassword}
                                    onChange={(e) => {
                                        setForm({ ...form, confirmPassword: e.target.value });
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
                                <RouterLink href={paths.auth.login}
                                            className="font-medium text-zinc-900 hover:underline dark:text-zinc-100">
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
