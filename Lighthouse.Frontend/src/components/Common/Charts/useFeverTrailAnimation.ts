import { useEffect, useState } from "react";

const REVEAL_INTERVAL_MS = 220;

export function useFeverTrailAnimation(pointCount: number): number {
	const [visibleCount, setVisibleCount] = useState(() =>
		Math.min(pointCount, 1),
	);

	useEffect(() => {
		if (pointCount <= 1) {
			setVisibleCount(pointCount);
			return;
		}

		setVisibleCount(1);
		const interval = setInterval(() => {
			setVisibleCount((current) => {
				if (current >= pointCount) {
					clearInterval(interval);
					return current;
				}
				return current + 1;
			});
		}, REVEAL_INTERVAL_MS);

		return () => clearInterval(interval);
	}, [pointCount]);

	return visibleCount;
}
