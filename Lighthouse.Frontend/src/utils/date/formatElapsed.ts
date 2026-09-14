export const __SCAFFOLD__ = true;

/**
 * How long something has been going, for a reader glancing at a list rather than reading a report.
 *
 * Not `formatDuration` in this same folder: that one takes a number of days and a chart's chosen axis
 * unit, and exists so every point on one chart is expressed the same way. This one takes milliseconds
 * and picks its own unit per value, because the rows it renders are independent of each other - a
 * refresh going for four seconds and one going for two days sit side by side and each wants its own
 * unit. Same shape, different knowledge.
 */
export function formatElapsed(_elapsedMs: number): string {
	throw new Error("Not yet implemented — RED scaffold");
}
