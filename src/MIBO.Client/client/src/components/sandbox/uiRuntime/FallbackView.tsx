import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "../../ui/Card.tsx";
import { Badge } from "../../ui/Badge.tsx";

export function FallbackView(props: { title?: string; reason: string; kind?: string }) {
    const { title = "Nu pot afișa UI", reason, kind } = props;

    return (
        <Card>
            <CardHeader>
                <div className="flex flex-wrap items-center gap-2">
                    <CardTitle>{title}</CardTitle>
                    {kind ? <Badge variant="outline">{kind}</Badge> : null}
                </div>
                <CardDescription>Fallback UI (safe). Verifică schema sau plugin-ul.</CardDescription>
            </CardHeader>
            <CardContent>
                <div className="text-sm text-zinc-700 dark:text-zinc-300">
                    <div className="font-semibold text-zinc-900 dark:text-zinc-100">Motiv</div>
                    <div className="mt-1 whitespace-pre-wrap">{reason}</div>
                </div>
            </CardContent>
        </Card>
    );
}
