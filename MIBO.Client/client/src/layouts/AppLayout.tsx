import { Outlet } from "react-router-dom";

export default function AppLayout() {
    return (
        <div className="h-screen w-screen bg-white text-zinc-900 dark:bg-zinc-950 dark:text-zinc-100">
            <Outlet />
        </div>
    );
}
