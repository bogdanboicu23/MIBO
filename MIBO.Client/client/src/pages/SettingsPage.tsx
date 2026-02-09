import { Link } from "react-router-dom";
import { paths } from "../app/routes/paths";

export default function SettingsPage() {
    return (
        <div className="mx-auto max-w-3xl px-6 py-10">
            <div className="text-2xl font-semibold">Settings</div>
            <div className="mt-2 text-sm text-zinc-500 dark:text-zinc-400">
                Placeholder pentru setări (API keys, model config, tools).
            </div>

            <Link
                to={paths.chat}
                className="mt-6 inline-flex rounded-xl border border-zinc-200 bg-white px-4 py-2 text-sm hover:bg-zinc-50 dark:border-zinc-800 dark:bg-zinc-900 dark:hover:bg-zinc-800"
            >
                ← Back to chat
            </Link>
        </div>
    );
}
