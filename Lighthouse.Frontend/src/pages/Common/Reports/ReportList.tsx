import DeleteIcon from "@mui/icons-material/Delete";
import { IconButton, Tooltip } from "@mui/material";
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

interface ReportListProps {
	owner: IReportOwner;
	reports: IReportListItem[];
	/** Given only to somebody who may delete; without it the list offers no delete control. */
	onDelete?: (report: IReportListItem) => void;
}

const deleteColumn = (
	onDelete: (report: IReportListItem) => void,
): DataGridColumn<IReportListItem> => ({
	field: "actions",
	headerName: "Actions",
	width: 100,
	sortable: false,
	hideable: false,
	renderCell: ({ row }) => (
		<Tooltip title="Delete">
			<IconButton
				aria-label={`Delete ${row.name}`}
				color="primary"
				onClick={() => onDelete(row)}
			>
				<DeleteIcon />
			</IconButton>
		</Tooltip>
	),
});

const ReportList: React.FC<Readonly<ReportListProps>> = ({
	owner,
	reports,
	onDelete,
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
		return onDelete === undefined ? shown : [...shown, deleteColumn(onDelete)];
	}, [owner, onDelete]);

	return (
		<DataGridBase
			rows={reports}
			columns={columns}
			storageKey={`reports-${owner.kind}`}
		/>
	);
};

export default ReportList;
