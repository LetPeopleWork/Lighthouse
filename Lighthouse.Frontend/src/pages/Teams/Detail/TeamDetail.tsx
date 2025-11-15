import { Button, Container, Stack, Tab, Tabs, Tooltip } from "@mui/material";
import Grid from "@mui/material/Grid";
import type React from "react";
import { useCallback, useContext, useEffect, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import ActionButton from "../../../components/Common/ActionButton/ActionButton";
import DetailHeader from "../../../components/Common/DetailHeader/DetailHeader";
import FeatureOwnerHeader from "../../../components/Common/FeatureOwnerHeader/FeatureOwnerHeader";
import ForecastConfiguration from "../../../components/Common/ForecastConfiguration/ForecastConfiguration";
import LoadingAnimation from "../../../components/Common/LoadingAnimation/LoadingAnimation";
import ServiceLevelExpectation from "../../../components/Common/ServiceLevelExpectation/ServiceLevelExpectation";
import SnackbarErrorHandler from "../../../components/Common/SnackbarErrorHandler/SnackbarErrorHandler";
import SystemWIPLimitDisplay from "../../../components/Common/SystemWipLimitDisplay/SystemWipLimitDisplay";
import { useLicenseRestrictions } from "../../../hooks/useLicenseRestrictions";
import type { Team } from "../../../models/Team/Team";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import { useTerminology } from "../../../services/TerminologyContext";
import type { IUpdateStatus } from "../../../services/UpdateSubscriptionService";
import TeamForecastView from "./TeamForecastView";
import TeamMetricsView from "./TeamMetricsView";

const TeamDetail: React.FC = () => {
	const navigate = useNavigate();
	const { id, tab } = useParams<{ id: string; tab?: string }>();
	const teamId = Number(id);

	const { getTerm } = useTerminology();
	const teamTerm = getTerm(TERMINOLOGY_KEYS.TEAM);
	const {
		canUpdateTeamData,
		updateTeamDataTooltip,
		canUpdateTeamSettings,
		updateTeamSettingsTooltip,
	} = useLicenseRestrictions();

	let subscribedToUpdates = false;

	const [team, setTeam] = useState<Team>();
	const [isLoading, setIsLoading] = useState<boolean>(true);
	const [isTeamUpdating, setIsTeamUpdating] = useState<boolean>(false);
	const [activeView, setActiveView] = useState<"forecast" | "metrics">(
		tab === "metrics" ? "metrics" : "forecast",
	);

	const { teamService, updateSubscriptionService } =
		useContext(ApiServiceContext);

	const fetchTeam = useCallback(async () => {
		const teamData = await teamService.getTeam(teamId);

		if (teamData) {
			setTeam(teamData);
		}

		setIsLoading(false);
	}, [teamService, teamId]);

	const onUpdateTeamData = async () => {
		if (!team) {
			return;
		}

		setIsTeamUpdating(true);
		await teamService.updateTeamData(team.id);
	};

	const onEditTeam = () => {
		navigate(`/teams/edit/${id}`);
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
		newView: "forecast" | "metrics",
	) => {
		setActiveView(newView);
		const tabPath = newView === "forecast" ? "forecasts" : newView;
		navigate(`/teams/${id}/${tabPath}`, { replace: true });
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

											<Stack
												direction={{ xs: "column", sm: "row" }}
												spacing={1}
												alignItems={{ xs: "flex-start", sm: "center" }}
											>
												<ForecastConfiguration team={team} />
												<ServiceLevelExpectation
													featureOwner={team}
													hide={activeView !== "forecast"}
												/>
												<SystemWIPLimitDisplay
													featureOwner={team}
													hide={activeView !== "forecast"}
												/>
											</Stack>
										</Stack>
									}
									centerContent={
										<Tabs
											value={activeView}
											onChange={handleViewChange}
											aria-label="team view tabs"
										>
											<Tab label="Forecasts" value="forecast" />
											<Tab label="Metrics" value="metrics" />
										</Tabs>
									}
									rightContent={
										<>
											<Tooltip title={updateTeamDataTooltip} arrow>
												<span>
													<ActionButton
														onClickHandler={onUpdateTeamData}
														buttonText={`Update ${teamTerm} Data`}
														maxHeight="40px"
														minWidth="120px"
														externalIsWaiting={isTeamUpdating}
														disabled={!canUpdateTeamData}
													/>
												</span>
											</Tooltip>
											<Tooltip title={updateTeamSettingsTooltip} arrow>
												<span>
													<Button
														variant="contained"
														onClick={onEditTeam}
														sx={{ maxHeight: "40px", minWidth: "120px" }}
														disabled={!canUpdateTeamSettings}
													>
														{`Edit ${teamTerm}`}
													</Button>
												</span>
											</Tooltip>
										</>
									}
								/>
							</Grid>

							<Grid size={{ xs: 12 }}>
								{activeView === "forecast" && team && (
									<TeamForecastView team={team} />
								)}

								{activeView === "metrics" && team && (
									<TeamMetricsView team={team} />
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
