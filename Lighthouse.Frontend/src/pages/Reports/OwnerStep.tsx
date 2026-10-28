import {
	Checkbox,
	FormControlLabel,
	FormGroup,
	Stack,
	Typography,
} from "@mui/material";
import type React from "react";
import { useId } from "react";
import type { IReportCreationOwner } from "../../models/Reports/ReportsAcrossOwners";
import { TERMINOLOGY_KEYS } from "../../models/TerminologyKeys";
import { useTerminology } from "../../services/TerminologyContext";

export const ownerKeyOf = (owner: IReportCreationOwner): string =>
	`${owner.ownerKind}:${owner.ownerId}`;

interface OwnerStepProps {
	/** The owners that may be picked, in the order they are listed and sent. */
	teams: readonly IReportCreationOwner[];
	picked: ReadonlySet<string>;
	onToggle: (owner: IReportCreationOwner) => void;
	stepNumber: number;
	stepCount: number;
	disabled: boolean;
}

const OwnerStep: React.FC<Readonly<OwnerStepProps>> = ({
	teams,
	picked,
	onToggle,
	stepNumber,
	stepCount,
	disabled,
}) => {
	const { getTerm } = useTerminology();
	const teamsHeadingId = useId();

	return (
		<Stack spacing={2}>
			<Typography variant="h6" component="h3">
				{`Step ${stepNumber} of ${stepCount} — Who gets this report?`}
			</Typography>
			<FormGroup role="group" aria-labelledby={teamsHeadingId}>
				<Typography id={teamsHeadingId} variant="subtitle1">
					{getTerm(TERMINOLOGY_KEYS.TEAMS)}
				</Typography>
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
		</Stack>
	);
};

export default OwnerStep;
