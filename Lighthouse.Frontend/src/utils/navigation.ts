import type { NavigateFunction } from "react-router";

/**
 * For handlers that must return nothing. Under a data router `navigate` hands back a promise, and a
 * navigation that fails there should still reach the console rather than reject with nobody listening.
 */
export const reportFailedNavigation = (
	navigation: ReturnType<NavigateFunction>,
): void => {
	Promise.resolve(navigation).catch((error: unknown) => {
		console.error("Navigation failed:", error);
	});
};
