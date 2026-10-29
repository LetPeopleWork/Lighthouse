import {
	Button,
	CircularProgress,
	DialogActions,
	DialogContent,
	List,
	ListItem,
	Stack,
	Typography,
} from "@mui/material";
import type React from "react";
import { Link } from "react-router";
import type { IReportCreationOutcome } from "../../models/Reports/ReportsAcrossOwners";
import { TERMINOLOGY_KEYS } from "../../models/TerminologyKeys";
import { useTerminology } from "../../services/TerminologyContext";
import { ownerTermKeyOf, reportPathOf } from "../Common/Reports/reportOwner";
import { notCreatedLineFor } from "../Common/Reports/wizard/reportRefusalCopy";

const NOT_CONFIRMED_CREATED =
	"It could not be confirmed which reports were created.";

export type BulkRun =
	| { status: "pending" }
	| { status: "error" }
	| { status: "success"; outcomes: readonly IReportCreationOutcome[] };

const reportsWord = (count: number) => (count === 1 ? "report" : "reports");

const headlineOf = (outcomes: readonly IReportCreationOutcome[]): string => {
	const created = outcomes.filter(
		(outcome) => outcome.status === "created",
	).length;
	return `${created} ${reportsWord(created)} created, ${outcomes.length - created} not created`;
};

const ownerOf = (outcome: IReportCreationOutcome) => ({
	kind: outcome.ownerKind,
	id: outcome.ownerId,
});

const CreatedStatus: React.FC<
	Readonly<{ outcome: IReportCreationOutcome; reportId: number }>
> = ({ outcome, reportId }) => (
	<>
		<Typography color="success.main">Created</Typography>
		<Button
			component={Link}
			to={reportPathOf(ownerOf(outcome), reportId)}
			size="small"
		>
			Open
		</Button>
	</>
);

const NotCreatedStatus: React.FC<
	Readonly<{
		outcome: IReportCreationOutcome;
		ownerTerm: string;
		minimumLengthDays: number;
	}>
> = ({ outcome, ownerTerm, minimumLengthDays }) => {
	const { getTerm } = useTerminology();
	return (
		<Typography color="error">
			{notCreatedLineFor(outcome.code, {
				minimumLengthDays,
				earliestStart: outcome.earliestStart ?? null,
				ownerTerm,
				workItemsTerm: getTerm(TERMINOLOGY_KEYS.WORK_ITEMS),
				ownerName: outcome.ownerName ?? `this ${ownerTerm}`,
			})}
		</Typography>
	);
};

const OutcomeLine: React.FC<
	Readonly<{ outcome: IReportCreationOutcome; minimumLengthDays: number }>
> = ({ outcome, minimumLengthDays }) => {
	const { getTerm } = useTerminology();
	const ownerTerm = getTerm(ownerTermKeyOf(ownerOf(outcome)));
	return (
		<ListItem disableGutters sx={{ gap: 2 }}>
			<Typography sx={{ flex: 1 }}>
				{`${ownerTerm} ${outcome.ownerName ?? ""}`.trim()}
			</Typography>
			{outcome.status === "created" && outcome.reportId !== undefined ? (
				<CreatedStatus outcome={outcome} reportId={outcome.reportId} />
			) : (
				<NotCreatedStatus
					outcome={outcome}
					ownerTerm={ownerTerm}
					minimumLengthDays={minimumLengthDays}
				/>
			)}
		</ListItem>
	);
};

interface BulkResultProps {
	count: number;
	minimumLengthDays: number;
	run: BulkRun;
	onDone: () => void;
}

/** How the run went: while it works, then one line per owner, or that no answer came. */
const BulkResult: React.FC<Readonly<BulkResultProps>> = ({
	count,
	minimumLengthDays,
	run,
	onDone,
}) => {
	if (run.status === "pending") {
		return (
			<DialogContent>
				<Stack direction="row" spacing={2} sx={{ alignItems: "center" }}>
					<CircularProgress size={24} />
					<Typography>{`Creating ${count} ${reportsWord(count)}…`}</Typography>
				</Stack>
			</DialogContent>
		);
	}

	if (run.status === "error") {
		return (
			<>
				<DialogContent>
					<Typography>{NOT_CONFIRMED_CREATED}</Typography>
				</DialogContent>
				<DialogActions>
					<Button onClick={onDone}>Go to Reports</Button>
				</DialogActions>
			</>
		);
	}

	return (
		<>
			<DialogContent>
				<Typography variant="h6" component="p">
					{headlineOf(run.outcomes)}
				</Typography>
				<List dense>
					{run.outcomes.map((outcome) => (
						<OutcomeLine
							key={`${outcome.ownerKind}:${outcome.ownerId}`}
							outcome={outcome}
							minimumLengthDays={minimumLengthDays}
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
};

export default BulkResult;
