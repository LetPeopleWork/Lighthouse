import AddIcon from "@mui/icons-material/Add";
import DeleteIcon from "@mui/icons-material/Delete";
import EditIcon from "@mui/icons-material/Edit";
import {
	Alert,
	Box,
	Container,
	Fade,
	IconButton,
	Link as MuiLink,
	Tooltip,
	Typography,
	useTheme,
} from "@mui/material";
import type { GridValidRowModel } from "@mui/x-data-grid";
import type React from "react";
import { useCallback, useContext, useEffect, useMemo, useState } from "react";
import { Link, useLocation, useNavigate } from "react-router-dom";
import { LicenseTooltip } from "../../components/App/License/LicenseToolTip";
import ActionButton from "../../components/Common/ActionButton/ActionButton";
import DataGridBase from "../../components/Common/DataGrid/DataGridBase";
import type { DataGridColumn } from "../../components/Common/DataGrid/types";
import DataOverviewTable from "../../components/Common/DataOverviewTable/DataOverviewTable";
import DeleteConfirmationDialog from "../../components/Common/DeleteConfirmationDialog/DeleteConfirmationDialog";
import FilterBar from "../../components/Common/FilterBar/FilterBar";
import LoadingAnimation from "../../components/Common/LoadingAnimation/LoadingAnimation";
import OnboardingStepper from "../../components/Common/OnboardingStepper/OnboardingStepper";
import { useLicenseRestrictions } from "../../hooks/useLicenseRestrictions";
import type { IFeatureOwner } from "../../models/IFeatureOwner";
import type { Portfolio } from "../../models/Portfolio/Portfolio";
import type { Team } from "../../models/Team/Team";
import { TERMINOLOGY_KEYS } from "../../models/TerminologyKeys";
import type { IWorkTrackingSystemConnection } from "../../models/WorkTracking/WorkTrackingSystemConnection";
import { ApiError } from "../../services/Api/ApiError";
import { ApiServiceContext } from "../../services/Api/ApiServiceContext";
import { useTerminology } from "../../services/TerminologyContext";

const OverviewDashboard: React.FC = () => {
	const [portfolios, setPortfolios] = useState<Portfolio[]>([]);
	const [teams, setTeams] = useState<Team[]>([]);
	const [connections, setConnections] = useState<
		IWorkTrackingSystemConnection[]
	>([]);
	const [isLoading, setIsLoading] = useState(true);
	const [hasError, setHasError] = useState(false);
	const [deleteDialogOpen, setDeleteDialogOpen] = useState<boolean>(false);
	const [selectedItem, setSelectedItem] = useState<Portfolio | Team | null>(
		null,
	);
	const [deleteType, setDeleteType] = useState<"portfolio" | "team" | null>(
		null,
	);
	const [deleteConnectionDialogOpen, setDeleteConnectionDialogOpen] =
		useState(false);
	const [selectedConnection, setSelectedConnection] =
		useState<IWorkTrackingSystemConnection | null>(null);
	const [deleteConnectionError, setDeleteConnectionError] = useState<
		string | null
	>(null);

	const location = useLocation();
	const navigate = useNavigate();
	const theme = useTheme();
	const queryParams = new URLSearchParams(location.search);
	const initialFilterText = queryParams.get("filter") ?? "";
	const [filterText, setFilterText] = useState(initialFilterText);

	const { getTerm } = useTerminology();
	const teamTerm = getTerm(TERMINOLOGY_KEYS.TEAM);
	const portfolioTerm = getTerm(TERMINOLOGY_KEYS.PORTFOLIO);
	const connectionTerm = getTerm(TERMINOLOGY_KEYS.WORK_TRACKING_SYSTEM);
	const connectionsTerm = getTerm(TERMINOLOGY_KEYS.WORK_TRACKING_SYSTEMS);

	const { portfolioService, teamService, workTrackingSystemService } =
		useContext(ApiServiceContext);
	const {
		canCreatePortfolio,
		canCreateTeam,
		maxPortfoliosWithoutPremium,
		maxTeamsWithoutPremium,
	} = useLicenseRestrictions();

	const hasConnections = connections.length > 0;
	const hasTeams = teams.length > 0;

	const fetchData = useCallback(async () => {
		try {
			setIsLoading(true);
			const [portfolioData, teamData, connectionData] = await Promise.all([
				portfolioService.getPortfolios(),
				teamService.getTeams(),
				workTrackingSystemService.getConfiguredWorkTrackingSystems(),
			]);
			setPortfolios(portfolioData);
			setTeams(teamData);
			setConnections(connectionData);
			setIsLoading(false);
		} catch (error) {
			console.error("Error fetching overview data:", error);
			setHasError(true);
		}
	}, [portfolioService, teamService, workTrackingSystemService]);

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

	const closeDeleteDialog = useCallback(() => {
		setDeleteDialogOpen(false);
		setSelectedItem(null);
		setDeleteType(null);
	}, []);

	const handleConfirmDelete = async () => {
		if (!selectedItem || !deleteType) return;

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
		} finally {
			closeDeleteDialog();
		}
	};

	// Connection handlers
	const handleAddConnection = async () => {
		navigate("/connections/new");
	};

	const handleDeleteConnection = useCallback(
		(connection: IWorkTrackingSystemConnection) => {
			setSelectedConnection(connection);
			setDeleteConnectionError(null);
			setDeleteConnectionDialogOpen(true);
		},
		[],
	);

	const closeDeleteConnectionDialog = useCallback(() => {
		setDeleteConnectionDialogOpen(false);
		setSelectedConnection(null);
		setDeleteConnectionError(null);
	}, []);

	const handleConfirmDeleteConnection = async () => {
		if (!selectedConnection?.id) return;

		try {
			await workTrackingSystemService.deleteWorkTrackingSystemConnection(
				selectedConnection.id,
			);
			setConnections((prev) =>
				prev.filter((c) => c.id !== selectedConnection.id),
			);
			closeDeleteConnectionDialog();
		} catch (error) {
			if (error instanceof ApiError && error.code === 409) {
				setDeleteConnectionError(error.message);
			} else {
				setDeleteConnectionError(
					"Failed to delete connection. Please try again.",
				);
			}
		}
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

	const connectionColumns: DataGridColumn<
		IWorkTrackingSystemConnection & GridValidRowModel
	>[] = useMemo(
		() => [
			{
				field: "name",
				headerName: "Name",
				flex: 1,
				hideable: false,
				sortable: true,
				renderCell: ({ row }) => (
					<Link
						to={`/connections/${row.id}/edit`}
						style={{
							textDecoration: "none",
							color: theme.palette.primary.main,
							fontWeight: "bold",
						}}
					>
						{row.name}
					</Link>
				),
			},
			{
				field: "workTrackingSystem",
				headerName: "Type",
				width: 180,
				sortable: true,
			},
			{
				field: "actions",
				headerName: "Actions",
				width: 150,
				sortable: false,
				hideable: false,
				renderCell: ({ row }) => (
					<Box
						sx={{
							display: "flex",
							justifyContent: "flex-end",
							gap: 1,
							width: "100%",
						}}
					>
						<Tooltip title="Edit">
							<IconButton
								component={Link}
								to={`/connections/${row.id}/edit`}
								size="medium"
								sx={{
									color: theme.palette.primary.main,
									transition: "transform 0.2s",
									"&:hover": { transform: "scale(1.1)" },
								}}
								data-testid="edit-connection-button"
							>
								<EditIcon fontSize="medium" />
							</IconButton>
						</Tooltip>
						<Tooltip title="Delete">
							<IconButton
								onClick={() => handleDeleteConnection(row)}
								size="medium"
								sx={{
									color: theme.palette.primary.main,
									transition: "transform 0.2s",
									"&:hover": { transform: "scale(1.1)" },
								}}
								data-testid="delete-connection-button"
							>
								<DeleteIcon fontSize="medium" />
							</IconButton>
						</Tooltip>
					</Box>
				),
			},
		],
		[theme, handleDeleteConnection],
	);

	const filteredConnections = useMemo(
		() =>
			connections.filter((c) =>
				c.name.toLowerCase().includes(filterText.toLowerCase()),
			),
		[connections, filterText],
	);

	const hasPortfolios = portfolios.length > 0;

	return (
		<LoadingAnimation isLoading={isLoading} hasError={hasError}>
			<Container maxWidth={false}>
				<FilterBar
					filterText={filterText}
					onFilterTextChange={handleFilterChange}
				/>

				{/* Onboarding stepper — visible when setup is incomplete */}
				<Box sx={{ mt: 2 }}>
					<OnboardingStepper
						hasConnections={hasConnections}
						connectionTerm={connectionTerm}
						hasTeams={hasTeams}
						hasPortfolios={hasPortfolios}
						canCreateTeam={canCreateTeam}
						canCreatePortfolio={canCreatePortfolio}
						teamTerm={teamTerm}
						portfolioTerm={portfolioTerm}
					/>
				</Box>

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
						<ActionButton
							buttonText={`Add ${connectionTerm}`}
							startIcon={<AddIcon />}
							onClickHandler={handleAddConnection}
							buttonVariant="contained"
						/>
						<Tooltip
							title={
								hasConnections
									? ""
									: `Create a ${connectionTerm} before adding a ${teamTerm}`
							}
						>
							<span>
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
											disabled={!canCreateTeam || !hasConnections}
										/>
									</span>
								</LicenseTooltip>
							</span>
						</Tooltip>
						<Tooltip
							title={hasTeams ? "" : "Create a team before adding a portfolio"}
						>
							<span>
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
											disabled={!canCreatePortfolio || !hasTeams}
										/>
									</span>
								</LicenseTooltip>
							</span>
						</Tooltip>
					</Box>
				</Box>

				{/* Vertical layout: Portfolios → Teams → Connections (bottom) */}
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

					{/* Connections Section — styled consistently with Portfolios/Teams */}
					<Container maxWidth={false} sx={{ pb: 4 }}>
						<Box
							sx={{
								display: "flex",
								justifyContent: "space-between",
								alignItems: "center",
								gap: 2,
								mb: 2,
							}}
						>
							<Typography
								variant="h5"
								sx={{
									fontWeight: 600,
									color: theme.palette.primary.main,
									textTransform: "capitalize",
								}}
							>
								{connectionsTerm}
							</Typography>
						</Box>

						{connections.length === 0 && (
							<Fade in={true} timeout={800}>
								<Alert
									severity="info"
									variant="outlined"
									sx={{
										mb: 3,
										borderRadius: 2,
										boxShadow: theme.shadows[1],
									}}
									data-testid="no-connections-alert"
								>
									<Typography variant="body1">
										No {connectionTerm} found.{" "}
										<MuiLink
											component={Link}
											to="/settings?tab=demodata"
											style={{
												color: theme.palette.primary.main,
												textDecoration: "none",
												fontWeight: 500,
											}}
											sx={{
												"&:hover": {
													textDecoration: "underline",
												},
											}}
										>
											Load Demo Data
										</MuiLink>{" "}
										or{" "}
										<MuiLink
											href="https://docs.lighthouse.letpeople.work"
											target="_blank"
											rel="noopener noreferrer"
											style={{
												color: theme.palette.primary.main,
												textDecoration: "none",
												fontWeight: 500,
											}}
											sx={{
												"&:hover": {
													textDecoration: "underline",
												},
											}}
										>
											Check the documentation
										</MuiLink>{" "}
										for more information.
									</Typography>
								</Alert>
							</Fade>
						)}

						{connections.length > 0 && filteredConnections.length === 0 && (
							<Fade in={true} timeout={500}>
								<Alert
									severity="warning"
									variant="outlined"
									sx={{ mb: 3, borderRadius: 2, boxShadow: theme.shadows[1] }}
								>
									No connections found matching the filter{" "}
									<strong>{filterText}</strong>
								</Alert>
							</Fade>
						)}

						{connections.length > 0 && filteredConnections.length > 0 && (
							<Box data-testid="connections-datagrid-container">
								<DataGridBase
									rows={
										filteredConnections as (IWorkTrackingSystemConnection &
											GridValidRowModel)[]
									}
									columns={connectionColumns}
									storageKey="overview-connection-table"
								/>
							</Box>
						)}
					</Container>
				</Box>

				{selectedItem && (
					<DeleteConfirmationDialog
						open={deleteDialogOpen}
						itemName={selectedItem.name}
						onConfirm={handleConfirmDelete}
						onCancel={closeDeleteDialog}
					/>
				)}

				{selectedConnection && deleteConnectionDialogOpen && (
					<DeleteConfirmationDialog
						open={deleteConnectionDialogOpen}
						itemName={selectedConnection.name}
						onConfirm={handleConfirmDeleteConnection}
						onCancel={closeDeleteConnectionDialog}
						errorMessage={deleteConnectionError ?? undefined}
					/>
				)}
			</Container>
		</LoadingAnimation>
	);
};

export default OverviewDashboard;
