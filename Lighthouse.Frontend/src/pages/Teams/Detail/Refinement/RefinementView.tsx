import {
	Stack,
	Table,
	TableBody,
	TableCell,
	TableContainer,
	TableHead,
	TableRow,
	Typography,
} from "@mui/material";
import type React from "react";
import { useContext, useEffect, useState } from "react";
import { ErrorSnackbarContext } from "../../../../components/Common/SnackbarErrorHandler/SnackbarErrorHandler";
import type { IRefinementView } from "../../../../models/Refinement/Refinement";
import type { Team } from "../../../../models/Team/Team";
import { TERMINOLOGY_KEYS } from "../../../../models/TerminologyKeys";
import { ApiServiceContext } from "../../../../services/Api/ApiServiceContext";
import { useTerminology } from "../../../../services/TerminologyContext";
import RefinementRow from "./RefinementRow";

interface RefinementViewProps {
	team: Team;
}

const RefinementView: React.FC<Readonly<RefinementViewProps>> = ({ team }) => {
	const { refinementService } = useContext(ApiServiceContext);
	// Read without the throwing hook: the view still renders where no snackbar host is mounted.
	const errorSnackbar = useContext(ErrorSnackbarContext);
	const { getTerm } = useTerminology();
	const [refinement, setRefinement] = useState<IRefinementView | null>(null);

	useEffect(() => {
		let isCurrent = true;

		refinementService
			.getRefinement(team.id)
			.then((answer) => {
				if (isCurrent) {
					setRefinement(answer);
				}
			})
			.catch((error: unknown) => {
				if (isCurrent) {
					errorSnackbar?.showError(
						error instanceof Error ? error.message : String(error),
					);
				}
			});

		return () => {
			isCurrent = false;
		};
	}, [team.id, refinementService, errorSnackbar]);

	if (refinement === null) {
		return null;
	}

	const refinementTerm = getTerm(TERMINOLOGY_KEYS.REFINEMENT);
	const count = refinement.workItems.length;

	if (count === 0) {
		return (
			<Typography>
				{`No ${getTerm(TERMINOLOGY_KEYS.WORK_ITEMS)} in ${refinementTerm} states right now`}
			</Typography>
		);
	}

	const workItemsTerm = getTerm(
		count === 1 ? TERMINOLOGY_KEYS.WORK_ITEM : TERMINOLOGY_KEYS.WORK_ITEMS,
	);

	return (
		<Stack spacing={2}>
			<Typography variant="h6" component="h2">
				{`${count} ${workItemsTerm} in ${refinementTerm}`}
			</Typography>
			<TableContainer>
				<Table size="small">
					<TableHead>
						<TableRow>
							<TableCell>ID</TableCell>
							<TableCell>Name</TableCell>
							<TableCell>State</TableCell>
							<TableCell>Category</TableCell>
							<TableCell>{getTerm(TERMINOLOGY_KEYS.WORK_ITEM_AGE)}</TableCell>
						</TableRow>
					</TableHead>
					<TableBody>
						{refinement.workItems.map((workItem) => (
							<RefinementRow key={workItem.referenceId} workItem={workItem} />
						))}
					</TableBody>
				</Table>
			</TableContainer>
		</Stack>
	);
};

export default RefinementView;
