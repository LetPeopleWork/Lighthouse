import { useCallback, useEffect, useRef, useState } from "react";

const REVEAL_INTERVAL_MS = 220;

export interface FeatureFeverReveal {
	frame: number | null;
	isRunning: boolean;
	run: () => void;
	showLatest: () => void;
}

export function useFeatureFeverReveal(maxLength: number): FeatureFeverReveal {
	const [frame, setFrame] = useState<number | null>(null);
	const [isRunning, setIsRunning] = useState(false);
	const intervalRef = useRef<ReturnType<typeof setInterval> | null>(null);

	const stop = useCallback(() => {
		if (intervalRef.current !== null) {
			clearInterval(intervalRef.current);
			intervalRef.current = null;
		}
		setIsRunning(false);
	}, []);

	const showLatest = useCallback(() => {
		stop();
		setFrame(null);
	}, [stop]);

	const run = useCallback(() => {
		stop();
		if (maxLength <= 1) {
			setFrame(0);
			return;
		}
		setFrame(0);
		setIsRunning(true);
		intervalRef.current = setInterval(() => {
			setFrame((current) => {
				const next = (current ?? 0) + 1;
				if (next >= maxLength - 1) {
					stop();
					return maxLength - 1;
				}
				return next;
			});
		}, REVEAL_INTERVAL_MS);
	}, [maxLength, stop]);

	useEffect(() => stop, [stop]);

	return { frame, isRunning, run, showLatest };
}
