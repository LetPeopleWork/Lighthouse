/**
 * RED scaffold — Epic 5733 slice 01. DELIVER replaces this body.
 *
 * The throw interpolates its arguments because noUnusedParameters is on.
 */

export type UsageDataDecision = "granted" | "declined";

export interface UsageDataDialogProps {
	open: boolean;
	/** The processor's name. The dialog must say who holds the data, not just that it is sent. */
	collectorName: string;
	/** Where the data rests, in words a reader can check — e.g. "Frankfurt, Germany". */
	dataResidency: string;
	/** Every field that would leave this instance, named. The dialog enumerates these verbatim. */
	fields: readonly string[];
	/** Categories the payload never carries. Stated positively in the dialog, not merely implied. */
	neverSent: readonly string[];
	/** Link to the full list — docs/settings/usagedata.md, which also names the GitHub release check. */
	docsUrl: string;
	/**
	 * Whether a decline will be revisited. Derived server-side and handed over as a boolean rather
	 * than a licence tier, so the dialog never has to know what a licence is — and so an anonymous
	 * endpoint never has to disclose one.
	 */
	willAskAgain: boolean;
	onDecision: (decision: UsageDataDecision) => void;
	onClose: () => void;
}

export const UsageDataDialog = ({
	open,
	collectorName,
	dataResidency,
	fields,
	neverSent,
	docsUrl,
	willAskAgain,
	onDecision,
	onClose,
}: UsageDataDialogProps): React.ReactElement => {
	throw new Error(
		`UsageDataDialog(open=${open}, collectorName=${collectorName}, dataResidency=${dataResidency}, ` +
			`fields=${fields.length}, neverSent=${neverSent.length}, docsUrl=${docsUrl}, ` +
			`willAskAgain=${willAskAgain}, onDecision=${typeof onDecision}, ` +
			`onClose=${typeof onClose}) is not implemented`,
	);
};

export default UsageDataDialog;
