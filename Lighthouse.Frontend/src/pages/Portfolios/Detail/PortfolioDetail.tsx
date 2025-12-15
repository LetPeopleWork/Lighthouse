import { Button, Container, Stack, Tab, Tabs, Tooltip } from "@mui/material";
import Grid from "@mui/material/Grid";
import type React from "react";
import { useCallback, useContext, useEffect, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import ActionButton from "../../../components/Common/ActionButton/ActionButton";
import DetailHeader from "../../../components/Common/DetailHeader/DetailHeader";
import FeatureOwnerHeader from "../../../components/Common/FeatureOwnerHeader/FeatureOwnerHeader";
import LoadingAnimation from "../../../components/Common/LoadingAnimation/LoadingAnimation";
import ServiceLevelExpectation from "../../../components/Common/ServiceLevelExpectation/ServiceLevelExpectation";
import SnackbarErrorHandler from "../../../components/Common/SnackbarErrorHandler/SnackbarErrorHandler";
import SystemWIPLimitDisplay from "../../../components/Common/SystemWipLimitDisplay/SystemWipLimitDisplay";
import { useLicenseRestrictions } from "../../../hooks/useLicenseRestrictions";
import type {
	IPortfolio,
	Portfolio,
} from "../../../models/Portfolio/Portfolio";
import type { ITeamSettings } from "../../../models/Team/TeamSettings";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import { useTerminology } from "../../../services/TerminologyContext";
import type { IUpdateStatus } from "../../../services/UpdateSubscriptionService";
import PortfolioDeliveryView from "./PortfolioDeliveryView";
import PortfolioForecastView from "./PortfolioForecastView";
import PortfolioMetricsView from "./PortfolioMetricsView";

const PortfolioDetail: React.FC = () => {
	const navigate = useNavigate();
	const { id, tab } = useParams<{ id: string; tab?: string }>();
	const portfolioId = Number(id);

	let subscribedToUpdates = false;

	const [portfolio, setPortfolio] = useState<Portfolio>();

	const [isLoading, setIsLoading] = useState<boolean>(true);
	const [isPortfolioUpdating, setIsPortfolioUpdating] =
		useState<boolean>(false);
	const [activeView, setActiveView] = useState<
		"features" | "metrics" | "deliveries"
	>(
		tab === "metrics"
			? "metrics"
			: tab === "deliveries"
				? "deliveries"
				: "features",
	);

	const [involvedTeams, setInvolvedTeams] = useState<ITeamSettings[]>([]);

	const { portfolioService, teamService, updateSubscriptionService } =
		useContext(ApiServiceContext);

	const { getTerm } = useTerminology();
	const featuresTerm = getTerm(TERMINOLOGY_KEYS.FEATURES);
	const deliveriesTerm = getTerm(TERMINOLOGY_KEYS.DELIVERIES);
	const {
		canUpdatePortfolioData,
		updatePortfolioDataTooltip,
		canUpdatePortfolioSettings,
		updatePortfolioSettingsTooltip,
	} = useLicenseRestrictions();

	const fetchPortfolio = useCallback(async () => {
		const fetchInvolvedTeams = async (portfolioData: IPortfolio | null) => {
			const teamSettings: ITeamSettings[] = [];

			for (const involvedTeam of portfolioData?.involvedTeams ?? []) {
				const involvedTeamSetting = await teamService.getTeamSettings(
					involvedTeam.id,
				);
				teamSettings.push(involvedTeamSetting);
			}

			return teamSettings;
		};

		const portfolioData = await portfolioService.getPortfolio(portfolioId);
		const settings = await portfolioService.getPortfolioSettings(portfolioId);
		const involvedTeamData = await fetchInvolvedTeams(portfolioData);

		if (portfolioData && settings) {
			setPortfolio(portfolioData);
			setInvolvedTeams(involvedTeamData);
		}

		setIsLoading(false);
	}, [portfolioService, teamService, portfolioId]);

	const onRefreshFeatures = async () => {
		if (portfolio == null) {
			return;
		}

		setIsPortfolioUpdating(true);
		await portfolioService.refreshFeaturesForPortfolio(portfolio.id);
	};

	const onEditPortfolio = () => {
		navigate(`/portfolios/edit/${id}`);
	};

	const onTeamSettingsChange = async (updatedTeamSettings: ITeamSettings) => {
		await teamService.updateTeam(updatedTeamSettings);

		await portfolioService.refreshForecastsForPortfolio(portfolioId);
	};

	const handleViewChange = (
		_event: React.SyntheticEvent,
		newView: "features" | "metrics" | "deliveries",
	) => {
		setActiveView(newView);
		const tabPath =
			newView === "features"
				? "features"
				: newView === "deliveries"
					? "deliveries"
					: newView;
		navigate(`/portfolios/${id}/${tabPath}`, { replace: true });
	};

	useEffect(() => {
		const setUpPortfolioUpdateSubscription = async () => {
			const handlePortfolioUpdate = async (update: IUpdateStatus) => {
				if (update.status === "Completed") {
					// Portfolio was updated - reload data!
					await fetchPortfolio();
				}

				updatePortfolioRefreshButton(update);
			};

			const updatePortfolioRefreshButton = (update: IUpdateStatus | null) => {
				const isFeatureUpdate =
					update?.updateType === "Features" ||
					update?.updateType === "Forecasts";

				if (isFeatureUpdate) {
					const isUpdating =
						update?.status === "Queued" || update?.status === "InProgress";
					setIsPortfolioUpdating(isUpdating);
				}
			};

			await updateSubscriptionService.subscribeToFeatureUpdates(
				portfolioId,
				handlePortfolioUpdate,
			);
			await updateSubscriptionService.subscribeToForecastUpdates(
				portfolioId,
				handlePortfolioUpdate,
			);

			const portfolioUpdateStatus =
				await updateSubscriptionService.getUpdateStatus(
					"Features",
					portfolioId,
				);
			updatePortfolioRefreshButton(portfolioUpdateStatus);

			const forecastUpdateStatus =
				await updateSubscriptionService.getUpdateStatus(
					"Forecasts",
					portfolioId,
				);
			updatePortfolioRefreshButton(forecastUpdateStatus);
		};

		if (portfolio && !subscribedToUpdates) {
			subscribedToUpdates = true;
			setUpPortfolioUpdateSubscription();
		} else {
			fetchPortfolio();
		}

		return () => {
			updateSubscriptionService.unsubscribeFromFeatureUpdates(portfolioId);
			updateSubscriptionService.unsubscribeFromForecastUpdates(portfolioId);
		};
	}, [
		portfolio,
		portfolioId,
		fetchPortfolio,
		updateSubscriptionService,
		subscribedToUpdates,
	]);

	return (
		<SnackbarErrorHandler>
			<LoadingAnimation hasError={false} isLoading={isLoading}>
				<Container maxWidth={false}>
					{portfolio && (
						<Grid container spacing={3}>
							<Grid size={{ xs: 12 }}>
								<DetailHeader
									leftContent={
										<Stack spacing={1} direction="row">
											<FeatureOwnerHeader featureOwner={portfolio} />
											<Stack
												direction={{ xs: "column", sm: "row" }}
												spacing={1}
												alignItems={{ xs: "flex-start", sm: "center" }}
											>
												<ServiceLevelExpectation
													featureOwner={portfolio}
													hide={activeView !== "features"}
													itemTypeKey={TERMINOLOGY_KEYS.FEATURES}
												/>
												<SystemWIPLimitDisplay
													featureOwner={portfolio}
													hide={activeView !== "features"}
												/>
											</Stack>
										</Stack>
									}
									centerContent={
										<Tabs
											value={activeView}
											onChange={handleViewChange}
											aria-label="portfolio view tabs"
										>
											<Tab label={featuresTerm} value="features" />
											<Tab label={deliveriesTerm} value="deliveries" />
											<Tab label="Metrics" value="metrics" />
										</Tabs>
									}
									rightContent={
										<>
											<Tooltip title={updatePortfolioDataTooltip} arrow>
												<span>
													<ActionButton
														buttonText={`Refresh ${featuresTerm}`}
														onClickHandler={onRefreshFeatures}
														maxHeight="40px"
														minWidth="120px"
														externalIsWaiting={isPortfolioUpdating}
														disabled={!canUpdatePortfolioData}
													/>
												</span>
											</Tooltip>
											<Tooltip title={updatePortfolioSettingsTooltip} arrow>
												<span>
													<Button
														variant="contained"
														onClick={onEditPortfolio}
														disabled={!canUpdatePortfolioSettings}
														sx={{ maxHeight: "40px", minWidth: "120px" }}
													>
														Edit Portfolio
													</Button>
												</span>
											</Tooltip>
										</>
									}
								/>
							</Grid>

							<Grid size={{ xs: 12 }}>
								{activeView === "features" && portfolio && (
									<PortfolioForecastView
										portfolio={portfolio}
										involvedTeams={involvedTeams}
										onTeamSettingsChange={onTeamSettingsChange}
									/>
								)}

								{activeView === "deliveries" && portfolio && (
									<PortfolioDeliveryView portfolio={portfolio} />
								)}

								{activeView === "metrics" && portfolio && (
									<PortfolioMetricsView portfolio={portfolio} />
								)}
							</Grid>
						</Grid>
					)}
				</Container>
			</LoadingAnimation>
		</SnackbarErrorHandler>
	);
};

export default PortfolioDetail;
