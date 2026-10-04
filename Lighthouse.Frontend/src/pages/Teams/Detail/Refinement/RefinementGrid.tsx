import { Paper, TableContainer } from "@mui/material";
import type React from "react";
import { useMemo } from "react";
import DataGridBase from "../../../../components/Common/DataGrid/DataGridBase";
import { useParentWorkItems } from "../../../../hooks/useParentWorkItems";
import type {
	IRefinementRow,
	IYardstick,
} from "../../../../models/Refinement/Refinement";
import { TERMINOLOGY_KEYS } from "../../../../models/TerminologyKeys";
import { useTerminology } from "../../../../services/TerminologyContext";
import {
	createRefinementColumns,
	type RefinementGridRow,
} from "./refinementColumns";
import type { IPendingVote } from "./useVoteCasting";
import { askYardstick } from "./YardstickQuestion";

interface RefinementGridProps {
	teamId: number;
	workItems: IRefinementRow[];
	yardstick: IYardstick;
	stagesConfigured: boolean;
	votesBeingSent: ReadonlySet<string>;
	onVote: (vote: IPendingVote) => void;
	onOpenVotes: (referenceId: string) => void;
}

/** The Work Items in refinement, one row each, with the vote every reader can cast on it. */
const RefinementGrid: React.FC<RefinementGridProps> = ({
	teamId,
	workItems,
	yardstick,
	stagesConfigured,
	votesBeingSent,
	onVote,
	onOpenVotes,
}) => {
	const { getTerm } = useTerminology();
	const parentReferences = useMemo(
		() =>
			workItems.map((workItem) => ({
				parentWorkItemReference: workItem.parentReferenceId,
			})),
		[workItems],
	);
	const parentMap = useParentWorkItems(parentReferences);
	const workItemTerm = getTerm(TERMINOLOGY_KEYS.WORK_ITEM);
	const { question, tooltip } = askYardstick(yardstick, getTerm);
	const columns = useMemo(
		() =>
			createRefinementColumns({
				workItemTerm,
				parentMap,
				voteQuestion: { question, tooltip },
				stagesConfigured,
				votesBeingSent,
				onVote,
				onOpenVotes,
			}),
		[
			workItemTerm,
			parentMap,
			question,
			tooltip,
			stagesConfigured,
			votesBeingSent,
			onVote,
			onOpenVotes,
		],
	);

	return (
		<TableContainer component={Paper}>
			<DataGridBase<RefinementGridRow>
				rows={workItems as RefinementGridRow[]}
				columns={columns}
				idField="referenceId"
				storageKey={`team-refinement-${teamId}`}
			/>
		</TableContainer>
	);
};

export default RefinementGrid;
