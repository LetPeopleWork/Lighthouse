import DeleteIcon from "@mui/icons-material/Delete";
import EditIcon from "@mui/icons-material/Edit";
import PictureAsPdfIcon from "@mui/icons-material/PictureAsPdf";
import { CircularProgress, IconButton, Tooltip } from "@mui/material";
import type React from "react";
import { useMemo } from "react";
import DataGridBase from "../../../components/Common/DataGrid/DataGridBase";
import type { DataGridColumn } from "../../../components/Common/DataGrid/types";
import StyledLink from "../../../components/Common/StyledLink/StyledLink";
import type {
	IReportListItem,
	IReportOwner,
} from "../../../models/Reports/Report";
import { parseLocalDate } from "../../../utils/date/localDate";
import { reportPathOf } from "./reportOwner";
import { isShownByThisVersion, reportTypeTitleOf } from "./wizard/reportTypes";

/** What somebody who may change the reports can do from a row. */
export interface ReportListEditorActions {
	onEdit: (report: IReportListItem) => void;
	onDelete: (report: IReportListItem) => void;
	/** The report being read before its edit opens, whose Edit waits meanwhile. */
	readingId: number | undefined;
}

/** What every reader can do from a row, and, for somebody who may change the reports, what else. */
export interface ReportListActions {
	onExportPdf: (report: IReportListItem) => void;
	/** The report whose PDF is being prepared, whose icon waits while every other PDF icon is held back. */
	preparingId: number | undefined;
	editor?: ReportListEditorActions;
}

interface ReportListProps {
	owner: IReportOwner;
	reports: IReportListItem[];
	actions: ReportListActions;
}

// Held back only in look while one PDF is prepared: a truly disabled icon would throw a keyboard reader's focus to
// the page.
const HELD_BACK_LOOK = { color: "action.disabled", cursor: "default" };

const ExportPdfButton: React.FC<{
	readonly report: IReportListItem;
	readonly actions: ReportListActions;
}> = ({ report, actions }) => {
	const isHeldBack = actions.preparingId !== undefined;
	return (
		<Tooltip title="Export PDF">
			<IconButton
				aria-label={`Export ${report.name} as PDF`}
				aria-disabled={isHeldBack ? "true" : undefined}
				color="primary"
				onClick={isHeldBack ? undefined : () => actions.onExportPdf(report)}
				sx={isHeldBack ? HELD_BACK_LOOK : undefined}
			>
				{actions.preparingId === report.id ? (
					<CircularProgress size={20} />
				) : (
					<PictureAsPdfIcon />
				)}
			</IconButton>
		</Tooltip>
	);
};

const EditButton: React.FC<{
	readonly report: IReportListItem;
	readonly actions: ReportListEditorActions;
}> = ({ report, actions }) => {
	const isReading = actions.readingId === report.id;
	return (
		<Tooltip title="Edit report">
			<span>
				<IconButton
					aria-label={`Edit ${report.name}`}
					color="primary"
					disabled={actions.readingId !== undefined}
					onClick={() => actions.onEdit(report)}
				>
					{isReading ? <CircularProgress size={20} /> : <EditIcon />}
				</IconButton>
			</span>
		</Tooltip>
	);
};

const DeleteButton: React.FC<{
	readonly report: IReportListItem;
	readonly actions: ReportListEditorActions;
}> = ({ report, actions }) => (
	<Tooltip title="Delete">
		<IconButton
			aria-label={`Delete ${report.name}`}
			color="primary"
			onClick={() => actions.onDelete(report)}
		>
			<DeleteIcon />
		</IconButton>
	</Tooltip>
);

const RowActions: React.FC<{
	readonly report: IReportListItem;
	readonly actions: ReportListActions;
}> = ({ report, actions }) => {
	const { editor } = actions;
	const isShown = isShownByThisVersion(report.templateKey);
	return (
		<>
			{isShown && <ExportPdfButton report={report} actions={actions} />}
			{isShown && editor !== undefined && (
				<EditButton report={report} actions={editor} />
			)}
			{editor !== undefined && (
				<DeleteButton report={report} actions={editor} />
			)}
		</>
	);
};

const actionsColumn = (
	actions: ReportListActions,
): DataGridColumn<IReportListItem> => ({
	field: "actions",
	headerName: "Actions",
	width: actions.editor === undefined ? 100 : 150,
	sortable: false,
	hideable: false,
	renderCell: ({ row }) => <RowActions report={row} actions={actions} />,
});

const ReportList: React.FC<Readonly<ReportListProps>> = ({
	owner,
	reports,
	actions,
}) => {
	const columns: DataGridColumn<IReportListItem>[] = useMemo(() => {
		const shown: DataGridColumn<IReportListItem>[] = [
			{
				field: "name",
				headerName: "Name",
				flex: 1,
				hideable: false,
				renderCell: ({ row }) => (
					<StyledLink to={reportPathOf(owner, row.id)}>{row.name}</StyledLink>
				),
			},
			{
				field: "templateKey",
				headerName: "Type",
				width: 200,
				valueGetter: (_value, row) =>
					reportTypeTitleOf(row.templateKey) ?? "Not supported in this version",
			},
			{
				field: "createdOn",
				headerName: "Created",
				width: 160,
				renderCell: ({ row }) =>
					parseLocalDate(row.createdOn)?.toLocaleDateString(),
			},
		];
		return [...shown, actionsColumn(actions)];
	}, [owner, actions]);

	return (
		<DataGridBase
			rows={reports}
			columns={columns}
			storageKey={`reports-${owner.kind}`}
		/>
	);
};

export default ReportList;
