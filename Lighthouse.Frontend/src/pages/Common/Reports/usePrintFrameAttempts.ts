import { useEffect, useRef, useState } from "react";

/** One export's go at printing; each draws into a frame of its own, even for the same reports. */
export interface PrintFrameAttempt<T> {
	readonly printing: T;
	readonly attempt: number;
}

/**
 * The export whose frame is on the page, if any. A fresh attempt replaces it at once; the frame goes again a turn
 * after its outcome, unless a newer attempt has taken its place by then.
 */
export const usePrintFrameAttempts = <T>() => {
	const attempts = useRef(0);
	const [current, setCurrent] = useState<PrintFrameAttempt<T> | undefined>();
	const frameRemoval = useRef<ReturnType<typeof setTimeout> | undefined>(
		undefined,
	);

	useEffect(() => () => clearTimeout(frameRemoval.current), []);

	const start = (printing: T) => {
		attempts.current += 1;
		setCurrent({ printing, attempt: attempts.current });
	};

	// Never inside the dialog's own closing: some browsers say it closed before print() has returned.
	const removeTheFrameOf = (attempt: number) => {
		frameRemoval.current = setTimeout(() =>
			setCurrent((shown) => (shown?.attempt === attempt ? undefined : shown)),
		);
	};

	return { current, start, removeTheFrameOf };
};
