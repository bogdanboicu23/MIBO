import { Link } from "react-router-dom";
import { paths } from "../app/routes/paths";

export default function NotFoundPage() {
    return (
        <div className="grid h-screen place-items-center">
            <div className="text-center">
                <div className="text-2xl font-semibold">404</div>
                <div className="mt-2 text-sm text-zinc-500 dark:text-zinc-400">Page not found</div>
                <Link to={paths.chat} className="mt-6 inline-flex rounded-xl border px-4 py-2 text-sm">
                    Go home
                </Link>
            </div>
        </div>
    );
}
