import { Button } from "@mui/material";
import type { GridValidRowModel } from "@mui/x-data-grid";
import type { DataGridColumn } from "../../../../components/Common/DataGrid/types";
import { createNameColumn } from "../../../../components/Common/FeatureListDataGrid/columns";
import ParentWorkItemCell from "../../../../components/Common/ParentWorkItemCell/ParentWorkItemCell";
import type { ParentWorkItem } from "../../../../hooks/useParentWorkItems";
import type { IRefinementRow } from "../../../../models/Refinement/Refinement";
import ReadinessCell from "./ReadinessCell";
import StageCell from "./StageCell";
import type { IPendingVote } from "./useVoteCasting";
import VoteControl from "./VoteControl";
import { describeVoteCount } from "./voteWording";
import YardstickQuestion, {
	type YardstickQuestionWords,
} from "./YardstickQuestion";

export type RefinementGridRow = IRefinementRow & GridValidRowModel;

export interface RefinementColumnsOptions {
	workItemTerm: string;
	parentMap: Map<string, ParentWorkItem>;
	/** The question every vote answers; it heads the vote column. */
	voteQuestion?: YardstickQuestionWords;
	stagesConfigured: boolean;
	votesBeingSent: ReadonlySet<string>;
	onVote: (vote: IPendingVote) => void;
	onOpenVotes: (referenceId: string) => void;
}

const stageColumn: DataGridColumn<RefinementGridRow> = {
	field: "stage",
	headerName: "Stage",
	width: 160,
	renderCell: ({ row }) => (
		<StageCell stage={row.stage} signalsDisagree={row.signalsDisagree} />
	),
};

export const createRefinementColumns = ({
	workItemTerm,
	parentMap,
	voteQuestion,
	stagesConfigured,
	votesBeingSent,
	onVote,
	onOpenVotes,
}: RefinementColumnsOptions): DataGridColumn<RefinementGridRow>[] => [
	createNameColumn<RefinementGridRow>(workItemTerm),
	{
		field: "parentReferenceId",
		headerName: "Parent",
		width: 300,
		sortable: false,
		renderCell: ({ row }) => (
			<ParentWorkItemCell
				parentReference={row.parentReferenceId}
				parentMap={parentMap}
			/>
		),
	},
	{
		field: "state",
		headerName: "State",
		width: 160,
	},
	...(stagesConfigured ? [stageColumn] : []),
	{
		field: "myVote",
		headerName: voteQuestion?.question ?? "Your vote",
		...(voteQuestion && {
			renderHeader: () => <YardstickQuestion {...voteQuestion} />,
		}),
		width: 260,
		minWidth: 220,
		sortable: false,
		renderCell: ({ row }) => (
			<VoteControl
				referenceId={row.referenceId}
				myVote={row.myVote ?? null}
				isSending={votesBeingSent.has(row.referenceId)}
				onVote={(answer) => onVote({ referenceId: row.referenceId, answer })}
			/>
		),
	},
	{
		field: "voteCount",
		headerName: "Votes",
		width: 120,
		renderCell: ({ row }) => {
			const voteCount = describeVoteCount(row.voteCount ?? 0);
			return (
				<Button
					size="small"
					aria-label={`${voteCount} - Votes and comments`}
					onClick={() => onOpenVotes(row.referenceId)}
				>
					{voteCount}
				</Button>
			);
		},
	},
	{
		field: "readiness",
		headerName: stagesConfigured ? "Votes say" : "Readiness",
		width: 200,
		renderCell: ({ row }) => (
			<ReadinessCell
				readiness={row.readiness}
				missingVotes={row.missingVotes}
			/>
		),
	},
];
