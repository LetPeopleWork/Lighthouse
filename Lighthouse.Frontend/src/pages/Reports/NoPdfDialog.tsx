import {
	Button,
	Dialog,
	DialogActions,
	DialogContent,
	DialogTitle,
	List,
	ListItem,
	Typography,
} from "@mui/material";
import type React from "react";
import type { IReportAcrossOwners } from "../../models/Reports/ReportsAcrossOwners";
import { useTerminology } from "../../services/TerminologyContext";
import { PART_OF_THE_PAGE_COULD_NOT_BE_LOADED } from "../Common/Reports/print/usePrintOnce";
import { ownerOfReport, ownerTermKeyOf } from "../Common/Reports/reportOwner";

const TITLE_ID = "no-pdf-was-made-title";
const DESCRIPTION_ID = "no-pdf-was-made-description";

interface NoPdfDialogProps {
	/** The reports that could not be read, in print order; none when a part of the page failed. */
	readonly failedReports: readonly IReportAcrossOwners[];
	/** Offered only when leaving the failed reports out still leaves something to print. */
	readonly onLeaveItOut: (() => void) | undefined;
	readonly onRetry: () => void;
	readonly onCancel: () => void;
}

/** Says why no PDF of the picked reports was made, naming each report that could not be read. */
const NoPdfDialog: React.FC<NoPdfDialogProps> = ({
	failedReports,
	onLeaveItOut,
	onRetry,
	onCancel,
}) => {
	const { getTerm } = useTerminology();
	const lineOf = (report: IReportAcrossOwners) =>
		`${getTerm(ownerTermKeyOf(ownerOfReport(report)))} ${report.ownerName}'s report "${report.name}" couldn't be loaded.`;
	return (
		<Dialog
			open
			onClose={onCancel}
			aria-labelledby={TITLE_ID}
			aria-describedby={DESCRIPTION_ID}
			maxWidth="sm"
			fullWidth
		>
			<DialogTitle id={TITLE_ID}>No PDF was made</DialogTitle>
			<DialogContent id={DESCRIPTION_ID}>
				{failedReports.length === 0 ? (
					<Typography>{PART_OF_THE_PAGE_COULD_NOT_BE_LOADED}</Typography>
				) : (
					<List dense>
						{failedReports.map((report) => (
							<ListItem
								key={`${report.ownerKind}-${report.ownerId}-${report.id}`}
								disableGutters
							>
								{lineOf(report)}
							</ListItem>
						))}
					</List>
				)}
			</DialogContent>
			<DialogActions>
				<Button onClick={onCancel}>Cancel</Button>
				{onLeaveItOut !== undefined && (
					<Button onClick={onLeaveItOut}>Leave it out</Button>
				)}
				<Button variant="contained" onClick={onRetry}>
					Retry
				</Button>
			</DialogActions>
		</Dialog>
	);
};

export default NoPdfDialog;
