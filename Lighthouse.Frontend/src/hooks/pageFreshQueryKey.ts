import type { QueryKey } from "@tanstack/react-query";

/** Every shared page read lives under this key, so a page move can ask them all again at once. */
export const PAGE_FRESH_READS = ["page-fresh"] as const;

export const pageFreshQueryKey = (key: QueryKey): QueryKey => [
	...PAGE_FRESH_READS,
	...key,
];
