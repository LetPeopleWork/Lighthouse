import { Paper, TableContainer } from "@mui/material";
import type React from "react";
import { useMemo } from "react";
import DataGridBase from "../../../../components/Common/DataGrid/DataGridBase";
import { useParentWorkItems } from "../../../../hooks/useParentWorkItems";
import type {
	IRefinementRow,
	IYardstick,
	SizingAnswer,
} from "../../../../models/Refinement/Refinement";
import { TERMINOLOGY_KEYS } from "../../../../models/TerminologyKeys";
import { useTerminology } from "../../../../services/TerminologyContext";
import {
	EnoughForContext,
	type EnoughForMarking,
	useEnoughForLine,
} from "./EnoughForLine";
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
	/** Which rows are needed before the next Refinement, or null when there is no number to mark them by. */
	marking: EnoughForMarking | null;
	votesBeingSent: ReadonlySet<string>;
	onVote: (vote: IPendingVote) => void;
	onTakeBack: (referenceId: string, answer: SizingAnswer) => void;
	onOpenVotes: (referenceId: string) => void;
}

/** The Work Items in refinement, one row each, with the vote every reader can cast on it. */
const RefinementGrid: React.FC<Readonly<RefinementGridProps>> = ({
	teamId,
	workItems,
	yardstick,
	stagesConfigured,
	marking,
	votesBeingSent,
	onVote,
	onTakeBack,
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
	const numbersNeeded = marking !== null;
	const columns = useMemo(
		() =>
			createRefinementColumns({
				workItemTerm,
				parentMap,
				voteQuestion: { question, tooltip },
				stagesConfigured,
				numbersNeeded,
				votesBeingSent,
				onVote,
				onTakeBack,
				onOpenVotes,
			}),
		[
			workItemTerm,
			parentMap,
			question,
			tooltip,
			stagesConfigured,
			numbersNeeded,
			votesBeingSent,
			onVote,
			onTakeBack,
			onOpenVotes,
		],
	);
	const { slots, getRowSpacing } = useEnoughForLine(marking);

	return (
		<EnoughForContext.Provider value={marking}>
			<TableContainer component={Paper}>
				<DataGridBase<RefinementGridRow>
					rows={workItems as RefinementGridRow[]}
					columns={columns}
					idField="referenceId"
					storageKey={`team-refinement-${teamId}`}
					slots={slots}
					getRowSpacing={getRowSpacing}
				/>
			</TableContainer>
		</EnoughForContext.Provider>
	);
};

export default RefinementGrid;
