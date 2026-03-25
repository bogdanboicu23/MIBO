import { useRouter } from "@/routes/hooks";
import { paths } from "@/routes/paths";
import { useAuthContext } from "@/auth/hooks";

export default function IntroPage() {
    const router = useRouter();
    const { user } = useAuthContext();

    return (
        <div className="flex min-h-[calc(100vh-64px)] items-center justify-center px-5">
            <div className="max-w-md w-full text-center space-y-8">
                <div
                    className="space-y-3"
                    style={{ animation: "slideUp 0.5s cubic-bezier(.22,1,.36,1) both" }}
                >
                    <h1 className="text-4xl font-bold tracking-tight text-zinc-900 dark:text-white">
                        Bună, {user?.firstName ?? "there"}!
                    </h1>
                    <p className="text-base text-zinc-500 dark:text-zinc-400 leading-relaxed">
                        Bine ai venit pe MIBO — asistentul tău AI care generează interfețe interactive direct în conversație.
                    </p>
                </div>

                <button
                    onClick={() => router.push(paths.chat)}
                    className="rounded-xl bg-zinc-900 px-8 py-3.5 text-sm font-semibold text-white hover:bg-zinc-800 dark:bg-white dark:text-zinc-900 dark:hover:bg-zinc-100 transition-colors"
                    style={{ animation: "slideUp 0.5s 0.12s cubic-bezier(.22,1,.36,1) both" }}
                >
                    Deschide Chat →
                </button>

                <style>{`
                    @keyframes slideUp {
                        from { opacity: 0; transform: translateY(16px); }
                        to   { opacity: 1; transform: translateY(0); }
                    }
                `}</style>
            </div>
        </div>
    );
}
