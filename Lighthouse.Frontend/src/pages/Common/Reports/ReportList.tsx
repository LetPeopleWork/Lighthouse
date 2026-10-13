import type React from "react";
import { useMemo } from "react";
import { Link } from "react-router";
import DataGridBase from "../../../components/Common/DataGrid/DataGridBase";
import type { DataGridColumn } from "../../../components/Common/DataGrid/types";
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
}

const ReportList: React.FC<Readonly<ReportListProps>> = ({
	owner,
	reports,
}) => {
	const columns: DataGridColumn<IReportListItem>[] = useMemo(
		() => [
			{
				field: "name",
				headerName: "Name",
				flex: 1,
				hideable: false,
				renderCell: ({ row }) => (
					<Link to={reportPathOf(owner, row.id)}>{row.name}</Link>
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
		],
		[owner],
	);

	return (
		<DataGridBase
			rows={reports}
			columns={columns}
			storageKey={`reports-${owner.kind}`}
		/>
	);
};

export default ReportList;
