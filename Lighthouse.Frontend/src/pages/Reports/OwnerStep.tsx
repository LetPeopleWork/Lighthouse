import {
	Box,
	Button,
	Checkbox,
	CircularProgress,
	FormControlLabel,
	FormGroup,
	Stack,
	Typography,
} from "@mui/material";
import type React from "react";
import { useId } from "react";
import {
	type IReportCreationOwner,
	MOST_PER_BULK_RUN,
} from "../../models/Reports/ReportsAcrossOwners";
import { TERMINOLOGY_KEYS } from "../../models/TerminologyKeys";
import { useTerminology } from "../../services/TerminologyContext";

export const ownerKeyOf = (
	owner: Pick<IReportCreationOwner, "ownerKind" | "ownerId">,
): string => `${owner.ownerKind}:${owner.ownerId}`;

export const isOverTheCeiling = (pickedCount: number): boolean =>
	pickedCount > MOST_PER_BULK_RUN;

/** Whether the owners to pick from are known: still on their way, could not be loaded, or here. */
export type OwnersStatus = "loading" | "failed" | "ready";

interface OwnerStepProps {
	ownersStatus: OwnersStatus;
	onRetry: () => void;
	/** The owners that may be picked, in the order they are listed and sent. */
	teams: readonly IReportCreationOwner[];
	picked: ReadonlySet<string>;
	onToggle: (owner: IReportCreationOwner) => void;
	onSelectAll: (owners: readonly IReportCreationOwner[]) => void;
	stepNumber: number;
	stepCount: number;
	disabled: boolean;
}

type OwnerListProps = Omit<
	OwnerStepProps,
	"ownersStatus" | "onRetry" | "stepNumber" | "stepCount"
>;

const OwnerList: React.FC<Readonly<OwnerListProps>> = ({
	teams,
	picked,
	onToggle,
	onSelectAll,
	disabled,
}) => {
	const { getTerm } = useTerminology();
	const teamsHeadingId = useId();
	const pickedCount = teams.filter((team) =>
		picked.has(ownerKeyOf(team)),
	).length;

	return (
		<>
			<FormGroup role="group" aria-labelledby={teamsHeadingId}>
				<Box
					sx={{
						display: "flex",
						justifyContent: "space-between",
						alignItems: "center",
					}}
				>
					<Typography id={teamsHeadingId} variant="subtitle1">
						{getTerm(TERMINOLOGY_KEYS.TEAMS)}
					</Typography>
					<Button
						size="small"
						disabled={disabled}
						onClick={() => onSelectAll(teams)}
					>
						Select all
					</Button>
				</Box>
				{teams.map((team) => (
					<FormControlLabel
						key={ownerKeyOf(team)}
						label={team.ownerName}
						control={
							<Checkbox
								checked={picked.has(ownerKeyOf(team))}
								disabled={disabled}
								onChange={() => onToggle(team)}
							/>
						}
					/>
				))}
			</FormGroup>
			<Typography variant="body2" color="text.secondary">
				{`Only ${getTerm(TERMINOLOGY_KEYS.TEAMS)} and ${getTerm(TERMINOLOGY_KEYS.PORTFOLIOS)} you are Admin of are listed.`}
			</Typography>
			{isOverTheCeiling(pickedCount) && (
				<Typography color="error" align="right">
					{`Pick at most ${MOST_PER_BULK_RUN} ${getTerm(TERMINOLOGY_KEYS.TEAMS)} and ${getTerm(TERMINOLOGY_KEYS.PORTFOLIOS)}.`}
				</Typography>
			)}
		</>
	);
};

const OwnerStep: React.FC<Readonly<OwnerStepProps>> = ({
	ownersStatus,
	onRetry,
	stepNumber,
	stepCount,
	...list
}) => {
	const { getTerm } = useTerminology();
	const ownersName = `${getTerm(TERMINOLOGY_KEYS.TEAMS)} and ${getTerm(TERMINOLOGY_KEYS.PORTFOLIOS)}`;

	return (
		<Stack spacing={2}>
			<Typography variant="h6" component="h3">
				{`Step ${stepNumber} of ${stepCount} — Who gets this report?`}
			</Typography>
			{ownersStatus === "loading" && (
				<CircularProgress aria-label={`Loading your ${ownersName}`} />
			)}
			{ownersStatus === "failed" && (
				<Box sx={{ display: "flex", alignItems: "center", gap: 2 }}>
					<Typography role="alert">{`Your ${ownersName} couldn't be loaded.`}</Typography>
					<Button onClick={onRetry}>Retry</Button>
				</Box>
			)}
			{ownersStatus === "ready" && list.teams.length === 0 && (
				<Typography>
					{`You are not Admin of any ${getTerm(TERMINOLOGY_KEYS.TEAM)} or ${getTerm(TERMINOLOGY_KEYS.PORTFOLIO)}.`}
				</Typography>
			)}
			{ownersStatus === "ready" && list.teams.length > 0 && (
				<OwnerList {...list} />
			)}
		</Stack>
	);
};

export default OwnerStep;
