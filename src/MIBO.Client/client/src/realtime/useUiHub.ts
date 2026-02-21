import { useEffect, useMemo, useRef, useState } from "react";
import * as signalR from "@microsoft/signalr";

export type UiPatchV1 = any;

export function useUiHub(params: {
    conversationId: string;
    onPatch: (patch: UiPatchV1) => void;
}) {
    const { conversationId, onPatch } = params;

    const [connected, setConnected] = useState(false);
    const connRef = useRef<signalR.HubConnection | null>(null);

    // păstrăm handler-ul stabil, fără să reinițializăm conexiunea
    const onPatchRef = useRef(onPatch);

    useEffect(() => {
        onPatchRef.current = onPatch;
    }, [onPatch]);

    const hubUrl = useMemo(() => `https://localhost:7286/hubs/ui`, []);

    useEffect(() => {
        let cancelled = false;

        const conn = new signalR.HubConnectionBuilder()
            .withUrl(hubUrl, {
                transport: signalR.HttpTransportType.WebSockets,
                // skipNegotiation: true,
                // accessTokenFactory: () => localStorage.getItem("token") ?? ""
            })
            .withAutomaticReconnect()
            .build();

        connRef.current = conn;

        conn.on("UiPatch", (patch: UiPatchV1) => {
            onPatchRef.current(patch);
        });

        const start = async () => {
            try {
                await conn.start();

                if (cancelled) {
                    await conn.stop();
                    return;
                }

                setConnected(true);

                // Join doar dacă e încă activ effect-ul
                await conn.invoke("JoinConversation", conversationId);
            } catch (err) {
                if (!cancelled) {
                    console.error("SignalR connect error:", err);
                    setConnected(false);
                }
            }
        };

        start();

        return () => {
            cancelled = true;
            setConnected(false);
            void conn.stop();
        };
    }, [conversationId, hubUrl]);

    return { connected };
}