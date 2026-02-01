import AddIcon from "@mui/icons-material/Add";
import { Box, Container, Typography } from "@mui/material";
import type React from "react";
import { useCallback, useContext, useEffect, useState } from "react";
import { useLocation, useNavigate } from "react-router-dom";
import { LicenseTooltip } from "../../components/App/License/LicenseToolTip";
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

	const location = useLocation();
	const navigate = useNavigate();
	const queryParams = new URLSearchParams(location.search);
	const initialFilterText = queryParams.get("filter") ?? "";
	const [filterText, setFilterText] = useState(initialFilterText);

	const { getTerm } = useTerminology();
	const teamTerm = getTerm(TERMINOLOGY_KEYS.TEAM);
	const portfolioTerm = getTerm(TERMINOLOGY_KEYS.PORTFOLIO);

	const { portfolioService, teamService } = useContext(ApiServiceContext);
	const {
		canCreatePortfolio,
		canCreateTeam,
		maxPortfoliosWithoutPremium,
		maxTeamsWithoutPremium,
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

	useEffect(() => {
		fetchData();
	}, [fetchData]);

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
						<LicenseTooltip
							canUseFeature={canCreatePortfolio}
							defaultTooltip=""
							premiumExtraInfo={`Free users can only create up to ${maxPortfoliosWithoutPremium} portfolio`}
						>
							<span>
								<ActionButton
									buttonText={`Add ${portfolioTerm}`}
									startIcon={<AddIcon />}
									onClickHandler={handleAddPortfolio}
									buttonVariant="contained"
									disabled={!canCreatePortfolio}
								/>
							</span>
						</LicenseTooltip>
						<LicenseTooltip
							canUseFeature={canCreateTeam}
							defaultTooltip=""
							premiumExtraInfo={`Free users can only create up to ${maxTeamsWithoutPremium} teams`}
						>
							<span>
								<ActionButton
									buttonText={`Add ${teamTerm}`}
									startIcon={<AddIcon />}
									onClickHandler={handleAddTeam}
									buttonVariant="contained"
									disabled={!canCreateTeam}
								/>
							</span>
						</LicenseTooltip>
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
