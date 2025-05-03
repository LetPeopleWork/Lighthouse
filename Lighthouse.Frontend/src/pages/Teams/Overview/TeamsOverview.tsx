import type React from "react";
import { useCallback, useContext, useEffect, useState } from "react";
import { useLocation, useNavigate } from "react-router-dom";
import DataOverviewTable from "../../../components/Common/DataOverviewTable/DataOverviewTable";
import DeleteConfirmationDialog from "../../../components/Common/DeleteConfirmationDialog/DeleteConfirmationDialog";
import LoadingAnimation from "../../../components/Common/LoadingAnimation/LoadingAnimation";
import type { IFeatureOwner } from "../../../models/IFeatureOwner";
import type { Team } from "../../../models/Team/Team";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";

const TeamsOverview: React.FC = () => {
	const [teams, setTeams] = useState<Team[]>([]);
	const [isLoading, setIsLoading] = useState<boolean>(false);
	const [hasError, setHasError] = useState<boolean>(false);
	const [deleteDialogOpen, setDeleteDialogOpen] = useState<boolean>(false);
	const [selectedTeam, setSelectedTeam] = useState<Team | null>(null);

	const location = useLocation();
	const navigate = useNavigate();
	const queryParams = new URLSearchParams(location.search);
	const initialFilterText = queryParams.get("filter") ?? "";

	const { teamService } = useContext(ApiServiceContext);

	const fetchData = useCallback(async () => {
		try {
			setIsLoading(true);
			const teamData = await teamService.getTeams();
			setTeams(teamData);
			setIsLoading(false);
		} catch (error) {
			console.error("Error fetching team data:", error);
			setHasError(true);
		}
	}, [teamService]);

	useEffect(() => {
		fetchData();
	}, [fetchData]);

	const handleDelete = (team: IFeatureOwner) => {
		setSelectedTeam(team as Team);
		setDeleteDialogOpen(true);
	};

	const handleDeleteConfirmation = async (confirmed: boolean) => {
		if (confirmed && selectedTeam) {
			try {
				setIsLoading(true);

				await teamService.deleteTeam(selectedTeam.id);
				await fetchData();
			} catch (error) {
				console.error("Error deleting team:", error);
				setHasError(true);
			}
		}

		setDeleteDialogOpen(false);
		setSelectedTeam(null);
	};

	const handleFilterChange = (newFilterText: string) => {
		const params = new URLSearchParams(location.search);

		if (newFilterText) {
			params.set("filter", newFilterText);
		} else {
			params.delete("filter");
		}

		navigate(
			{
				pathname: location.pathname,
				search: params.toString(),
			},
			{ replace: true },
		);
	};

	return (
		<LoadingAnimation hasError={hasError} isLoading={isLoading}>
			<DataOverviewTable
				data={teams}
				api="teams"
				onDelete={handleDelete}
				initialFilterText={initialFilterText}
				onFilterChange={handleFilterChange}
			/>

			{selectedTeam && (
				<DeleteConfirmationDialog
					open={deleteDialogOpen}
					itemName={selectedTeam.name}
					onClose={handleDeleteConfirmation}
				/>
			)}
		</LoadingAnimation>
	);
};

export default TeamsOverview;
