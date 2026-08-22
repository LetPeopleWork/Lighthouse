import { useCallback, useEffect, useState } from "react";

const STORAGE_KEY = "lighthouse:deliveries:skip-archive-confirmation";

interface UseArchiveConfirmationPreferenceResult {
	/** Whether the confirmation still has to be shown before archiving. */
	shouldConfirm: boolean;
	/** Remember, on this browser only, that this reader does not want asking again. */
	stopAsking: () => void;
}

/**
 * Archiving is reversible and someone who retires Deliveries every week does not need asking every
 * week. The preference lives in this browser rather than on the account: it is a convenience, and
 * getting the dialog once on a new machine is a smaller cost than a round trip and a column.
 */
export const useArchiveConfirmationPreference =
	(): UseArchiveConfirmationPreferenceResult => {
		const [shouldConfirm, setShouldConfirm] = useState(true);

		useEffect(() => {
			setShouldConfirm(localStorage.getItem(STORAGE_KEY) !== "true");
		}, []);

		const stopAsking = useCallback(() => {
			localStorage.setItem(STORAGE_KEY, "true");
			setShouldConfirm(false);
		}, []);

		return { shouldConfirm, stopAsking };
	};
