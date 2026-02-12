import { ro } from "./ro";
import { en } from "./en";
export const locales = { ro, en };
export type LocaleKey = keyof typeof locales;
