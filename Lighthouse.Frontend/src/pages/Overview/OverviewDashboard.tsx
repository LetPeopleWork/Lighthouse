import AddIcon from "@mui/icons-material/Add";
import { Box, Container, Tooltip, Typography } from "@mui/material";
import type React from "react";
import { useCallback, useContext, useEffect, useState } from "react";
import { useLocation, useNavigate } from "react-router-dom";
import ActionButton from "../../components/Common/ActionButton/ActionButton";
import DataOverviewTable from "../../components/Common/DataOverviewTable/DataOverviewTable";
import DeleteConfirmationDialog from "../../components/Common/DeleteConfirmationDialog/DeleteConfirmationDialog";
import FilterBar from "../../components/Common/FilterBar/FilterBar";
import LoadingAnimation from "../../components/Common/LoadingAnimation/LoadingAnimation";
import { useLicenseRestrictions } from "../../hooks/useLicenseRestrictions";
import type { IFeatureOwner } from "../../models/IFeatureOwner";
import type { Project } from "../../models/Project/Project";
import type { Team } from "../../models/Team/Team";
import { TERMINOLOGY_KEYS } from "../../models/TerminologyKeys";
import { ApiServiceContext } from "../../services/Api/ApiServiceContext";
import { useTerminology } from "../../services/TerminologyContext";

const OverviewDashboard: React.FC = () => {
	const [projects, setProjects] = useState<Project[]>([]);
	const [teams, setTeams] = useState<Team[]>([]);
	const [isLoading, setIsLoading] = useState(true);
	const [hasError, setHasError] = useState(false);
	const [deleteDialogOpen, setDeleteDialogOpen] = useState<boolean>(false);
	const [selectedItem, setSelectedItem] = useState<Project | Team | null>(null);
	const [deleteType, setDeleteType] = useState<"project" | "team" | null>(null);

	const location = useLocation();
	const navigate = useNavigate();
	const queryParams = new URLSearchParams(location.search);
	const initialFilterText = queryParams.get("filter") ?? "";
	const [filterText, setFilterText] = useState(initialFilterText);

	const { getTerm } = useTerminology();
	const teamTerm = getTerm(TERMINOLOGY_KEYS.TEAM);

	const { projectService, teamService } = useContext(ApiServiceContext);
	const {
		canCreateProject,
		createProjectTooltip,
		canCreateTeam,
		createTeamTooltip,
	} = useLicenseRestrictions();

	const fetchData = useCallback(async () => {
		try {
			setIsLoading(true);
			const [projectData, teamData] = await Promise.all([
				projectService.getProjects(),
				teamService.getTeams(),
			]);
			setProjects(projectData);
			setTeams(teamData);
			setIsLoading(false);
		} catch (error) {
			console.error("Error fetching overview data:", error);
			setHasError(true);
		}
	}, [projectService, teamService]);

	useEffect(() => {
		fetchData();
	}, [fetchData]);

	const handleProjectDelete = (project: IFeatureOwner) => {
		setSelectedItem(project as Project);
		setDeleteType("project");
		setDeleteDialogOpen(true);
	};

	const handleTeamDelete = (team: IFeatureOwner) => {
		setSelectedItem(team as Team);
		setDeleteType("team");
		setDeleteDialogOpen(true);
	};

	const handleDeleteConfirmation = async (confirmed: boolean) => {
		if (confirmed && selectedItem && deleteType) {
			try {
				setIsLoading(true);

				if (deleteType === "project") {
					await projectService.deleteProject(selectedItem.id);
				} else if (deleteType === "team") {
					await teamService.deleteTeam(selectedItem.id);
				}

				await fetchData();
			} catch (error) {
				console.error(`Error deleting ${deleteType}:`, error);
				setHasError(true);
			}
		}

		setDeleteDialogOpen(false);
		setSelectedItem(null);
		setDeleteType(null);
	};

	// Update URL when filter changes
	const handleFilterChange = (newFilterText: string) => {
		setFilterText(newFilterText);
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

	const handleAddProject = async () => {
		navigate("/projects/new");
	};

	const handleAddTeam = async () => {
		navigate("/teams/new");
	};

	return (
		<LoadingAnimation isLoading={isLoading} hasError={hasError}>
			<Container maxWidth={false}>
				<FilterBar
					filterText={filterText}
					onFilterTextChange={handleFilterChange}
				/>

				{/* Header with Add buttons */}
				<Box
					sx={{
						mt: 2,
						mb: 3,
						display: "flex",
						justifyContent: "space-between",
						alignItems: "center",
						flexWrap: "wrap",
						gap: 2,
					}}
				>
					<Typography
						variant="h4"
						component="h1"
						sx={{ fontWeight: 600 }}
					></Typography>
					<Box sx={{ display: "flex", gap: 2 }}>
						<Tooltip title={createProjectTooltip} arrow>
							<span>
								<ActionButton
									buttonText="Add Project"
									startIcon={<AddIcon />}
									onClickHandler={handleAddProject}
									buttonVariant="contained"
									disabled={!canCreateProject}
								/>
							</span>
						</Tooltip>
						<Tooltip title={createTeamTooltip} arrow>
							<span>
								<ActionButton
									buttonText={`Add ${teamTerm}`}
									startIcon={<AddIcon />}
									onClickHandler={handleAddTeam}
									buttonVariant="contained"
									disabled={!canCreateTeam}
								/>
							</span>
						</Tooltip>
					</Box>
				</Box>

				{/* Vertical layout for tables */}
				<Box
					sx={{
						display: "flex",
						flexDirection: "column",
						gap: 4,
					}}
				>
					<DataOverviewTable
						data={projects}
						title="Projects"
						api="projects"
						onDelete={handleProjectDelete}
						filterText={filterText}
					/>
					<DataOverviewTable
						data={teams}
						title={`${teamTerm}s`}
						api="teams"
						onDelete={handleTeamDelete}
						filterText={filterText}
					/>
				</Box>

				{selectedItem && (
					<DeleteConfirmationDialog
						open={deleteDialogOpen}
						itemName={selectedItem.name}
						onClose={handleDeleteConfirmation}
					/>
				)}
			</Container>
		</LoadingAnimation>
	);
};

export default OverviewDashboard;
