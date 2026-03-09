import { useMemo, useRef, useState, useCallback, useEffect } from "react";
import type { Conversation, Message } from "../types/chat";
import { endpoints } from "@/axios/endpoints.ts";
import { useAxios } from "@/axios/hooks";
import { uidStr } from "@/_mock/conversations.ts";

const DEMO_USER_ID = "u-demo-001";

type ServerConversationSummary = {
    conversationId: string;
    userId: string;
    title: string;
    createdAt: string;
    updatedAt: string;
    lastMessageAt?: string | null;
    lastMessagePreview?: string | null;
    messageCount: number;
};

type ServerConversationMessage = {
    messageId: string;
    conversationId: string;
    userId: string;
    role: "user" | "assistant" | "system";
    text: string;
    uiV1: any | null;
    correlationId: string;
    createdAt: string;
};

type ServerConversationDetails = {
    conversation: ServerConversationSummary;
    messages: ServerConversationMessage[];
};

const COMPONENT_NAME_ALIASES: Record<string, string> = {
    piechart: "pieChart",
    pie: "pieChart",
    barchart: "barChart",
    bar: "barChart",
    linechart: "lineChart",
    line: "lineChart",
    datatable: "dataTable",
    table: "dataTable",
    productcarousel: "productCarousel",
    productdetail: "productDetail",
    kpi: "kpiCard",
    kpicard: "kpiCard",
    pagetitle: "pageTitle",
    search: "searchBar",
    searchinput: "searchBar",
    sort: "sortDropdown",
    categories: "categoryChips",
    categorychips: "categoryChips",
    summary: "summaryPanel",
    actionpanel: "actionPanel",
    actions: "actionPanel",
    form: "formCard",
    formcard: "formCard",
    timeline: "timelineCard",
    timelinecard: "timelineCard",
    json: "jsonViewer",
    jsonviewer: "jsonViewer",
    markdown: "markdown",
    pagination: "pagination",
    cartsummary: "cartSummary",
    carousel: "carousel",
};

const KNOWN_COMPONENT_NAMES = new Set<string>([
    "pieChart",
    "barChart",
    "lineChart",
    "dataTable",
    "productCarousel",
    "productDetail",
    "kpiCard",
    "pageTitle",
    "searchBar",
    "sortDropdown",
    "categoryChips",
    "pagination",
    "cartSummary",
    "carousel",
    "summaryPanel",
    "markdown",
    "actionPanel",
    "formCard",
    "timelineCard",
    "jsonViewer",
]);

const PLACEHOLDER_COMPONENT_NAMES = new Set<string>([
    "type",
    "props",
    "component",
    "components",
    "children",
    "items",
    "root",
]);

function normalizeComponentName(raw: unknown): string {
    const name = String(raw ?? "").trim();
    if (!name) return "";
    const compact = name.replace(/[^a-zA-Z0-9]/g, "").toLowerCase();
    return COMPONENT_NAME_ALIASES[compact] ?? name;
}

function inferComponentNameFromProps(props: Record<string, unknown>): string | null {
    const p = props ?? {};
    const explicit = normalizeComponentName((p as any).component ?? (p as any).name ?? (p as any).kind);
    if (explicit && KNOWN_COMPONENT_NAMES.has(explicit)) return explicit;

    if (Array.isArray((p as any).columns) || Array.isArray((p as any).rows) || (p as any).rowKey || (p as any).rowsKey) {
        return "dataTable";
    }

    if ((p as any).total !== undefined && (p as any).limit !== undefined && (p as any).skip !== undefined) {
        return "pagination";
    }

    if ((p as any).placeholder !== undefined && ((p as any).actionType || (p as any).payloadTemplate || (p as any).submitOnEmpty !== undefined)) {
        return "searchBar";
    }

    if (Array.isArray((p as any).options) && ((p as any).actionType || (p as any).payloadTemplate || (p as any).selected !== undefined)) {
        return "sortDropdown";
    }

    if (Array.isArray((p as any).data)) {
        const first = ((p as any).data as any[]).find((x) => x && typeof x === "object") as Record<string, unknown> | undefined;
        if (first) {
            const hasPieShape = "value" in first && ("name" in first || "label" in first || "category" in first);
            if (hasPieShape || (p as any).showLegend !== undefined || (p as any).formatValue !== undefined) return "pieChart";
            const hasLineOrBarShape =
                ("x" in first || "label" in first || "name" in first) &&
                ("y" in first || "value" in first);
            if (hasLineOrBarShape) return "barChart";
        }
    }

    if ((p as any).product || (p as any).productId || (p as any).thumbnail || (p as any).images) return "productDetail";
    if (Array.isArray((p as any).products) || (p as any).itemComponent) return "productCarousel";
    if (Array.isArray((p as any).actions) && !Array.isArray((p as any).columns)) return "actionPanel";
    if (Array.isArray((p as any).events) || Array.isArray((p as any).timeline)) return "timelineCard";
    if ((p as any).value !== undefined && ((p as any).label || (p as any).title || (p as any).unit)) return "kpiCard";
    if ((p as any).markdown || ((p as any).content && typeof (p as any).content === "string")) return "markdown";
    if ((p as any).text !== undefined || (p as any).subtitle !== undefined) return "pageTitle";

    return null;
}

function normalizeBackendUi(ui: any): any {
    if (!ui || typeof ui !== "object") return null;
    if (ui.schema !== "ui.v1") return ui;

    const root = normalizeUiNode(ui.root);
    if (!root) return null;

    const bindings = normalizeBindings(ui.bindings, root);
    const subscriptions = Array.isArray(ui.subscriptions) ? ui.subscriptions : [];
    const data = ui.data && typeof ui.data === "object" ? ui.data : {};

    return {
        schema: "ui.v1",
        root,
        data,
        bindings,
        subscriptions,
    };
}

function looksLikeMarkdownUiCandidate(text: string): boolean {
    const t = (text ?? "").trim();
    if (!t) return false;
    if (t.includes("```")) return true;
    if (/\n\s*[-*]\s+/.test(t)) return true;
    if (/\n\s*\d+\.\s+/.test(t)) return true;
    return false;
}

function buildMarkdownUiFromText(text: string): any {
    return {
        schema: "ui.v1",
        root: {
            type: "layout",
            name: "column",
            props: { gap: 12 },
            children: [
                {
                    type: "component",
                    name: "markdown",
                    props: {
                        title: "Code / Markdown",
                        content: text,
                        showCodeHeader: true,
                    },
                    children: [],
                },
            ],
        },
        data: {},
        bindings: [],
        subscriptions: [],
    };
}

function resolveAssistantUi(rawUi: any, text: string): any {
    const normalized = normalizeBackendUi(rawUi);
    if (normalized) return normalized;
    if (looksLikeMarkdownUiCandidate(text)) return buildMarkdownUiFromText(text);
    return null;
}

function normalizeUiNode(node: any): any | null {
    if (!node) return null;

    if (node.root && Object.keys(node).length === 1) {
        return normalizeUiNode(node.root);
    }

    // Legacy layout shorthand:
    // { type: "column"|"row"|"grid", components: [...] }
    if (typeof node.type === "string" && ["column", "row", "grid"].includes(node.type)) {
        const childrenRaw = Array.isArray(node.components)
            ? node.components
            : Array.isArray(node.children)
                ? node.children
                : [];
        const children = childrenRaw.map((c: any) => normalizeUiNode(c)).filter(Boolean);
        return {
            type: "layout",
            name: node.type,
            props: node.props && typeof node.props === "object" ? node.props : {},
            children,
        };
    }

    // Legacy component shorthand:
    // { type: "dataTable", props: {...} } or { component: "dataTable", ... }
    if (typeof node.type === "string" && !["layout", "component"].includes(node.type)) {
        let props = node.props && typeof node.props === "object" ? node.props : {};
        // tolerate malformed payloads like props: { props: { ... } }
        if (
            props &&
            typeof props === "object" &&
            "props" in (props as any) &&
            typeof (props as any).props === "object" &&
            Object.keys(props as any).length === 1
        ) {
            props = (props as any).props;
        }
        const normalizedName = normalizeComponentName(node.type);
        const inferred = inferComponentNameFromProps(props);
        const name =
            KNOWN_COMPONENT_NAMES.has(normalizedName)
                ? normalizedName
                : (inferred ?? normalizedName);

        if (!name || (PLACEHOLDER_COMPONENT_NAMES.has(String(name).toLowerCase()) && !inferred)) {
            return null;
        }
        const childrenRaw = Array.isArray(node.children)
            ? node.children
            : Array.isArray(node.components)
                ? node.components
                : [];
        const children = childrenRaw.map((c: any) => normalizeUiNode(c)).filter(Boolean);
        return {
            type: "component",
            name,
            props,
            children,
        };
    }

    if (node.type === "layout" || node.type === "component") {
        const inferredName =
            node.name ??
            (node.type === "component" ? node.component : undefined) ??
            (node.type === "layout" ? "column" : undefined);
        const rawName = String(inferredName ?? "").trim();
        if (!rawName) return null;
        const childrenRaw = Array.isArray(node.children)
            ? node.children
            : Array.isArray(node.components)
                ? node.components
                : [];
        const children = childrenRaw.map((c: any) => normalizeUiNode(c)).filter(Boolean);
        let props = node.props && typeof node.props === "object" ? node.props : {};
        if (
            props &&
            typeof props === "object" &&
            "props" in (props as any) &&
            typeof (props as any).props === "object" &&
            Object.keys(props as any).length === 1
        ) {
            props = (props as any).props;
        }

        if (node.type === "layout") {
            const normalizedLayout = normalizeComponentName(rawName);
            const name = ["column", "row", "grid"].includes(normalizedLayout) ? normalizedLayout : "column";
            return {
                type: "layout",
                name,
                props,
                children,
            };
        }

        const normalizedName = normalizeComponentName(rawName);
        const inferred = inferComponentNameFromProps(props);
        const finalName =
            KNOWN_COMPONENT_NAMES.has(normalizedName)
                ? normalizedName
                : (inferred ?? normalizedName);
        const isPlaceholder = PLACEHOLDER_COMPONENT_NAMES.has(normalizedName.toLowerCase());
        if (isPlaceholder && !inferred) {
            if (Object.keys(props).length === 0 && children.length === 0) return null;
            return null;
        }

        return {
            type: node.type,
            name: finalName,
            props,
            children,
        };
    }

    if (typeof node.component === "string") {
        const props = { ...node };
        delete props.component;
        delete props.children;
        delete props.root;
        const normalizedName = normalizeComponentName(node.component);
        const inferred = inferComponentNameFromProps(props);
        const name =
            KNOWN_COMPONENT_NAMES.has(normalizedName)
                ? normalizedName
                : (inferred ?? normalizedName);
        if (!name) return null;
        return {
            type: "component",
            name,
            props,
            children: [],
        };
    }

    if (typeof node === "object") {
        const children = Object.entries(node)
            .filter(([k]) => !["root", "bindings", "subscriptions"].includes(k))
            .map(([k, v]) => {
                const props = v && typeof v === "object" ? ((v as any).props ?? v) : {};
                const normalizedName = normalizeComponentName(k);
                const inferred = inferComponentNameFromProps(props);
                const name =
                    KNOWN_COMPONENT_NAMES.has(normalizedName)
                        ? normalizedName
                        : (inferred ?? normalizedName);
                if (!name || (PLACEHOLDER_COMPONENT_NAMES.has(normalizedName.toLowerCase()) && !inferred)) {
                    return null;
                }
                return {
                    type: "component",
                    name,
                    props,
                    children: [],
                };
            })
            .filter(Boolean);

        if (children.length > 0) {
            return {
                type: "layout",
                name: "column",
                props: { gap: 12 },
                children,
            };
        }
    }

    return null;
}

function normalizeBindings(bindings: any, root: any): any[] {
    if (!Array.isArray(bindings)) return [];

    const normalizeFrom = (raw: unknown): string => {
        const s = String(raw ?? "").trim();
        const m = /^\$\{(?:tool|step):(.+)\}$/.exec(s);
        return m?.[1]?.trim() || s;
    };

    const normalizePath = (raw: unknown): string => {
        const p = String(raw ?? "").trim();
        if (!p) return "/root";
        if (p.startsWith("/")) return p;
        return findComponentPath(root, p) ?? "/root";
    };

    return bindings
        .map((b) => {
            if (b && typeof b === "object" && b.componentPath && b.prop && b.from) {
                return {
                    componentPath: normalizePath(b.componentPath),
                    prop: String(b.prop),
                    from: normalizeFrom(b.from),
                };
            }

            if (b && typeof b === "object" && b.component && b.prop && b.tool) {
                const comp = String(b.component);
                const arg = b.arg ? String(b.arg) : "";
                const from = normalizeFrom(arg ? `${String(b.tool)}.${arg}` : String(b.tool));
                const componentPath = findComponentPath(root, comp) ?? "/root";
                return {
                    componentPath,
                    prop: String(b.prop),
                    from: comp === "productDetail" && arg === "products" ? `${String(b.tool)}.products.0` : from,
                };
            }

            return null;
        })
        .filter(Boolean);
}

function findComponentPath(node: any, name: string, path = "/root"): string | null {
    if (!node || typeof node !== "object") return null;
    if (node.type === "component" && String(node.name) === name) return path;

    if (!Array.isArray(node.children)) return null;
    for (let i = 0; i < node.children.length; i++) {
        const found = findComponentPath(node.children[i], name, `${path}/children/${i}`);
        if (found) return found;
    }
    return null;
}

export function useChat() {
    const { api } = useAxios();

    const [conversations, setConversations] = useState<Conversation[]>([]);
    const [activeId, setActiveId] = useState("");
    const [model, setModel] = useState<"AI Agent" | "AI Agent Pro">("AI Agent");
    const [isTyping, setIsTyping] = useState(false);

    const active = useMemo(
        () => conversations.find((c) => c.id === activeId),
        [conversations, activeId]
    );

    const listRef = useRef<HTMLDivElement>(null as any);
    const abortRef = useRef<AbortController | null>(null);

    const updateConversation = useCallback(
        (convId: string, updater: (c: Conversation) => Conversation) => {
            setConversations((prev) => prev.map((c) => (c.id === convId ? updater(c) : c)));
        },
        []
    );

    const mapSummaryToConversation = useCallback((item: ServerConversationSummary): Conversation => {
        return {
            id: item.conversationId,
            title: item.title || "New chat",
            updatedAt: Date.parse(item.updatedAt) || Date.now(),
            messages: [],
            uiV1: null,
            correlationId: null,
            loaded: false,
            lastMessagePreview: item.lastMessagePreview ?? null,
        };
    }, []);

    const loadConversationList = useCallback(async () => {
        const data = await api.get<{ items: ServerConversationSummary[] }>(endpoints.conversations.list, {
            params: { userId: DEMO_USER_ID, skip: 0, limit: 100 },
        });

        const mapped = (data.items ?? []).map(mapSummaryToConversation);
        setConversations(mapped);

        if (mapped.length > 0) {
            setActiveId((prev) => prev || mapped[0].id);
        }
    }, [api, mapSummaryToConversation]);

    const loadConversationDetails = useCallback(
        async (conversationId: string) => {
            const data = await api.get<ServerConversationDetails>(endpoints.conversations.detail(conversationId), {
                params: { userId: DEMO_USER_ID, messagesLimit: 300 },
            });

            const messages: Message[] = (data.messages ?? []).map((m) => ({
                id: m.messageId,
                role: m.role,
                content: m.text,
                uiV1: m.role === "assistant" ? resolveAssistantUi(m.uiV1, m.text ?? "") : null,
                createdAt: Date.parse(m.createdAt) || Date.now(),
            }));

            const latestUi = [...messages].reverse().find((m) => m.role === "assistant" && m.uiV1)?.uiV1 ?? null;

            updateConversation(conversationId, (c) => ({
                ...c,
                title: data.conversation?.title || c.title,
                updatedAt: Date.parse(data.conversation?.updatedAt ?? "") || c.updatedAt,
                messages,
                uiV1: latestUi,
                loaded: true,
                lastMessagePreview: data.conversation?.lastMessagePreview ?? c.lastMessagePreview,
            }));
        },
        [api, updateConversation]
    );

    useEffect(() => {
        void loadConversationList();
    }, [loadConversationList]);

    useEffect(() => {
        if (!activeId) return;
        const conv = conversations.find((c) => c.id === activeId);
        if (!conv || conv.loaded) return;
        void loadConversationDetails(activeId);
    }, [activeId, conversations, loadConversationDetails]);

    async function newChat() {
        const created = await api.post<ServerConversationSummary>(endpoints.conversations.list, {
            title: "New chat",
        }, {
            params: { userId: DEMO_USER_ID },
        });

        const c = mapSummaryToConversation(created);
        c.loaded = true;

        setConversations((prev) => [c, ...prev]);
        setActiveId(c.id);
        return c;
    }

    function selectConversation(id: string) {
        setActiveId(id);
    }

    async function renameConversation(id: string, title: string) {
        const nextTitle = title.trim() || "Untitled";
        updateConversation(id, (c) => ({ ...c, title: nextTitle }));

        try {
            await api.patch(endpoints.conversations.detail(id), { title: nextTitle }, {
                params: { userId: DEMO_USER_ID },
            });
        } catch {
            await loadConversationList();
        }
    }

    async function deleteConversation(id: string) {
        const prev = conversations;
        setConversations((p) => p.filter((c) => c.id !== id));

        if (id === activeId) {
            const next = conversations.find((c) => c.id !== id)?.id;
            setActiveId(next ?? "");
        }

        try {
            await api.delete(endpoints.conversations.detail(id), {
                params: { userId: DEMO_USER_ID },
            });
        } catch {
            setConversations(prev);
        }
    }

    function stop() {
        abortRef.current?.abort();
        abortRef.current = null;
        setIsTyping(false);
    }

    /**
     * Send user prompt -> /v1/chat/stream (SSE)
     * Streams text tokens in real-time; UI payload arrives as a named "ui" event.
     */
    async function send(text: string) {
        if (!text.trim() || isTyping) return;

        let currentActive = active;
        if (!currentActive) {
            currentActive = await newChat();
        }
        if (!currentActive) return;

        abortRef.current?.abort();

        const now = Date.now();
        const trimmed = text.trim();

        const userMsg: Message = { id: uidStr(), role: "user", content: trimmed, createdAt: now };

        const assistantId = uidStr();
        const assistantMsg: Message = {
            id: assistantId,
            role: "assistant",
            content: "",
            createdAt: now,
            uiV1: null,
        };

        const convId = currentActive.id;

        updateConversation(convId, (c) => ({
            ...c,
            messages: [...c.messages, userMsg, assistantMsg],
            updatedAt: now,
            loaded: true,
            title:
                c.title === "New chat"
                    ? trimmed.slice(0, 60) + (trimmed.length > 60 ? "…" : "")
                    : c.title,
            lastMessagePreview: trimmed,
        }));

        setIsTyping(true);
        const ac = new AbortController();
        abortRef.current = ac;

        const payload = {
            conversationId: convId,
            userId: DEMO_USER_ID,
            prompt: trimmed,
            clientContext: {
                timezone: Intl.DateTimeFormat().resolvedOptions().timeZone,
                locale: navigator.language,
            },
        };

        try {
            await api.postStream(endpoints.conversations.stream, payload, {
                signal: ac.signal,

                onToken: (token: string) => {
                    updateConversation(convId, (c) => ({
                        ...c,
                        messages: c.messages.map((m) =>
                            m.id === assistantId
                                ? { ...m, content: (m.content ?? "") + token }
                                : m
                        ),
                        updatedAt: Date.now(),
                    }));
                },

                onEvent: (eventType: string, data: any) => {
                    if (eventType === "ui") {
                        const normalized = resolveAssistantUi(data, "");
                        updateConversation(convId, (c) => ({
                            ...c,
                            messages: c.messages.map((m) =>
                                m.id === assistantId ? { ...m, uiV1: normalized } : m
                            ),
                            uiV1: normalized,
                            updatedAt: Date.now(),
                        }));
                    } else if (eventType === "done") {
                        updateConversation(convId, (c) => ({
                            ...c,
                            correlationId: data?.correlationId ?? null,
                        }));
                    }
                },

                onDone: () => {
                    setIsTyping(false);
                    abortRef.current = null;
                },

                onError: () => {
                    setIsTyping(false);
                    abortRef.current = null;
                },
            });
        } catch (e: any) {
            if (e?.name === "CanceledError" || e?.name === "AbortError") return;

            updateConversation(convId, (c) => ({
                ...c,
                messages: c.messages.map((m) =>
                    m.id === assistantId && (m.content ?? "").trim() === ""
                        ? { ...m, content: "Eroare la răspunsul AI / conexiune. Încearcă din nou." }
                        : m
                ),
                updatedAt: Date.now(),
            }));
        } finally {
            setIsTyping(false);
            abortRef.current = null;
        }
    }

    async function sendAction(type: string, payload: Record<string, unknown>, uiContext?: any) {
        if (!active) return;
        const payloadUi = (payload as any)?.__ui ?? null;
        const focusedComponentId =
            typeof payloadUi?.component === "string" ? payloadUi.component :
            typeof payloadUi?.path === "string" ? payloadUi.path :
            null;

        const req = {
            schema: "action.v1",
            conversationId: active.id,
            userId: DEMO_USER_ID,
            action: { type, payload },
            uiContext: uiContext ?? { focusedComponentId },
        };

        const data = await api.post<{
            schema: string;
            text?: string | null;
            uiPatch?: any | null;
            uiV1?: any | null;
            correlationId?: string;
            toolStates?: Record<string, unknown> | null;
            warnings?: string[] | null;
        }>(
            endpoints.conversations.action,
            req
        );

        const hasPatch = Boolean(data.uiPatch);

        if (hasPatch) {
            applyPatchToConversation(active.id, data.uiPatch);
        }

        if (data.toolStates && hasPatch) {
            applyPatchToConversation(active.id, {
                schema: "ui.patch.v1",
                ops: [{ op: "set", path: "/toolStates", value: data.toolStates }],
            });
        }

        // UI actions should update current card in-place by default.
        // If backend returns text/UI without patch, append assistant message for backward compatibility.
        if (!hasPatch && (data.text || data.uiV1)) {
            const now = Date.now();
            const msg: Message = {
                id: uidStr(),
                role: "assistant",
                content: data.text ?? "",
                uiV1: resolveAssistantUi(data.uiV1, data.text ?? ""),
                createdAt: now,
            };
            updateConversation(active.id, (c) => ({
                ...c,
                messages: [...c.messages, msg],
                updatedAt: now,
                uiV1: resolveAssistantUi(data.uiV1, data.text ?? "") ?? c.uiV1,
            }));
        }

        if (hasPatch && (data.text || (data.warnings && data.warnings.length))) {
            const statusParts = [
                data.text ?? "",
                ...(Array.isArray(data.warnings) ? data.warnings.map((w) => `[warn:${w}]`) : []),
            ].filter(Boolean);
            if (statusParts.length > 0) {
                updateConversation(active.id, (c) => {
                    const updatedMessages = [...c.messages];
                    for (let i = updatedMessages.length - 1; i >= 0; i--) {
                        const m = updatedMessages[i];
                        if (m.role !== "assistant") continue;
                        if (!m.uiV1) continue;
                        updatedMessages[i] = {
                            ...m,
                            content: [m.content ?? "", statusParts.join(" ")].filter(Boolean).join("\n"),
                        };
                        return { ...c, messages: updatedMessages, updatedAt: Date.now() };
                    }

                    const fallbackMsg: Message = {
                        id: uidStr(),
                        role: "assistant",
                        content: statusParts.join(" "),
                        createdAt: Date.now(),
                    };
                    return { ...c, messages: [...c.messages, fallbackMsg], updatedAt: Date.now() };
                });
            }
        }
    }

    function applyPatchFromRealtime(patch: any) {
        if (!active) return;
        applyPatchToConversation(active.id, patch);
    }

    function applyPatchToConversation(conversationId: string, patch: any) {
        updateConversation(conversationId, (c) => {
            const baseUi = c.uiV1 ?? createUiFromPatchSeed(patch);
            if (!baseUi) return c;

            const nextUi = applyUiPatch(baseUi, patch);
            const updatedMessages = [...c.messages];
            for (let i = updatedMessages.length - 1; i >= 0; i--) {
                if (updatedMessages[i].role === "assistant" && updatedMessages[i].uiV1) {
                    updatedMessages[i] = { ...updatedMessages[i], uiV1: nextUi };
                    break;
                }
            }
            return { ...c, uiV1: nextUi, messages: updatedMessages, updatedAt: Date.now() };
        });
    }

    function createUiFromPatchSeed(patch: any) {
        const seed: any = {
            schema: "ui.v1",
            root: null,
            data: {},
            bindings: [],
            subscriptions: [],
        };

        for (const op of patch?.ops ?? []) {
            if (op?.path === "/root" && (op.op === "set" || op.op === "replace")) seed.root = op.value;
            if (op?.path === "/data" && (op.op === "set" || op.op === "replace")) seed.data = op.value ?? {};
            if (op?.path === "/bindings" && (op.op === "set" || op.op === "replace")) seed.bindings = op.value ?? [];
            if (op?.path === "/subscriptions" && (op.op === "set" || op.op === "replace")) seed.subscriptions = op.value ?? [];
        }

        return seed.root ? seed : null;
    }

    function applyUiPatch(ui: any, patch: any) {
        const next = structuredClone(ui);

        for (const op of patch?.ops ?? []) {
            const path = String(op.path ?? "");
            const parts = path.split("/").filter(Boolean);
            if (parts.length === 0) continue;

            let cur: any = next;
            for (let i = 0; i < parts.length - 1; i++) {
                const p = parts[i];
                cur[p] ??= {};
                cur = cur[p];
                if (cur == null) break;
            }
            if (cur == null) continue;

            const key = parts[parts.length - 1];

            if (op.op === "set" || op.op === "replace") cur[key] = op.value;
            else if (op.op === "merge") cur[key] = { ...(cur[key] ?? {}), ...(op.value ?? {}) };
            else if (op.op === "remove") delete cur[key];
        }

        return next;
    }

    return {
        conversations,
        activeId,
        active,
        model,
        setModel,
        isTyping,
        listRef,

        newChat,
        selectConversation,
        renameConversation,
        deleteConversation,

        send,
        stop,

        sendAction,
        applyPatchFromRealtime,
    };
}
