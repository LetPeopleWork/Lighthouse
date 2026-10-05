import ChatBubbleOutlineIcon from "@mui/icons-material/ChatBubbleOutlineOutlined";
import HelpOutlineIcon from "@mui/icons-material/HelpOutlineOutlined";
import type { SxProps, Theme } from "@mui/material";
import { Box, Button, Tooltip } from "@mui/material";
import type { GridValidRowModel } from "@mui/x-data-grid";
import type React from "react";
import type { DataGridColumn } from "../../../../components/Common/DataGrid/types";
import { createNameColumn } from "../../../../components/Common/FeatureListDataGrid/columns";
import ParentWorkItemCell from "../../../../components/Common/ParentWorkItemCell/ParentWorkItemCell";
import type { ParentWorkItem } from "../../../../hooks/useParentWorkItems";
import type { IRefinementRow } from "../../../../models/Refinement/Refinement";
import { NeededNumber } from "./EnoughForLine";
import ReadinessCell from "./ReadinessCell";
import StageCell from "./StageCell";
import type { IPendingVote } from "./useVoteCasting";
import VoteControl from "./VoteControl";
import { describeVoteCount } from "./voteWording";
import YardstickQuestion, {
	type YardstickQuestionWords,
} from "./YardstickQuestion";

export type RefinementGridRow = IRefinementRow & GridValidRowModel;

// Read by screen readers and text search, never shown; the tooltip says the same to a pointer.
const VISUALLY_HIDDEN: SxProps<Theme> = {
	position: "absolute",
	width: "1px",
	height: "1px",
	overflow: "hidden",
	clip: "rect(0 0 0 0)",
	whiteSpace: "nowrap",
};

const COMMENTS = "Comments";
const OPEN_QUESTION = "Open question";

const Marker: React.FC<
	Readonly<{ label: string; children: React.ReactElement }>
> = ({ label, children }) => (
	<Tooltip title={label}>
		<Box component="span" sx={{ display: "inline-flex", ml: 0.5 }}>
			{children}
			<Box component="span" sx={VISUALLY_HIDDEN}>
				{label}
			</Box>
		</Box>
	</Tooltip>
);

const describeVotesCell = (row: IRefinementRow, voteCount: string): string =>
	[
		voteCount,
		...(row.hasComments ? [COMMENTS.toLowerCase()] : []),
		...(row.hasOpenQuestion ? [OPEN_QUESTION.toLowerCase()] : []),
	].join(", ");

export interface RefinementColumnsOptions {
	workItemTerm: string;
	parentMap: Map<string, ParentWorkItem>;
	/** The question every vote answers; it heads the vote column. */
	voteQuestion: YardstickQuestionWords;
	stagesConfigured: boolean;
	/** Whether a "#" column numbers the rows needed before the next Refinement. */
	numbersNeeded: boolean;
	votesBeingSent: ReadonlySet<string>;
	onVote: (vote: IPendingVote) => void;
	onOpenVotes: (referenceId: string) => void;
}

const neededNumberColumn: DataGridColumn<RefinementGridRow> = {
	field: "neededNumber",
	headerName: "#",
	width: 56,
	sortable: false,
	filterable: false,
	disableColumnMenu: true,
	renderCell: () => <NeededNumber />,
};

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
	numbersNeeded,
	votesBeingSent,
	onVote,
	onOpenVotes,
}: RefinementColumnsOptions): DataGridColumn<RefinementGridRow>[] => [
	...(numbersNeeded ? [neededNumberColumn] : []),
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
		headerName: voteQuestion.question,
		renderHeader: () => <YardstickQuestion {...voteQuestion} />,
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
					aria-label={`${describeVotesCell(row, voteCount)} - Votes and comments`}
					onClick={() => onOpenVotes(row.referenceId)}
				>
					{voteCount}
					{row.hasComments && (
						<Marker label={COMMENTS}>
							<ChatBubbleOutlineIcon fontSize="small" />
						</Marker>
					)}
					{row.hasOpenQuestion && (
						<Marker label={OPEN_QUESTION}>
							<HelpOutlineIcon fontSize="small" color="warning" />
						</Marker>
					)}
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
