import Alert from "@mui/material/Alert";
import Button from "@mui/material/Button";
import Dialog from "@mui/material/Dialog";
import DialogActions from "@mui/material/DialogActions";
import DialogContent from "@mui/material/DialogContent";
import DialogTitle from "@mui/material/DialogTitle";
import Link from "@mui/material/Link";
import Typography from "@mui/material/Typography";
import type React from "react";

export type UsageDataDecision = "granted" | "declined";

export interface UsageDataDialogProps {
	open: boolean;
	/** Categories the payload never carries. Stated positively in the dialog, not merely implied. */
	neverSent: readonly string[];
	/** Link to the full account — docs/settings/usagedata.md. */
	docsUrl: string;
	/** Set when the last answer could not be recorded, so the dialog can stay open and say so. */
	failedToRecord?: boolean;
	onDecision: (decision: UsageDataDecision) => void;
	onClose: () => void;
}

/**
 * Every sentence below ends in a full stop, and that is load-bearing rather than tidy. The copy is
 * held to its promises by patterns that refuse to cross a sentence boundary, so a missing full stop
 * lets two unrelated sentences read as one claim - "we will not do X. Ask again later" becoming
 * "will not ... ask".
 *
 * The dialog deliberately does not enumerate what is sent, name who holds it, or say how often it
 * goes. All of that changes as the feature grows, and a list in a dialog goes stale silently while
 * still looking authoritative. The linked page carries it and is the one place to keep current.
 *
 * The dialog also says nothing at all about the IP address, deliberately. The instance's address
 * reaches the collector's edge on any request, as it does for any website; what the controls
 * suppress is whether it is stored and enriched. Saying the weaker true thing invites a reader to
 * hear the stronger false one, and this audience is engineers who would notice.
 */
export const UsageDataDialog = ({
	open,
	neverSent,
	docsUrl,
	failedToRecord = false,
	onDecision,
	onClose,
}: UsageDataDialogProps): React.ReactElement => {
	return (
		<Dialog open={open} onClose={onClose} maxWidth="sm" fullWidth>
			<DialogTitle>May Lighthouse send usage data?</DialogTitle>

			<DialogContent dividers>
				<Typography variant="body2" sx={{ mb: 2 }}>
					Lighthouse can tell us how it is being used. This is off unless you
					switch it on.
				</Typography>

				<Typography variant="body2" sx={{ mb: 2 }}>
					You are identified only by a random identifier, created for this
					purpose, that means nothing anywhere else. We never send{" "}
					{neverSent.join(", ")}. The usage data page lists exactly what is
					sent, and is kept current as that changes.
				</Typography>

				<Typography variant="subtitle2" sx={{ mb: 1 }}>
					Why we ask for this
				</Typography>

				<Typography variant="body2" sx={{ mb: 2 }}>
					We cannot see how Lighthouse is used. Without this we are guessing
					which parts earn their keep, which features nobody opens, and where
					our time is best spent. Usage data lets us decide from evidence rather
					than intuition, so Lighthouse improves in the directions people
					actually use it.
				</Typography>

				<Typography variant="subtitle2" sx={{ mb: 1 }}>
					You stay in control
				</Typography>

				{/* This is a limit rather than a feature, and the dialog is where it has to be read.
				    Somebody deciding here relies on this, not on the linked page. */}
				<Typography variant="body2" sx={{ mb: 2 }}>
					You can change your mind at any time from the footer, and nothing
					further is sent. Changing your mind does not erase what was already
					sent.
				</Typography>

				<Typography variant="body2">
					<Link href={docsUrl} target="_blank" rel="noopener noreferrer">
						Read the full usage data page
					</Link>
					, which covers what is sent, who holds it, where it is stored, and how
					long it is kept.
				</Typography>
			</DialogContent>

			{failedToRecord ? (
				<Alert severity="error" sx={{ mx: 3, mb: 1 }}>
					Your answer could not be saved, so nothing has changed. Please try
					again.
				</Alert>
			) : null}

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
