import {
	Alert,
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
import type {
	IReportAcrossOwners,
	IReportRemovalOutcome,
} from "../../models/Reports/ReportsAcrossOwners";
import { notRemovedLineFor } from "../Common/Reports/wizard/reportRefusalCopy";
import { NotConfirmed, reportsWord, UnderWay } from "./BulkResult";
import { useBulkReportRemoval } from "./useBulkReportRemoval";

const TITLE_ID = "remove-selected-reports-title";

const NOT_CONFIRMED_REMOVED =
	"It could not be confirmed which reports were removed.";

const headlineOf = (outcomes: readonly IReportRemovalOutcome[]): string => {
	const removed = outcomes.filter(
		(outcome) => outcome.status === "removed",
	).length;
	return `${removed} ${reportsWord(removed)} removed`;
};

/** Removing a report whose Then the kept history no longer reaches loses that Then for good, so say so first. */
const lostForGoodWarningOf = (
	reports: readonly IReportAcrossOwners[],
): string | undefined => {
	const lost = reports.filter(
		(report) => report.thenAndNow?.thenRebuildable === false,
	).length;
	if (lost === 0) return undefined;
	return lost === 1
		? "1 of them has a Then that can no longer be rebuilt from the kept history; removing it loses it for good."
		: `${lost} of them have a Then that can no longer be rebuilt from the kept history; removing them loses them for good.`;
};

const Confirmation: React.FC<
	Readonly<{
		reports: readonly IReportAcrossOwners[];
		onRemove: () => void;
		onCancel: () => void;
	}>
> = ({ reports, onRemove, onCancel }) => {
	const warning = lostForGoodWarningOf(reports);
	return (
		<>
			<DialogContent>
				{warning !== undefined && (
					<Alert severity="warning" sx={{ mb: 1 }}>
						{warning}
					</Alert>
				)}
				<List dense>
					{reports.map((report) => (
						<ListItem key={report.id} disableGutters>
							{`${report.name} — ${report.ownerName}`}
						</ListItem>
					))}
				</List>
			</DialogContent>
			<DialogActions>
				<Button onClick={onCancel}>Cancel</Button>
				<Button variant="contained" color="error" onClick={onRemove}>
					{`Remove ${reports.length} ${reportsWord(reports.length)}`}
				</Button>
			</DialogActions>
		</>
	);
};

const NotRemovedLine: React.FC<
	Readonly<{
		outcome: IReportRemovalOutcome;
		report: IReportAcrossOwners | undefined;
	}>
> = ({ outcome, report }) => (
	<ListItem disableGutters sx={{ gap: 2 }}>
		<Typography sx={{ flex: 1 }}>{report?.name ?? ""}</Typography>
		<Typography color="error">
			{notRemovedLineFor(outcome.code, {
				ownerName: report?.ownerName ?? "",
			})}
		</Typography>
	</ListItem>
);

const Result: React.FC<
	Readonly<{
		reports: readonly IReportAcrossOwners[];
		outcomes: readonly IReportRemovalOutcome[];
		onDone: () => void;
	}>
> = ({ reports, outcomes, onDone }) => (
	<>
		<DialogContent>
			<Typography variant="h6" component="p">
				{headlineOf(outcomes)}
			</Typography>
			<List dense>
				{outcomes
					.filter((outcome) => outcome.status === "notRemoved")
					.map((outcome) => (
						<NotRemovedLine
							key={outcome.reportId}
							outcome={outcome}
							report={reports.find((report) => report.id === outcome.reportId)}
						/>
					))}
			</List>
		</DialogContent>
		<DialogActions>
			<Button variant="contained" onClick={onDone}>
				Done
			</Button>
		</DialogActions>
	</>
);

interface RemoveSelectedDialogProps {
	reports: readonly IReportAcrossOwners[];
	onCancel: () => void;
	/** Once the removal has been answered, or could not be, and the reader is done with the result. */
	onDone: () => void;
}

/** Asks before removing the picked reports, then shows how the removal went. */
const RemoveSelectedDialog: React.FC<Readonly<RemoveSelectedDialogProps>> = ({
	reports,
	onCancel,
	onDone,
}) => {
	const removal = useBulkReportRemoval();
	const isPending = removal.isPending;
	const isAnswered = removal.isSuccess || removal.isError;
	const close = () => {
		if (isPending) return;
		if (isAnswered) onDone();
		else onCancel();
	};
	const content = () => {
		if (isPending) return <UnderWay doing="Removing" count={reports.length} />;
		if (removal.isError)
			return <NotConfirmed message={NOT_CONFIRMED_REMOVED} onDone={onDone} />;
		if (removal.isSuccess)
			return (
				<Result reports={reports} outcomes={removal.data} onDone={onDone} />
			);
		return (
			<Confirmation
				reports={reports}
				onCancel={onCancel}
				onRemove={() => removal.mutate(reports)}
			/>
		);
	};
	return (
		<Dialog
			open
			onClose={close}
			aria-labelledby={TITLE_ID}
			maxWidth="sm"
			fullWidth
		>
			<DialogTitle id={TITLE_ID}>Remove reports</DialogTitle>
			{content()}
		</Dialog>
	);
};

export default RemoveSelectedDialog;
