import { useCallback, useState } from "react";

export function useShowTips(
	ownerType: "team" | "portfolio",
	ownerId: number,
): {
	showTips: boolean;
	toggleShowTips: () => void;
} {
	const storageKey = `lighthouse:metrics:${ownerType}:${ownerId}:showTips`;

	const [showTips, setShowTips] = useState<boolean>(() => {
		try {
			const stored = localStorage.getItem(storageKey);
			if (stored === "false") {
				return false;
			}
		} catch {
			/* ignore storage errors */
		}
		return true;
	});

	const toggleShowTips = useCallback(() => {
		setShowTips((prev) => {
			const next = !prev;
			try {
				localStorage.setItem(storageKey, String(next));
			} catch {
				/* ignore storage errors */
			}
			return next;
		});
	}, [storageKey]);

	return { showTips, toggleShowTips };
}
