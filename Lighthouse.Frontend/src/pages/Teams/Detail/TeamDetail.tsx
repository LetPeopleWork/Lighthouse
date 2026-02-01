import CloudSyncIcon from "@mui/icons-material/CloudSync";
import {
	CircularProgress,
	Container,
	IconButton,
	Stack,
	Tab,
	Tabs,
} from "@mui/material";
import Grid from "@mui/material/Grid";
import type React from "react";
import { useCallback, useContext, useEffect, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { LicenseTooltip } from "../../../components/App/License/LicenseToolTip";
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
import type { Team } from "../../../models/Team/Team";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import { useTerminology } from "../../../services/TerminologyContext";
import type { IUpdateStatus } from "../../../services/UpdateSubscriptionService";
import TeamFeaturesView from "./TeamFeaturesView";
import TeamForecastView from "./TeamForecastView";
import TeamMetricsView from "./TeamMetricsView";

type TeamViewType = "features" | "forecasts" | "metrics" | "settings";

const TeamDetail: React.FC = () => {
	const navigate = useNavigate();
	const { id, tab } = useParams<{ id: string; tab?: string }>();
	const teamId = Number(id);

	const { getTerm } = useTerminology();
	const teamTerm = getTerm(TERMINOLOGY_KEYS.TEAM);
	const featureTerm = getTerm(TERMINOLOGY_KEYS.FEATURES);

	const { canUpdateTeamData, canUpdateTeamSettings, maxTeamsWithoutPremium } =
		useLicenseRestrictions();

	let subscribedToUpdates = false;

	const getInitialView = (tabParam: string | undefined): TeamViewType => {
		if (tabParam === "metrics") {
			return "metrics";
		}

		if (tabParam === "forecasts") {
			return "forecasts";
		}

		if (tabParam === "settings") {
			return "settings";
		}

		return "features";
	};

	const [team, setTeam] = useState<Team>();
	const [isLoading, setIsLoading] = useState<boolean>(true);
	const [isTeamUpdating, setIsTeamUpdating] = useState<boolean>(false);
	const [activeView, setActiveView] = useState<TeamViewType>(
		getInitialView(tab),
	);

	const { teamService, updateSubscriptionService, workTrackingSystemService } =
		useContext(ApiServiceContext);

	const fetchTeam = useCallback(async () => {
		const teamData = await teamService.getTeam(teamId);

		if (teamData) {
			setTeam(teamData);
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
					// Team was updated - reload data!
					setIsTeamUpdating(false);
					await fetchTeam();
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
												disabled={!canUpdateTeamSettings}
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
												disabled={!canUpdateTeamSettings}
											/>
											<SystemWipQuickSetting
												wipLimit={team.systemWIPLimit}
												onSave={async (systemWip) => {
													await updateTeamSettings((settings) => {
														settings.systemWIPLimit = systemWip;
													});
												}}
												disabled={!canUpdateTeamSettings}
											/>
											<FeatureWipQuickSetting
												featureWip={team.featureWip}
												onSave={async (featureWip) => {
													await updateTeamSettings((settings) => {
														settings.featureWIP = featureWip;
													}, true);
												}}
												disabled={!canUpdateTeamSettings}
											/>
										</QuickSettingsBar>
									}
									centerContent={
										<Tabs
											value={activeView}
											onChange={handleViewChange}
											aria-label="team view tabs"
										>
											<Tab label={featureTerm} value="features" />
											<Tab label="Forecasts" value="forecasts" />
											<Tab label="Metrics" value="metrics" />
											<Tab label="Settings" value="settings" />
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
										disableSave={!canUpdateTeamSettings}
										saveTooltip={`Free users can only update team settings for up to ${maxTeamsWithoutPremium} teams`}
									/>
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
