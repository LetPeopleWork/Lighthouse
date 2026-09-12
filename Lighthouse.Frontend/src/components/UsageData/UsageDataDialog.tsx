import Button from "@mui/material/Button";
import Dialog from "@mui/material/Dialog";
import DialogActions from "@mui/material/DialogActions";
import DialogContent from "@mui/material/DialogContent";
import DialogTitle from "@mui/material/DialogTitle";
import Link from "@mui/material/Link";
import List from "@mui/material/List";
import ListItem from "@mui/material/ListItem";
import Typography from "@mui/material/Typography";
import type React from "react";

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

/**
 * Every sentence below ends in a full stop, and that is load-bearing rather than tidy. The copy is
 * held to its promises by patterns that refuse to cross a sentence boundary, so a missing full stop
 * lets two unrelated sentences read as one claim - "we will not do X. Ask again later" becoming
 * "will not ... ask".
 *
 * The dialog also says nothing at all about the IP address, deliberately. The instance's address
 * reaches the collector's edge on any request, as it does for any website; what the controls
 * suppress is whether it is stored and enriched. Saying the weaker true thing invites a reader to
 * hear the stronger false one, and this audience is engineers who would notice.
 */
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
	return (
		<Dialog open={open} onClose={onClose} maxWidth="sm" fullWidth>
			<DialogTitle>May Lighthouse send usage data?</DialogTitle>

			<DialogContent dividers>
				<Typography variant="body2" sx={{ mb: 1 }}>
					Once a day, this instance would send five things about itself.
				</Typography>

				<List dense disablePadding sx={{ mb: 2, listStyleType: "disc", pl: 3 }}>
					{fields.map((field) => (
						<ListItem key={field} sx={{ display: "list-item", py: 0 }}>
							{field}
						</ListItem>
					))}
				</List>

				<Typography variant="body2" sx={{ mb: 2 }}>
					We never send {neverSent.join(", ")}.
				</Typography>

				<Typography variant="body2" sx={{ mb: 2 }}>
					{collectorName} would hold it, on servers in {dataResidency}.
					Processing can also happen elsewhere, including in the US. We keep it
					for one year.
				</Typography>

				<Typography variant="body2" sx={{ mb: 2 }}>
					{willAskAgain
						? "If you decide later, we will ask you again in a few months."
						: "Whichever you choose, we will not ask you again."}
				</Typography>

				<Typography variant="body2">
					<Link href={docsUrl} target="_blank" rel="noopener noreferrer">
						Read the full usage data page
					</Link>
					, which also covers what you can change afterwards.
				</Typography>
			</DialogContent>

			<DialogActions>
				<Button onClick={() => onDecision("declined")} color="inherit">
					No, thank you
				</Button>
				<Button onClick={() => onDecision("granted")} variant="contained">
					Yes, send it
				</Button>
			</DialogActions>
		</Dialog>
	);
};

export default UsageDataDialog;
