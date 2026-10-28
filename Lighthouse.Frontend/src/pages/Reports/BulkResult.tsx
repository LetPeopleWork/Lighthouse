import {
	Box,
	Button,
	CircularProgress,
	DialogActions,
	DialogContent,
	Stack,
	Typography,
} from "@mui/material";
import type React from "react";
import { Link } from "react-router";
import type { IReportCreationOutcome } from "../../models/Reports/ReportsAcrossOwners";
import { useTerminology } from "../../services/TerminologyContext";
import { ownerTermKeyOf, reportPathOf } from "../Common/Reports/reportOwner";

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

const OutcomeLine: React.FC<{ outcome: IReportCreationOutcome }> = ({
	outcome,
}) => {
	const { getTerm } = useTerminology();
	const owner = { kind: outcome.ownerKind, id: outcome.ownerId };
	const reportId = outcome.status === "created" ? outcome.reportId : undefined;
	const isCreated = reportId !== undefined;
	return (
		<Box sx={{ display: "flex", alignItems: "center", gap: 2 }}>
			<Typography sx={{ flex: 1 }}>
				{`${getTerm(ownerTermKeyOf(owner))} ${outcome.ownerName ?? ""}`.trim()}
			</Typography>
			<Typography color={isCreated ? "success.main" : "error"}>
				{isCreated ? "Created" : "Not created"}
			</Typography>
			{isCreated && (
				<Button
					component={Link}
					to={reportPathOf(owner, reportId)}
					size="small"
				>
					Open
				</Button>
			)}
		</Box>
	);
};

interface BulkResultProps {
	count: number;
	run: BulkRun;
	onDone: () => void;
}

/** How the run went: while it works, then one line per owner, or that no answer came. */
const BulkResult: React.FC<Readonly<BulkResultProps>> = ({
	count,
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
				<Stack spacing={1}>
					<Typography variant="h6" component="p">
						{headlineOf(run.outcomes)}
					</Typography>
					{run.outcomes.map((outcome) => (
						<OutcomeLine
							key={`${outcome.ownerKind}:${outcome.ownerId}`}
							outcome={outcome}
						/>
					))}
				</Stack>
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
