import CloudSyncIcon from "@mui/icons-material/CloudSync";
import {
	Alert,
	CircularProgress,
	Container,
	IconButton,
	Stack,
	Tab,
	Tabs,
	Tooltip,
} from "@mui/material";
import Grid from "@mui/material/Grid";
import type React from "react";
import { useCallback, useContext, useEffect, useRef, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { LicenseTooltip } from "../../../components/App/License/LicenseToolTip";
import ScopedGroupMappingManager from "../../../components/Common/Authorization/ScopedGroupMappingManager";
import ScopedMembershipManager from "../../../components/Common/Authorization/ScopedMembershipManager";
import DetailHeader from "../../../components/Common/DetailHeader/DetailHeader";
import FeatureOwnerHeader from "../../../components/Common/FeatureOwnerHeader/FeatureOwnerHeader";
import LoadingAnimation from "../../../components/Common/LoadingAnimation/LoadingAnimation";
import FeatureWipQuickSetting from "../../../components/Common/QuickSettings/FeatureWipQuickSetting";
import SleQuickSetting from "../../../components/Common/QuickSettings/SleQuickSetting";
import SystemWipQuickSetting from "../../../components/Common/QuickSettings/SystemWipQuickSetting";
import ThroughputQuickSetting from "../../../components/Common/QuickSettings/ThroughputQuickSetting";
import QuickSettingsBar from "../../../components/Common/QuickSettingsBar/QuickSettingsBar";
import SnackbarErrorHandler from "../../../components/Common/SnackbarErrorHandler/SnackbarErrorHandler";
import ModifyTeamSettings from "../../../components/Common/Team/ModifyTeamSettings";
import { useLicenseRestrictions } from "../../../hooks/useLicenseRestrictions";
import { useRbac } from "../../../hooks/useRbac";
import type {
	RbacScopedMemberSummary,
	ScopedRbacRole,
} from "../../../models/Authorization/RbacModels";
import type { Team } from "../../../models/Team/Team";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import { useTerminology } from "../../../services/TerminologyContext";
import type { IUpdateStatus } from "../../../services/UpdateSubscriptionService";
import TeamFeaturesView from "./TeamFeaturesView";
import TeamForecastView from "./TeamForecastView";
import TeamMetricsView from "./TeamMetricsView";

type TeamViewType =
	| "features"
	| "forecasts"
	| "metrics"
	| "settings"
	| "access";

const TeamDetail: React.FC = () => {
	const navigate = useNavigate();
	const { id, tab } = useParams<{ id: string; tab?: string }>();
	const teamId = Number(id);

	const { getTerm } = useTerminology();
	const teamTerm = getTerm(TERMINOLOGY_KEYS.TEAM);
	const featuresTerm = getTerm(TERMINOLOGY_KEYS.FEATURES);
	const portfolioTerm = getTerm(TERMINOLOGY_KEYS.PORTFOLIO);

	const { canUpdateTeamData, maxTeamsWithoutPremium } =
		useLicenseRestrictions();

	let subscribedToUpdates = false;

	const getInitialView = (
		tabParam: string | undefined,
		teamData: Team | undefined,
	): TeamViewType => {
		if (tabParam === "metrics") {
			return "metrics";
		}

		if (tabParam === "forecasts") {
			return "forecasts";
		}

		if (tabParam === "settings") {
			return "settings";
		}

		if (tabParam === "access") {
			return "access";
		}

		// If features tab is requested but team has no features, redirect to forecasts
		if (tabParam === "features" && teamData?.features.length === 0) {
			return "forecasts";
		}

		return "features";
	};

	const [team, setTeam] = useState<Team>();
	const [hasNoAccess, setHasNoAccess] = useState(false);
	const [isLoading, setIsLoading] = useState<boolean>(true);
	const [isTeamUpdating, setIsTeamUpdating] = useState<boolean>(false);
	const [activeView, setActiveView] = useState<TeamViewType>(
		getInitialView(tab, undefined),
	);
	const [pendingTeamRefresh, setPendingTeamRefresh] = useState(false);
	const [teamMembers, setTeamMembers] = useState<RbacScopedMemberSummary[]>([]);
	const [teamMembersLoading, setTeamMembersLoading] = useState(false);
	const [teamMembersError, setTeamMembersError] = useState<string>();

	// Always reflect the latest activeView inside async subscription callbacks
	const activeViewRef = useRef(activeView);
	activeViewRef.current = activeView;

	const {
		teamService,
		updateSubscriptionService,
		workTrackingSystemService,
		rbacService,
	} = useContext(ApiServiceContext);

	const rbac = useRbac();

	const showSettingsTab = !team || rbac.isTeamAdmin(team.id);
	const showAccessTab = !team || rbac.isTeamAdmin(team.id);

	const loadTeamMembers = useCallback(
		async (targetTeamId: number) => {
			setTeamMembersError(undefined);
			setTeamMembersLoading(true);
			try {
				const members = await rbacService.getTeamMembers(targetTeamId);
				setTeamMembers(members);
			} catch {
				setTeamMembersError("Failed to load team members.");
			} finally {
				setTeamMembersLoading(false);
			}
		},
		[rbacService],
	);

	const handleAssignTeamRole = useCallback(
		async (userProfileId: number, role: ScopedRbacRole) => {
			if (!team) {
				return;
			}

			setTeamMembersError(undefined);
			try {
				await rbacService.upsertTeamMember(team.id, userProfileId, role);
				await loadTeamMembers(team.id);
			} catch {
				setTeamMembersError("Failed to update team member role.");
			}
		},
		[rbacService, team, loadTeamMembers],
	);

	const handleRemoveTeamMember = useCallback(
		async (userProfileId: number) => {
			if (!team) {
				return;
			}

			setTeamMembersError(undefined);
			try {
				await rbacService.removeTeamMember(team.id, userProfileId);
				await loadTeamMembers(team.id);
			} catch {
				setTeamMembersError("Failed to remove team member.");
			}
		},
		[rbacService, team, loadTeamMembers],
	);

	const handleCreateTeamGroupMapping = useCallback(
		async (groupValue: string, role: ScopedRbacRole) => {
			if (!team) {
				return;
			}

			await rbacService.createGroupMapping({
				groupValue,
				role,
				scopeType: "Team",
				scopeId: team.id,
			});
		},
		[rbacService, team],
	);

	const handleRemoveTeamGroupMapping = useCallback(
		async (mappingId: number) => {
			if (!team) {
				return;
			}

			await rbacService.removeGroupMapping(mappingId);
		},
		[rbacService, team],
	);

	const fetchTeam = useCallback(async () => {
		setHasNoAccess(false);

		try {
			const teamData = await teamService.getTeam(teamId);

			if (teamData) {
				setTeam(teamData);
			} else {
				setTeam(undefined);
				setHasNoAccess(true);
			}
		} catch {
			setTeam(undefined);
			setHasNoAccess(true);
		}

		setIsLoading(false);
	}, [teamService, teamId]);

	const updateTeamSettings = useCallback(
		async (
			updateFn: (
				settings: ReturnType<
					typeof teamService.getTeamSettings
				> extends Promise<infer T>
					? T
					: never,
			) => void,
			shouldUpdatePortfolioForecasts = false,
		) => {
			if (!team) return;

			const settings = await teamService.getTeamSettings(team.id);
			updateFn(settings);
			await teamService.updateTeam(settings);
			await fetchTeam();

			if (shouldUpdatePortfolioForecasts) {
				await teamService.updateForecastsForTeamPortfolios(team.id);
			}
		},
		[team, teamService, fetchTeam],
	);

	const onUpdateTeamData = async () => {
		if (!team) {
			return;
		}

		setIsTeamUpdating(true);
		await teamService.updateTeamData(team.id);
	};

	useEffect(() => {
		const setUpTeamUpdateSubscription = async () => {
			const handleTeamUpdate = async (update: IUpdateStatus) => {
				if (update.status === "Completed") {
					setIsTeamUpdating(false);
					if (activeViewRef.current === "settings") {
						// Defer the reload until the user leaves the settings tab
						setPendingTeamRefresh(true);
					} else {
						await fetchTeam();
					}
				} else {
					// Team Update is in progress - update Button
					updateTeamRefreshButton(update);
				}
			};

			const updateTeamRefreshButton = (update: IUpdateStatus | null) => {
				if (update) {
					const isUpdating =
						update.status === "Queued" || update.status === "InProgress";
					setIsTeamUpdating(isUpdating);
				}
			};

			await updateSubscriptionService.subscribeToTeamUpdates(
				teamId,
				handleTeamUpdate,
			);

			const updateStatus = await updateSubscriptionService.getUpdateStatus(
				"Team",
				teamId,
			);
			updateTeamRefreshButton(updateStatus);
		};

		if (team && !subscribedToUpdates) {
			subscribedToUpdates = true;
			setUpTeamUpdateSubscription();
		} else {
			fetchTeam();
		}

		return () => {
			updateSubscriptionService.unsubscribeFromTeamUpdates(teamId);
		};
	}, [team, subscribedToUpdates, updateSubscriptionService, teamId, fetchTeam]);

	// Redirect to forecasts if on features tab but team has no features
	useEffect(() => {
		if (team && activeView === "features" && team.features.length === 0) {
			// Redirect to forecasts when features tab is requested but team has no features
			const newView = "forecasts";
			setActiveView(newView);
			navigate(`/teams/${id}/${newView}`, { replace: true });
		}
	}, [team, activeView, id, navigate]);

	// Flush any pending background refresh as soon as the user leaves the settings tab
	useEffect(() => {
		if (pendingTeamRefresh && activeView !== "settings") {
			setPendingTeamRefresh(false);
			void fetchTeam();
		}
	}, [activeView, pendingTeamRefresh, fetchTeam]);

	useEffect(() => {
		if (activeView === "access" && team) {
			void loadTeamMembers(team.id);
		}
	}, [activeView, team, loadTeamMembers]);

	const handleViewChange = (
		_event: React.SyntheticEvent,
		newView: TeamViewType,
	) => {
		setActiveView(newView);
		navigate(`/teams/${id}/${newView}`, { replace: true });
	};

	return (
		<SnackbarErrorHandler>
			<LoadingAnimation hasError={false} isLoading={isLoading}>
				<Container maxWidth={false}>
					{hasNoAccess && (
						<Alert
							severity="info"
							sx={{ mb: 2 }}
							data-testid="team-no-access-alert"
						>
							This team is unavailable or you no longer have access. Contact a
							System Admin if you need access restored.
						</Alert>
					)}

					{team && (
						<Grid container spacing={3}>
							<Grid size={{ xs: 12 }}>
								<DetailHeader
									leftContent={
										<Stack spacing={1} direction="row">
											<FeatureOwnerHeader featureOwner={team} />
										</Stack>
									}
									quickSettingsContent={
										<QuickSettingsBar>
											<ThroughputQuickSetting
												useFixedDates={team.useFixedDatesForThroughput}
												startDate={team.throughputStartDate}
												endDate={team.throughputEndDate}
												hasBlackoutOverlap={team.hasThroughputBlackoutOverlap}
												onSave={async (
													useFixedDates,
													throughputHistory,
													startDate,
													endDate,
												) => {
													await updateTeamSettings((settings) => {
														settings.useFixedDatesForThroughput = useFixedDates;
														settings.throughputHistory = throughputHistory;
														if (startDate) {
															settings.throughputHistoryStartDate = startDate;
														}
														if (endDate) {
															settings.throughputHistoryEndDate = endDate;
														}
													}, true);
												}}
												disabled={!canUpdateTeamData}
											/>
											<SleQuickSetting
												probability={team.serviceLevelExpectationProbability}
												range={team.serviceLevelExpectationRange}
												onSave={async (probability, range) => {
													await updateTeamSettings((settings) => {
														settings.serviceLevelExpectationProbability =
															probability;
														settings.serviceLevelExpectationRange = range;
													});
												}}
												disabled={!canUpdateTeamData}
											/>
											<SystemWipQuickSetting
												wipLimit={team.systemWIPLimit}
												onSave={async (systemWip) => {
													await updateTeamSettings((settings) => {
														settings.systemWIPLimit = systemWip;
													});
												}}
												disabled={!canUpdateTeamData}
											/>
											<FeatureWipQuickSetting
												featureWip={team.featureWip}
												onSave={async (featureWip) => {
													await updateTeamSettings((settings) => {
														settings.featureWIP = featureWip;
													}, true);
												}}
												disabled={!canUpdateTeamData}
											/>
										</QuickSettingsBar>
									}
									centerContent={
										<Tabs
											value={activeView}
											onChange={handleViewChange}
											aria-label="team view tabs"
										>
											<Tab
												label={
													<Tooltip
														title={
															team.features.length === 0
																? `Add ${teamTerm} to a ${portfolioTerm} to see all ${featuresTerm} associated with the ${teamTerm}.`
																: ""
														}
														arrow
													>
														<span style={{ pointerEvents: "auto" }}>
															{featuresTerm}
														</span>
													</Tooltip>
												}
												value="features"
												disabled={team.features.length === 0}
												aria-label={featuresTerm}
											/>
											<Tab label="Forecasts" value="forecasts" />
											<Tab label="Metrics" value="metrics" />
											{showSettingsTab && (
												<Tab label="Settings" value="settings" />
											)}
											{showAccessTab && <Tab label="Access" value="access" />}
										</Tabs>
									}
									rightContent={
										<LicenseTooltip
											canUseFeature={canUpdateTeamData}
											defaultTooltip={`Update ${teamTerm} Data`}
											premiumExtraInfo={`Free users can only update team data for up to ${maxTeamsWithoutPremium} teams.`}
										>
											<span>
												<IconButton
													aria-label={`Update ${teamTerm} Data`}
													onClick={onUpdateTeamData}
													disabled={!canUpdateTeamData || isTeamUpdating}
													color="primary"
												>
													{isTeamUpdating ? (
														<CircularProgress size={24} />
													) : (
														<CloudSyncIcon />
													)}
												</IconButton>
											</span>
										</LicenseTooltip>
									}
								/>
							</Grid>

							<Grid size={{ xs: 12 }}>
								{activeView === "features" && team && (
									<TeamFeaturesView team={team} />
								)}

								{activeView === "forecasts" && team && (
									<TeamForecastView team={team} />
								)}

								{activeView === "metrics" && team && (
									<TeamMetricsView team={team} />
								)}

								{activeView === "settings" && team && (
									<ModifyTeamSettings
										title={`Edit ${teamTerm}`}
										getWorkTrackingSystems={() =>
											workTrackingSystemService.getConfiguredWorkTrackingSystems()
										}
										getTeamSettings={() => teamService.getTeamSettings(team.id)}
										saveTeamSettings={async (settings) => {
											await teamService.updateTeam(settings);
										}}
										validateTeamSettings={(settings) =>
											teamService.validateTeamSettings(settings)
										}
										disableSave={!canUpdateTeamData}
										saveTooltip={`Free users can only update team settings for up to ${maxTeamsWithoutPremium} teams`}
									/>
								)}

								{activeView === "access" && team && (
									<Stack spacing={2}>
										<ScopedMembershipManager
											title="Team Access"
											members={teamMembers}
											allowedRoles={["TeamAdmin", "Viewer"]}
											loading={teamMembersLoading}
											error={teamMembersError}
											onAssignRole={handleAssignTeamRole}
											onRemoveRole={handleRemoveTeamMember}
										/>
										<ScopedGroupMappingManager
											title="Team Group Access"
											allowedRoles={["TeamAdmin", "Viewer"]}
											groupMappingsFetcher={() =>
												rbacService.getTeamGroupMappings(team.id)
											}
											onCreateMapping={handleCreateTeamGroupMapping}
											onRemoveMapping={handleRemoveTeamGroupMapping}
										/>
									</Stack>
								)}
							</Grid>
						</Grid>
					)}
				</Container>
			</LoadingAnimation>
		</SnackbarErrorHandler>
	);
};

export default TeamDetail;
