import { useCallback, useState } from "react";

export interface LegendFilter {
	selected: ReadonlySet<string>;
	isVisible: (id: string) => boolean;
	toggle: (id: string) => void;
	showAll: () => void;
}

// An empty selection means "show everything" (AC-4.3): the forecaster has not filtered yet, so
// unpicking the last entry has to land back on the unfiltered chart rather than on an empty one.
export const isShown = (selected: ReadonlySet<string>, id: string): boolean =>
	selected.size === 0 || selected.has(id);

// AC-4.6: one instance per chart, so two expanded deliveries filter independently.
export const useLegendFilter = (): LegendFilter => {
	const [selected, setSelected] = useState<ReadonlySet<string>>(new Set());

	const toggle = useCallback((id: string) => {
		setSelected((previous) => {
			const next = new Set(previous);
			if (next.has(id)) {
				next.delete(id);
			} else {
				next.add(id);
			}
			return next;
		});
	}, []);

	const showAll = useCallback(() => setSelected(new Set()), []);

	const isVisible = useCallback(
		(id: string) => isShown(selected, id),
		[selected],
	);

	return { selected, isVisible, toggle, showAll };
};
