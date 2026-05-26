import { useCallback, useEffect, useState } from "react";

export const PACE_BANDS_STORAGE_KEY = "workItemAgingPaceBandsEnabled";

interface UseShowPaceBandsResult {
	showPaceBands: boolean;
	togglePaceBands: () => void;
}

export const useShowPaceBands = (): UseShowPaceBandsResult => {
	const [showPaceBands, setShowPaceBands] = useState<boolean>(false);

	useEffect(() => {
		const stored = localStorage.getItem(PACE_BANDS_STORAGE_KEY);
		if (stored !== null) {
			setShowPaceBands(stored === "true");
		}
	}, []);

	const togglePaceBands = useCallback((): void => {
		setShowPaceBands((previous) => {
			const next = !previous;
			localStorage.setItem(PACE_BANDS_STORAGE_KEY, next.toString());
			return next;
		});
	}, []);

	return { showPaceBands, togglePaceBands };
};
