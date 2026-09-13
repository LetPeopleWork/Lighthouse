import type React from "react";
import { useEffect, useRef } from "react";
import { useUsageDataConsent } from "../../hooks/useUsageDataConsent";
import { evaluateAskEligibility } from "../../services/UsageData/usageDataAskEligibility";
import { readAskedMarker } from "../../services/UsageData/usageDataAskMarker";
import {
	claimPromptSlot,
	promptSlotHolder,
} from "../../services/UsageData/promptSession";

/**
 * Puts the usage data question in front of somebody who never went looking for it.
 *
 * It draws nothing itself. The dialog already exists and is already opened from the footer icon, so
 * what is missing is only the decision to open it - which keeps one dialog with one set of copy
 * rather than a second one that drifts away from the first.
 */
export const UsageDataAsk = (): React.ReactElement | null => {
	const { mayAsk, decision, openDialog, noteAsked } = useUsageDataConsent();

	// Opening the dialog is not idempotent from this component's point of view: it records that the
	// question was put, and recording it twice would move the window somebody is waiting out. The
	// ref survives the re-renders that follow opening; the browser-side marker survives everything
	// after that.
	const alreadyAsked = useRef(false);

	useEffect(() => {
		if (alreadyAsked.current) {
			return;
		}

		const holder = promptSlotHolder();

		const { shouldAsk } = evaluateAskEligibility({
			mayAsk,
			decision,
			lastAskedAt: readAskedMarker(),
			promptSlotTaken: holder !== null && holder !== "usage-data",
		});

		if (!shouldAsk || !claimPromptSlot("usage-data")) {
			return;
		}

		alreadyAsked.current = true;

		// Recorded as the question is put, not when it is answered. Somebody who closes the dialog
		// has answered nothing, and is exactly the person this has to remember.
		noteAsked();
		openDialog();
	}, [mayAsk, decision, openDialog, noteAsked]);

	return null;
};

export default UsageDataAsk;
