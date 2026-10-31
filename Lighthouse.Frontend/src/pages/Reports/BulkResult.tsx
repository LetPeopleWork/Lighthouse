import OpenInNewIcon from "@mui/icons-material/OpenInNew";
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
import { Link, useHref } from "react-router";
import type { IReportCreationOutcome } from "../../models/Reports/ReportsAcrossOwners";
import { TERMINOLOGY_KEYS } from "../../models/TerminologyKeys";
import { useTerminology } from "../../services/TerminologyContext";
import { isTauriEnv } from "../../utils/tauri";
import { OwnerKindIcon } from "../Common/Reports/OwnerKindIcon";
import {
	ownerOfReport,
	ownerTermKeyOf,
	reportPathOf,
} from "../Common/Reports/reportOwner";
import { notCreatedLineFor } from "../Common/Reports/wizard/reportRefusalCopy";
import { ownerKeyOf } from "./OwnerStep";

const NOT_CONFIRMED_CREATED =
	"It could not be confirmed which reports were created.";

export type BulkRun =
	| { status: "pending" }
	| { status: "error" }
	| { status: "success"; outcomes: readonly IReportCreationOutcome[] };

export const reportsWord = (count: number) =>
	count === 1 ? "report" : "reports";

/** A bulk run sent and not yet answered, such as "Creating 3 reports…". */
export const UnderWay: React.FC<Readonly<{ doing: string; count: number }>> = ({
	doing,
	count,
}) => (
	<DialogContent>
		<Stack direction="row" spacing={2} sx={{ alignItems: "center" }}>
			<CircularProgress size={24} />
			<Typography>{`${doing} ${count} ${reportsWord(count)}…`}</Typography>
		</Stack>
	</DialogContent>
);

/** No answer came, so the run's outcome is unknown and the Reports page is where to see it. */
export const NotConfirmed: React.FC<
	Readonly<{ message: string; onDone: () => void }>
> = ({ message, onDone }) => (
	<>
		<DialogContent>
			<Typography>{message}</Typography>
		</DialogContent>
		<DialogActions>
			<Button onClick={onDone}>Go to Reports</Button>
		</DialogActions>
	</>
);

const headlineOf = (outcomes: readonly IReportCreationOutcome[]): string => {
	const created = outcomes.filter(
		(outcome) => outcome.status === "created",
	).length;
	return `${created} ${reportsWord(created)} created, ${outcomes.length - created} not created`;
};

/**
 * In a browser the report opens in a new tab, so the result stays to open the others from. The desktop app opens no
 * second window, so there Open moves to the report in place.
 */
const OpenReport: React.FC<Readonly<{ path: string }>> = ({ path }) => {
	const href = useHref(path);
	return isTauriEnv() ? (
		<Button component={Link} to={path} size="small">
			Open
		</Button>
	) : (
		<Button
			href={href}
			target="_blank"
			rel="noopener noreferrer"
			size="small"
			endIcon={<OpenInNewIcon />}
		>
			Open
		</Button>
	);
};

const CreatedStatus: React.FC<
	Readonly<{ outcome: IReportCreationOutcome; reportId: number }>
> = ({ outcome, reportId }) => (
	<>
		<Typography color="success.main">Created</Typography>
		<OpenReport path={reportPathOf(ownerOfReport(outcome), reportId)} />
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
	const ownerTerm = getTerm(ownerTermKeyOf(ownerOfReport(outcome)));
	return (
		<ListItem disableGutters sx={{ gap: 2 }}>
			<Stack direction="row" spacing={1} sx={{ flex: 1, alignItems: "center" }}>
				<OwnerKindIcon kind={ownerOfReport(outcome).kind} title={ownerTerm} />
				<Typography>{outcome.ownerName ?? ""}</Typography>
			</Stack>
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
	/** The name each owner was picked under, by owner key, for a row the server leaves unnamed. */
	pickedNames: ReadonlyMap<string, string>;
	onDone: () => void;
}

/** How the run went: while it works, then one line per owner, or that no answer came. */
const BulkResult: React.FC<Readonly<BulkResultProps>> = ({
	count,
	minimumLengthDays,
	run,
	pickedNames,
	onDone,
}) => {
	if (run.status === "pending") {
		return <UnderWay doing="Creating" count={count} />;
	}

	if (run.status === "error") {
		return <NotConfirmed message={NOT_CONFIRMED_CREATED} onDone={onDone} />;
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
							key={ownerKeyOf(outcome)}
							outcome={{
								...outcome,
								ownerName:
									outcome.ownerName ?? pickedNames.get(ownerKeyOf(outcome)),
							}}
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
