import DeleteIcon from "@mui/icons-material/Delete";
import EditIcon from "@mui/icons-material/Edit";
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
import { reportTypeTitleOf } from "./wizard/reportTypes";

/** What somebody who may change the reports can do from a row; without it the list offers no actions. */
export interface ReportListActions {
	onEdit: (report: IReportListItem) => void;
	onDelete: (report: IReportListItem) => void;
	/** The report being read before its edit opens, whose Edit waits meanwhile. */
	readingId: number | undefined;
}

interface ReportListProps {
	owner: IReportOwner;
	reports: IReportListItem[];
	actions?: ReportListActions;
}

const isShownByThisVersion = (report: IReportListItem): boolean =>
	reportTypeTitleOf(report.templateKey) !== undefined;

const EditButton: React.FC<{
	readonly report: IReportListItem;
	readonly actions: ReportListActions;
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

const actionsColumn = (
	actions: ReportListActions,
): DataGridColumn<IReportListItem> => ({
	field: "actions",
	headerName: "Actions",
	width: 120,
	sortable: false,
	hideable: false,
	renderCell: ({ row }) => (
		<>
			{isShownByThisVersion(row) && (
				<EditButton report={row} actions={actions} />
			)}
			<Tooltip title="Delete">
				<IconButton
					aria-label={`Delete ${row.name}`}
					color="primary"
					onClick={() => actions.onDelete(row)}
				>
					<DeleteIcon />
				</IconButton>
			</Tooltip>
		</>
	),
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
		return actions === undefined ? shown : [...shown, actionsColumn(actions)];
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
