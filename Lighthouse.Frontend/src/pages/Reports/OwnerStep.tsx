import {
	Box,
	Button,
	Checkbox,
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

export const ownerKeyOf = (owner: IReportCreationOwner): string =>
	`${owner.ownerKind}:${owner.ownerId}`;

export const isOverTheCeiling = (pickedCount: number): boolean =>
	pickedCount > MOST_PER_BULK_RUN;

interface OwnerStepProps {
	/** The owners that may be picked, in the order they are listed and sent. */
	teams: readonly IReportCreationOwner[];
	picked: ReadonlySet<string>;
	onToggle: (owner: IReportCreationOwner) => void;
	onSelectAll: (owners: readonly IReportCreationOwner[]) => void;
	stepNumber: number;
	stepCount: number;
	disabled: boolean;
}

const OwnerStep: React.FC<Readonly<OwnerStepProps>> = ({
	teams,
	picked,
	onToggle,
	onSelectAll,
	stepNumber,
	stepCount,
	disabled,
}) => {
	const { getTerm } = useTerminology();
	const teamsHeadingId = useId();
	const pickedCount = teams.filter((team) =>
		picked.has(ownerKeyOf(team)),
	).length;

	return (
		<Stack spacing={2}>
			<Typography variant="h6" component="h3">
				{`Step ${stepNumber} of ${stepCount} — Who gets this report?`}
			</Typography>
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
			{isOverTheCeiling(pickedCount) && (
				<Typography color="error" align="right">
					{`Pick at most ${MOST_PER_BULK_RUN} ${getTerm(TERMINOLOGY_KEYS.TEAMS)} and ${getTerm(TERMINOLOGY_KEYS.PORTFOLIOS)}.`}
				</Typography>
			)}
		</Stack>
	);
};

export default OwnerStep;
