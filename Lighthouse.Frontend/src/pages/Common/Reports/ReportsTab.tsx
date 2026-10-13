import AddIcon from "@mui/icons-material/Add";
import { Box, Button, Stack } from "@mui/material";
import { useQuery } from "@tanstack/react-query";
import type React from "react";
import { useContext } from "react";
import { useRbac } from "../../../hooks/useRbac";
import type { IReportOwner } from "../../../models/Reports/Report";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import ReportList from "./ReportList";

interface ReportsTabProps {
	owner: IReportOwner;
}

const ReportsTab: React.FC<Readonly<ReportsTabProps>> = ({ owner }) => {
	const { reportService } = useContext(ApiServiceContext);
	const rbac = useRbac();

	const { data, isLoading } = useQuery({
		queryKey: ["reports", owner.kind, owner.id],
		queryFn: () => reportService.getReports(owner),
	});

	const isEditor =
		!rbac.isLoading &&
		(owner.kind === "team"
			? rbac.isTeamAdmin(owner.id)
			: rbac.isPortfolioAdmin(owner.id));

	return (
		<Stack spacing={2}>
			{isEditor && (
				<Box sx={{ display: "flex", justifyContent: "flex-end" }}>
					<Button variant="contained" startIcon={<AddIcon />}>
						Create Report
					</Button>
				</Box>
			)}
			<ReportList
				owner={owner}
				reports={data?.reports ?? []}
				loading={isLoading}
			/>
		</Stack>
	);
};

export default ReportsTab;
