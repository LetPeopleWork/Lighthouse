import AddIcon from "@mui/icons-material/Add";
import UpdateIcon from "@mui/icons-material/Update";
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
import type { Portfolio } from "../../models/Portfolio/Portfolio";
import type { Team } from "../../models/Team/Team";
import { TERMINOLOGY_KEYS } from "../../models/TerminologyKeys";
import { ApiServiceContext } from "../../services/Api/ApiServiceContext";
import { useTerminology } from "../../services/TerminologyContext";
import type { IUpdateStatus } from "../../services/UpdateSubscriptionService";

const OverviewDashboard: React.FC = () => {
	const [portfolios, setPortfolios] = useState<Portfolio[]>([]);
	const [teams, setTeams] = useState<Team[]>([]);
	const [isLoading, setIsLoading] = useState(true);
	const [hasError, setHasError] = useState(false);
	const [deleteDialogOpen, setDeleteDialogOpen] = useState<boolean>(false);
	const [selectedItem, setSelectedItem] = useState<Portfolio | Team | null>(
		null,
	);
	const [deleteType, setDeleteType] = useState<"portfolio" | "team" | null>(
		null,
	);

	const [globalUpdateStatus, setGlobalUpdateStatus] = useState<{
		hasActiveUpdates: boolean;
		activeCount: number;
	}>({ hasActiveUpdates: false, activeCount: 0 });

	const location = useLocation();
	const navigate = useNavigate();
	const queryParams = new URLSearchParams(location.search);
	const initialFilterText = queryParams.get("filter") ?? "";
	const [filterText, setFilterText] = useState(initialFilterText);

	const { getTerm } = useTerminology();
	const teamTerm = getTerm(TERMINOLOGY_KEYS.TEAM);
	const portfolioTerm = getTerm(TERMINOLOGY_KEYS.PORTFOLIO);

	const { portfolioService, teamService, updateSubscriptionService } =
		useContext(ApiServiceContext);
	const {
		canCreatePortfolio,
		createPortfolioTooltip,
		canCreateTeam,
		createTeamTooltip,
		canUpdateAllTeamsAndPortfolios,
		updateAllTeamsAndPortfoliosTooltip,
	} = useLicenseRestrictions();

	const fetchData = useCallback(async () => {
		try {
			setIsLoading(true);
			const [portfolioData, teamData] = await Promise.all([
				portfolioService.getPortfolios(),
				teamService.getTeams(),
			]);
			setPortfolios(portfolioData);
			setTeams(teamData);
			setIsLoading(false);
		} catch (error) {
			console.error("Error fetching overview data:", error);
			setHasError(true);
		}
	}, [portfolioService, teamService]);

	const fetchGlobalUpdateStatus = useCallback(async () => {
		try {
			const status = await updateSubscriptionService.getGlobalUpdateStatus();
			setGlobalUpdateStatus(status);
		} catch (error) {
			console.error("Error fetching global update status:", error);
		}
	}, [updateSubscriptionService]);

	useEffect(() => {
		fetchData();
	}, [fetchData]);

	useEffect(() => {
		fetchGlobalUpdateStatus();

		for (const team of teams) {
			updateSubscriptionService.subscribeToTeamUpdates(
				team.id,
				(status: IUpdateStatus) => {
					if (status.status === "Completed" || status.status === "Failed") {
						fetchGlobalUpdateStatus();
					}
				},
			);
		}

		for (const portfolio of portfolios) {
			updateSubscriptionService.subscribeToFeatureUpdates(
				portfolio.id,
				(status: IUpdateStatus) => {
					if (status.status === "Completed" || status.status === "Failed") {
						fetchGlobalUpdateStatus();
					}
				},
			);
		}

		return () => {
			// Cleanup subscriptions
			teams.forEach((team) => {
				updateSubscriptionService.unsubscribeFromTeamUpdates(team.id);
			});
			portfolios.forEach((portfolio) => {
				updateSubscriptionService.unsubscribeFromFeatureUpdates(portfolio.id);
			});
		};
	}, [teams, portfolios, updateSubscriptionService, fetchGlobalUpdateStatus]);

	const handlePortfolioDelete = (portfolio: IFeatureOwner) => {
		setSelectedItem(portfolio as Portfolio);
		setDeleteType("portfolio");
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

				if (deleteType === "portfolio") {
					await portfolioService.deletePortfolio(selectedItem.id);
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

	const handleAddPortfolio = async () => {
		navigate("/portfolios/new");
	};

	const handleAddTeam = async () => {
		navigate("/teams/new");
	};

	const handleUpdateAll = async () => {
		try {
			setGlobalUpdateStatus({
				hasActiveUpdates: true,
				activeCount: teams.length + portfolios.length,
			});
			await teamService.updateAllTeamData();
			await portfolioService.refreshFeaturesForAllPortfolios();
		} catch (error) {
			console.error("Error updating all teams and portfolios:", error);
			setHasError(true);
		}
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
						<Tooltip title={createPortfolioTooltip} arrow>
							<span>
								<ActionButton
									buttonText={`Add ${portfolioTerm}`}
									startIcon={<AddIcon />}
									onClickHandler={handleAddPortfolio}
									buttonVariant="contained"
									disabled={!canCreatePortfolio}
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
						<Tooltip title={updateAllTeamsAndPortfoliosTooltip} arrow>
							<span>
								<ActionButton
									buttonText={
										globalUpdateStatus.activeCount > 0
											? `Update All (${globalUpdateStatus?.activeCount})`
											: "Update All"
									}
									startIcon={<UpdateIcon />}
									onClickHandler={handleUpdateAll}
									buttonVariant="outlined"
									disabled={
										!canUpdateAllTeamsAndPortfolios ||
										globalUpdateStatus.hasActiveUpdates
									}
									externalIsWaiting={globalUpdateStatus.hasActiveUpdates}
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
						data={portfolios}
						title={`${portfolioTerm}s`}
						api="portfolios"
						onDelete={handlePortfolioDelete}
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
