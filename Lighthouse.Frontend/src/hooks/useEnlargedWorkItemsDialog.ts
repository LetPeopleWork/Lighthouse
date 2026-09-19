import { useCallback, useState } from "react";

/**
 * Named for the state it holds rather than for the change that introduced it, so that a later
 * rename is never tempting: a renamed key silently forgets every viewer's choice.
 */
export const ENLARGED_WORK_ITEMS_DIALOG_STORAGE_KEY =
	"lighthouse:workItemsDialog:enlarged";

interface UseEnlargedWorkItemsDialogResult {
	enlarged: boolean;
	toggleEnlarged: () => void;
}

/**
 * Whether the work item dialog opens at full screen, remembered for this viewer across every
 * surface that opens it. One key for the dialog rather than one per call site: a working
 * preference a reader has to set again on each of fifteen surfaces is the thing the toggle exists
 * to remove.
 *
 * Read while the state is first created rather than in an effect. Both are house patterns, but the
 * effect form applies the stored value one frame late — invisible when it repaints a chart
 * background, a visible resize when it is a dialog.
 */
export const useEnlargedWorkItemsDialog =
	(): UseEnlargedWorkItemsDialogResult => {
		const [enlarged, setEnlarged] = useState<boolean>(() => {
			try {
				return (
					localStorage.getItem(ENLARGED_WORK_ITEMS_DIALOG_STORAGE_KEY) ===
					"true"
				);
			} catch {
				// A browser with site data blocked still gets a dialog, just not a remembered size.
				return false;
			}
		});

		const toggleEnlarged = useCallback((): void => {
			setEnlarged((current) => {
				const next = !current;
				try {
					localStorage.setItem(
						ENLARGED_WORK_ITEMS_DIALOG_STORAGE_KEY,
						String(next),
					);
				} catch {
					// The size still applies to this dialog; it just will not outlive the tab.
				}
				return next;
			});
		}, []);

		return { enlarged, toggleEnlarged };
	};
