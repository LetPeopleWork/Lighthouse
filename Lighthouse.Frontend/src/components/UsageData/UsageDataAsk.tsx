import type React from "react";

/**
 * Puts the usage data question in front of somebody who never went looking for it.
 *
 * RED scaffold (DISTILL). DELIVER replaces the body and mounts it; nothing renders it yet, so a
 * scaffold that throws cannot take the application down in the meantime.
 *
 * It draws nothing itself. The dialog already exists and is already opened from the footer icon, so
 * what is missing is only the decision to open it - which keeps one dialog with one set of copy
 * rather than a second one that drifts away from the first.
 */
export const UsageDataAsk = (): React.ReactElement | null => {
	throw new Error("UsageDataAsk is not implemented — RED scaffold.");
};

export default UsageDataAsk;
