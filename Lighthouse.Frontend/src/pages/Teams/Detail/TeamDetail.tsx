import GppMaybeOutlinedIcon from "@mui/icons-material/GppMaybeOutlined";
import {
	Button,
	Container,
	IconButton,
	Tab,
	Tabs,
	Tooltip,
	Typography,
} from "@mui/material";
import Grid from "@mui/material/Grid";
import type React from "react";
import { useCallback, useContext, useEffect, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import ActionButton from "../../../components/Common/ActionButton/ActionButton";
import LoadingAnimation from "../../../components/Common/LoadingAnimation/LoadingAnimation";
import LocalDateTimeDisplay from "../../../components/Common/LocalDateTimeDisplay/LocalDateTimeDisplay";
import ServiceLevelExpectation from "../../../components/Common/ServiceLevelExpectation/ServiceLevelExpectation";
import type { Team } from "../../../models/Team/Team";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import type { IUpdateStatus } from "../../../services/UpdateSubscriptionService";
import TeamForecastView from "./TeamForecastView";
import TeamMetricsView from "./TeamMetricsView";

const TeamDetail: React.FC = () => {
	const navigate = useNavigate();
	const { id } = useParams<{ id: string }>();
	const teamId = Number(id);

	let subscribedToUpdates = false;

	const [team, setTeam] = useState<Team>();
	const [isLoading, setIsLoading] = useState<boolean>(true);
	const [isTeamUpdating, setIsTeamUpdating] = useState<boolean>(false);
	const [activeView, setActiveView] = useState<"forecast" | "metrics">(
		"forecast",
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
	};

	return (
		<LoadingAnimation hasError={false} isLoading={isLoading}>
			<Container maxWidth={false}>
				{team == null ? (
					<></>
				) : (
					<Grid container spacing={3}>
						<Grid size={{ xs: 4 }}>
							<Typography variant="h3">
								{team.name}

								{team.useFixedDatesForThroughput && (
									<Tooltip title="This team is using a fixed Throughput - consider switching to a rolling history to get more realistic forecasts">
										<IconButton size="small" sx={{ ml: 1 }}>
											<GppMaybeOutlinedIcon sx={{ color: "warning.main" }} />
										</IconButton>
									</Tooltip>
								)}
							</Typography>

							<Typography variant="h6">
								Last Updated on{" "}
								<LocalDateTimeDisplay
									utcDate={team.lastUpdated}
									showTime={true}
								/>
							</Typography>
						</Grid>

						<Grid
							size={{ xs: 4 }}
							sx={{
								display: "flex",
								justifyContent: "center",
								alignItems: "center",
							}}
						>
							<Tabs
								value={activeView}
								onChange={handleViewChange}
								aria-label="team view tabs"
							>
								<Tab label="Forecasts" value="forecast" />
								<Tab label="Metrics" value="metrics" />
							</Tabs>
						</Grid>

						<Grid
							size={{ xs: 4 }}
							sx={{ display: "flex", gap: 2, justifyContent: "flex-end" }}
						>
							<ServiceLevelExpectation featureOwner={team} />

							<ActionButton
								onClickHandler={onUpdateTeamData}
								buttonText="Update Team Data"
								maxHeight="40px"
								minWidth="120px"
								externalIsWaiting={isTeamUpdating}
							/>
							<Button
								variant="contained"
								onClick={onEditTeam}
								sx={{ maxHeight: "40px", minWidth: "120px" }}
							>
								Edit Team
							</Button>
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
	);
};

export default TeamDetail;
