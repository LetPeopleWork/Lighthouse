/**
 * Standard terminology keys used throughout the application.
 * These keys correspond to configurable terminology entries in the database.
 */
export const TERMINOLOGY_KEYS = {
	WORK_ITEM: "workItem",
	WORK_ITEMS: "workItems",
} as const;

export type TerminologyKey =
	(typeof TERMINOLOGY_KEYS)[keyof typeof TERMINOLOGY_KEYS];
