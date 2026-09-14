const SECOND = 1000;
const MINUTE = 60 * SECOND;
const HOUR = 60 * MINUTE;
const DAY = 24 * HOUR;

/**
 * How long something has been going, for a reader glancing at a list rather than reading a report.
 *
 * Not `formatDuration` in this same folder: that one takes a number of days and a chart's chosen axis
 * unit, and exists so every point on one chart is expressed the same way. This one takes milliseconds
 * and picks its own unit per value, because the rows it renders are independent of each other - a
 * refresh going for four seconds and one going for two days sit side by side and each wants its own
 * unit. Same shape, different knowledge.
 */
export function formatElapsed(elapsedMs: number): string {
	// The server already clamps a moment stamped by a replica whose clock runs ahead, so a negative
	// value should not arrive. Rendering one as "-3s" would turn somebody else's bug into a row that
	// reads as broken rather than as new.
	const elapsed = Math.max(0, elapsedMs);

	if (elapsed < MINUTE) {
		return `${Math.floor(elapsed / SECOND)}s`;
	}

	if (elapsed < HOUR) {
		return `${Math.floor(elapsed / MINUTE)}m`;
	}

	// Two units past an hour, because the smaller one is the part still visibly moving: "1h" would sit
	// unchanged for an hour on the screen of somebody deciding whether to keep waiting.
	if (elapsed < DAY) {
		return `${Math.floor(elapsed / HOUR)}h ${Math.floor((elapsed % HOUR) / MINUTE)}m`;
	}

	return `${Math.floor(elapsed / DAY)}d ${Math.floor((elapsed % DAY) / HOUR)}h`;
}
