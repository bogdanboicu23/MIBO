const INTRO_PENDING_KEY = "mibo:intro:pending";

export function queueIntroPlayback() {
    try {
        sessionStorage.setItem(INTRO_PENDING_KEY, "1");
    } catch {
        // Ignore storage failures and continue without intro persistence.
    }
}

export function hasQueuedIntroPlayback() {
    try {
        return sessionStorage.getItem(INTRO_PENDING_KEY) === "1";
    } catch {
        return false;
    }
}

export function clearQueuedIntroPlayback() {
    try {
        sessionStorage.removeItem(INTRO_PENDING_KEY);
    } catch {
        // Ignore storage failures and continue.
    }
}
