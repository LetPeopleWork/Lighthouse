import { Chip } from "@mui/material";
import type React from "react";
import { Link } from "react-router";
import type {
	IReportListItem,
	IReportOwner,
} from "../../../models/Reports/Report";
import {
	reportPathOf,
	reportsPathOf,
} from "../../../pages/Common/Reports/reportOwner";
import { useOwnerReports } from "../../../pages/Common/Reports/useOwnerReports";
import { OverviewCellChips, overviewCellStatusOf } from "./OverviewCellChips";

export interface ReportsChipsProps {
	readonly owner: IReportOwner;
}

const byName = (left: IReportListItem, right: IReportListItem) =>
	left.name.localeCompare(right.name);

export const ReportsChips: React.FC<ReportsChipsProps> = ({ owner }) => {
	const reports = useOwnerReports(owner);

	return (
		<OverviewCellChips
			status={overviewCellStatusOf(reports)}
			items={reports.data && [...reports.data.reports].sort(byName)}
			renderItem={(report) => (
				<Link
					key={report.id}
					to={reportPathOf(owner, report.id)}
					style={{ textDecoration: "none" }}
				>
					<Chip
						label={report.name}
						size="small"
						variant="outlined"
						sx={{ cursor: "pointer" }}
					/>
				</Link>
			)}
			moreTo={reportsPathOf(owner)}
			emptyText="No Reports"
			failureText="Couldn't load Reports"
			loadingLabel="Loading Reports"
		/>
	);
};
