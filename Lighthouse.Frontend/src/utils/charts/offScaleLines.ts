/** A percentile or SLE line the chart may draw, with the words an edge marker uses when it cannot. */
export interface ChartLine {
	readonly key: string;
	readonly value: number;
	readonly color: string;
	/** What an edge marker says for this line, e.g. "95%: 33 days". */
	readonly markerText: string;
}

export interface MarkerSpec {
	readonly key: string;
	readonly text: string;
	readonly color: string;
	/** 0 is the row nearest the plot. */
	readonly row: number;
}

/** The lines above the top of the axis; the others are drawn in place. */
export function linesAboveAxis<T extends { readonly value: number }>(
	lines: readonly T[],
	axisTop: number,
): T[] {
	return lines.filter((line) => line.value > axisTop);
}

export function markerSpecs(offScale: readonly ChartLine[]): MarkerSpec[] {
	return offScale.map((line, row) => ({
		key: line.key,
		text: line.markerText,
		color: line.color,
		row,
	}));
}
