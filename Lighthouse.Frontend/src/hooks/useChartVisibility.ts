import { useCallback, useEffect, useRef, useState } from "react";
import type { IPercentileValue } from "../models/PercentileValue";

export function usePercentileVisibility(percentiles: IPercentileValue[]) {
	const percentilesRef = useRef<string>("");
	const [visiblePercentiles, setVisiblePercentiles] = useState<
		Record<number, boolean>
	>(() => {
		percentilesRef.current = JSON.stringify(
			percentiles.map((p) => p.percentile).sort((a, b) => a - b),
		);
		const initialVisibility: Record<number, boolean> = {};
		for (const p of percentiles) {
			initialVisibility[p.percentile] = true;
		}
		return initialVisibility;
	});

	useEffect(() => {
		const percentilesKey = JSON.stringify(
			percentiles.map((p) => p.percentile).sort((a, b) => a - b),
		);
		if (percentilesRef.current === percentilesKey) return;
		percentilesRef.current = percentilesKey;

		const initialVisibility: Record<number, boolean> = {};
		for (const p of percentiles) {
			initialVisibility[p.percentile] = true;
		}
		setVisiblePercentiles(initialVisibility);
	}, [percentiles]);

	const togglePercentileVisibility = useCallback((percentile: number) => {
		setVisiblePercentiles((prev) => ({
			...prev,
			[percentile]: !prev[percentile],
		}));
	}, []);

	return {
		visiblePercentiles,
		togglePercentileVisibility,
	};
}

export function useServiceLevelExpectationVisibility() {
	const [sleVisible, setSleVisible] = useState<boolean>(false);

	const toggleSleVisibility = useCallback(() => {
		setSleVisible((prev) => !prev);
	}, []);

	return {
		sleVisible,
		toggleSleVisibility,
	};
}

export function useTypesVisibility<T extends string>(
	types: T[],
	initialVisibility?: Record<T, boolean>,
) {
	const typesRef = useRef<string>("");
	const [visibleTypes, setVisibleTypes] = useState<Record<T, boolean>>(() => {
		typesRef.current = JSON.stringify(
			[...types].sort((a, b) => a.localeCompare(b)),
		);
		if (initialVisibility) {
			return initialVisibility;
		}
		return Object.fromEntries(types.map((type) => [type, true])) as Record<
			T,
			boolean
		>;
	});

	useEffect(() => {
		const typesKey = JSON.stringify(
			[...types].sort((a, b) => a.localeCompare(b)),
		);
		if (typesRef.current === typesKey) return;
		typesRef.current = typesKey;

		if (initialVisibility) {
			setVisibleTypes(initialVisibility);
		} else {
			setVisibleTypes(
				Object.fromEntries(types.map((type) => [type, true])) as Record<
					T,
					boolean
				>,
			);
		}
	}, [types, initialVisibility]);

	const toggleTypeVisibility = useCallback((type: T) => {
		setVisibleTypes((prev) => {
			const visibleCount = Object.values(prev).filter(
				(v) => v !== false,
			).length;

			// Prevent hiding the last visible type
			if (prev[type] !== false && visibleCount <= 1) {
				return prev;
			}

			return {
				...prev,
				[type]: !prev[type],
			};
		});
	}, []);

	return {
		visibleTypes,
		toggleTypeVisibility,
	};
}

export interface UseChartVisibilityProps<T extends string> {
	percentiles: IPercentileValue[];
	types: T[];
	initialTypeVisibility?: Record<T, boolean>;
}

export function useChartVisibility<T extends string>({
	percentiles,
	types,
	initialTypeVisibility,
}: UseChartVisibilityProps<T>) {
	const percentileVisibility = usePercentileVisibility(percentiles);
	const sleVisibility = useServiceLevelExpectationVisibility();
	const typeVisibility = useTypesVisibility(types, initialTypeVisibility);

	return {
		...percentileVisibility,
		...sleVisibility,
		...typeVisibility,
	};
}
