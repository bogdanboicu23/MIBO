import { useEffect, useMemo, useState } from "react";
import { Icon } from "@iconify/react";
import type { UiComponentProps } from "@/components/sandbox/registry/types";
import { extractObject, resolveFromDataKey } from "@/components/sandbox/registry/dataResolver";

type EmailDraft = {
    to: string;
    cc: string;
    bcc: string;
    subject: string;
    body: string;
};

function toStringList(value: unknown): string {
    if (Array.isArray(value)) {
        return value
            .map((item) => String(item ?? "").trim())
            .filter(Boolean)
            .join(", ");
    }

    return String(value ?? "").trim();
}

function normalizeDraft(source: Record<string, any>): EmailDraft {
    return {
        to: toStringList(source.to ?? source.recipient ?? source.recipients),
        cc: toStringList(source.cc),
        bcc: toStringList(source.bcc),
        subject: String(source.subject ?? ""),
        body: String(source.body ?? source.message ?? source.content ?? ""),
    };
}

function areDraftsEqual(a: EmailDraft, b: EmailDraft): boolean {
    return a.to === b.to
        && a.cc === b.cc
        && a.bcc === b.bcc
        && a.subject === b.subject
        && a.body === b.body;
}

function normalizeRecipients(value: string): string {
    return value
        .split(/[\n,;]+/g)
        .map((item) => item.trim())
        .filter(Boolean)
        .join(",");
}

function buildGmailComposeUrl(draft: EmailDraft): string {
    const params = new URLSearchParams({
        view: "cm",
        fs: "1",
        tf: "1",
    });

    const to = normalizeRecipients(draft.to);
    const cc = normalizeRecipients(draft.cc);
    const bcc = normalizeRecipients(draft.bcc);

    if (to) params.set("to", to);
    if (cc) params.set("cc", cc);
    if (bcc) params.set("bcc", bcc);
    if (draft.subject.trim()) params.set("su", draft.subject.trim());
    if (draft.body.trim()) params.set("body", draft.body);

    return `https://mail.google.com/mail/?${params.toString()}`;
}

export function EmailComposerCard({ props, data }: UiComponentProps) {
    const rawProps = ((props as any) ?? {}) as Record<string, any>;

    const title = String(rawProps.title ?? "Email composer");
    const subtitle = String(rawProps.subtitle ?? "Review the draft and open it directly in Gmail.");
    const sendLabel = String(rawProps.sendLabel ?? "Open Gmail");
    const resetLabel = String(rawProps.resetLabel ?? "Reset");
    const helperText = String(rawProps.helperText ?? "Separate multiple recipients with commas.");
    const openInNewTab = rawProps.openInNewTab !== false;

    const draftDataKey = String(rawProps.draftDataKey ?? rawProps.dataKey ?? "").trim();
    const draftFromData = useMemo(() => {
        if (!draftDataKey) return {};
        return extractObject(resolveFromDataKey((data ?? {}) as Record<string, any>, draftDataKey)) ?? {};
    }, [data, draftDataKey]);
    const draftFromProps = useMemo(() => extractObject(rawProps.initialValues) ?? {}, [rawProps.initialValues]);

    const initialDraft = useMemo(
        () => normalizeDraft({ ...draftFromData, ...draftFromProps, ...rawProps }),
        [draftFromData, draftFromProps, rawProps]
    );

    const [draft, setDraft] = useState<EmailDraft>(initialDraft);

    useEffect(() => {
        setDraft((current) => (areDraftsEqual(current, initialDraft) ? current : initialDraft));
    }, [initialDraft]);

    const showCc = rawProps.showCc === true || draft.cc.trim().length > 0;
    const showBcc = rawProps.showBcc === true || draft.bcc.trim().length > 0;
    const canSend = Boolean(
        draft.to.trim()
        || draft.cc.trim()
        || draft.bcc.trim()
        || draft.subject.trim()
        || draft.body.trim()
    );

    const gmailUrl = buildGmailComposeUrl(draft);

    const updateField =
        (field: keyof EmailDraft) =>
            (event: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>) => {
                const value = event.target.value;
                setDraft((current) => ({ ...current, [field]: value }));
            };

    const openGmail = () => {
        if (!canSend || typeof window === "undefined") {
            return;
        }

        window.open(gmailUrl, openInNewTab ? "_blank" : "_self", openInNewTab ? "noopener,noreferrer" : undefined);
    };

    const inputClassName = "w-full rounded-xl border border-zinc-300 bg-white px-3 py-2 text-sm outline-none focus:ring-2 focus:ring-zinc-300 dark:border-zinc-700 dark:bg-zinc-950 dark:focus:ring-zinc-700";

    return (
        <div className="rounded-2xl border border-zinc-200/70 bg-white p-4 shadow-sm dark:border-zinc-800/70 dark:bg-zinc-950">
            <div className="flex items-start justify-between gap-4">
                <div>
                    <div className="text-sm font-semibold">{title}</div>
                    {subtitle ? <div className="mt-1 text-xs opacity-70">{subtitle}</div> : null}
                </div>
                <div className="inline-flex items-center gap-1.5 rounded-full border border-emerald-200 bg-emerald-50 px-3 py-1 text-[11px] font-semibold uppercase tracking-[0.16em] text-emerald-700 dark:border-emerald-900/70 dark:bg-emerald-950/40 dark:text-emerald-300">
                    <Icon icon="logos:google-gmail" className="h-4 w-4 shrink-0" />
                    Gmail
                </div>
            </div>

            <div className="mt-4 grid gap-3">
                <label className="grid gap-1">
                    <span className="text-xs font-medium opacity-80">To</span>
                    <input
                        className={inputClassName}
                        value={draft.to}
                        placeholder={String(rawProps.toPlaceholder ?? "name@example.com")}
                        onChange={updateField("to")}
                    />
                </label>

                {showCc ? (
                    <label className="grid gap-1">
                        <span className="text-xs font-medium opacity-80">Cc</span>
                        <input
                            className={inputClassName}
                            value={draft.cc}
                            placeholder={String(rawProps.ccPlaceholder ?? "team@example.com")}
                            onChange={updateField("cc")}
                        />
                    </label>
                ) : null}

                {showBcc ? (
                    <label className="grid gap-1">
                        <span className="text-xs font-medium opacity-80">Bcc</span>
                        <input
                            className={inputClassName}
                            value={draft.bcc}
                            placeholder={String(rawProps.bccPlaceholder ?? "stakeholders@example.com")}
                            onChange={updateField("bcc")}
                        />
                    </label>
                ) : null}

                <label className="grid gap-1">
                    <span className="text-xs font-medium opacity-80">Subject</span>
                    <input
                        className={inputClassName}
                        value={draft.subject}
                        placeholder={String(rawProps.subjectPlaceholder ?? "Email subject")}
                        onChange={updateField("subject")}
                    />
                </label>

                <label className="grid gap-1">
                    <span className="text-xs font-medium opacity-80">Body</span>
                    <textarea
                        className={`${inputClassName} min-h-40`}
                        value={draft.body}
                        placeholder={String(rawProps.bodyPlaceholder ?? "Write or refine the generated email draft...")}
                        onChange={updateField("body")}
                    />
                </label>
            </div>

            <div className="mt-3 rounded-xl border border-zinc-200/80 bg-zinc-50 px-3 py-2 text-xs text-zinc-600 dark:border-zinc-800/80 dark:bg-zinc-900 dark:text-zinc-400">
                {helperText}
            </div>

            <div className="mt-4 flex flex-wrap gap-2">
                <button
                    type="button"
                    onClick={openGmail}
                    disabled={!canSend}
                    className="inline-flex items-center gap-2 rounded-xl border border-zinc-900 bg-zinc-900 px-4 py-2 text-sm font-medium text-white transition hover:bg-zinc-800 disabled:cursor-not-allowed disabled:opacity-50 dark:border-white dark:bg-white dark:text-zinc-900 dark:hover:bg-zinc-100"
                >
                    <Icon icon="logos:google-gmail" className="h-4 w-4 shrink-0" />
                    {sendLabel}
                </button>
                <button
                    type="button"
                    onClick={() => setDraft(initialDraft)}
                    className="rounded-xl border border-zinc-300 bg-white px-4 py-2 text-sm font-medium hover:bg-zinc-50 dark:border-zinc-700 dark:bg-zinc-950 dark:hover:bg-zinc-900"
                >
                    {resetLabel}
                </button>
            </div>
        </div>
    );
}
