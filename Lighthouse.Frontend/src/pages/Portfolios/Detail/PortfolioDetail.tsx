import CloudSyncIcon from "@mui/icons-material/CloudSync";
import {
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
import { useCallback, useContext, useEffect, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import DetailHeader from "../../../components/Common/DetailHeader/DetailHeader";
import FeatureOwnerHeader from "../../../components/Common/FeatureOwnerHeader/FeatureOwnerHeader";
import LoadingAnimation from "../../../components/Common/LoadingAnimation/LoadingAnimation";
import ModifyProjectSettings from "../../../components/Common/ProjectSettings/ModifyProjectSettings";
import PortfolioFeatureWipQuickSetting from "../../../components/Common/QuickSettings/PortfolioFeatureWipQuickSetting";
import SleQuickSetting from "../../../components/Common/QuickSettings/SleQuickSetting";
import SystemWipQuickSetting from "../../../components/Common/QuickSettings/SystemWipQuickSetting";
import QuickSettingsBar from "../../../components/Common/QuickSettingsBar/QuickSettingsBar";
import SnackbarErrorHandler from "../../../components/Common/SnackbarErrorHandler/SnackbarErrorHandler";
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

type PortfolioViewType = "features" | "metrics" | "deliveries" | "settings";

const PortfolioDetail: React.FC = () => {
	const navigate = useNavigate();
	const { id, tab } = useParams<{ id: string; tab?: string }>();
	const portfolioId = Number(id);

	let subscribedToUpdates = false;

	const [portfolio, setPortfolio] = useState<Portfolio>();

	const [isLoading, setIsLoading] = useState<boolean>(true);
	const [isPortfolioUpdating, setIsPortfolioUpdating] =
		useState<boolean>(false);

	const getInitialActiveView = (tabParam?: string): PortfolioViewType => {
		if (tabParam === "metrics") return "metrics";
		if (tabParam === "deliveries") return "deliveries";
		if (tabParam === "settings") return "settings";
		return "features";
	};

	const [activeView, setActiveView] = useState<PortfolioViewType>(
		getInitialActiveView(tab),
	);

	const [involvedTeams, setInvolvedTeams] = useState<ITeamSettings[]>([]);

	const {
		portfolioService,
		teamService,
		updateSubscriptionService,
		workTrackingSystemService,
	} = useContext(ApiServiceContext);

	const { getTerm } = useTerminology();
	const featuresTerm = getTerm(TERMINOLOGY_KEYS.FEATURES);
	const deliveriesTerm = getTerm(TERMINOLOGY_KEYS.DELIVERIES);
	const { canUpdatePortfolioData, updatePortfolioDataTooltip } =
		useLicenseRestrictions();

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

	const updatePortfolioSettings = useCallback(
		async (
			updateFn: (
				settings: ReturnType<
					typeof portfolioService.getPortfolioSettings
				> extends Promise<infer T>
					? T
					: never,
			) => void,
		) => {
			if (!portfolio) return;

			const settings = await portfolioService.getPortfolioSettings(
				portfolio.id,
			);
			updateFn(settings);
			await portfolioService.updatePortfolio(settings);
			await fetchPortfolio();
		},
		[portfolio, portfolioService, fetchPortfolio],
	);

	const updateTeamSettingsFromPortfolio = useCallback(
		async (
			teamId: number,
			updateFn: (
				settings: ReturnType<
					typeof teamService.getTeamSettings
				> extends Promise<infer T>
					? T
					: never,
			) => void,
			shouldRefreshForecasts = false,
		) => {
			if (!portfolio) return;

			const settings = await teamService.getTeamSettings(teamId);
			updateFn(settings);
			await teamService.updateTeam(settings);
			await fetchPortfolio();

			if (shouldRefreshForecasts) {
				await portfolioService.refreshForecastsForPortfolio(portfolio.id);
			}
		},
		[portfolio, teamService, portfolioService, fetchPortfolio],
	);

	const onRefreshFeatures = async () => {
		if (portfolio == null) {
			return;
		}

		setIsPortfolioUpdating(true);
		await portfolioService.refreshFeaturesForPortfolio(portfolio.id);
	};

	const getTabPath = (newView: PortfolioViewType): string => {
		if (newView === "features") return "features";
		if (newView === "deliveries") return "deliveries";
		return newView;
	};

	const handleViewChange = (
		_event: React.SyntheticEvent,
		newView: PortfolioViewType,
	) => {
		setActiveView(newView);
		const tabPath = getTabPath(newView);
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
										</Stack>
									}
									quickSettingsContent={
										<QuickSettingsBar>
											<SleQuickSetting
												probability={
													portfolio.serviceLevelExpectationProbability
												}
												range={portfolio.serviceLevelExpectationRange}
												onSave={async (probability, range) => {
													await updatePortfolioSettings((settings) => {
														settings.serviceLevelExpectationProbability =
															probability;
														settings.serviceLevelExpectationRange = range;
													});
												}}
												disabled={!canUpdatePortfolioData}
											/>
											<SystemWipQuickSetting
												wipLimit={portfolio.systemWIPLimit}
												onSave={async (systemWip) => {
													await updatePortfolioSettings((settings) => {
														settings.systemWIPLimit = systemWip;
													});
												}}
												disabled={!canUpdatePortfolioData}
											/>
											<PortfolioFeatureWipQuickSetting
												teams={involvedTeams}
												onSave={async (teamId, featureWip) => {
													await updateTeamSettingsFromPortfolio(
														teamId,
														(settings) => {
															settings.featureWIP = featureWip;
														},
														true,
													);
												}}
												disabled={!canUpdatePortfolioData}
											/>
										</QuickSettingsBar>
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
											<Tab label="Settings" value="settings" />
										</Tabs>
									}
									rightContent={
										<Tooltip
											title={
												updatePortfolioDataTooltip || `Refresh ${featuresTerm}`
											}
											arrow
										>
											<span>
												<IconButton
													aria-label={`Refresh ${featuresTerm}`}
													onClick={onRefreshFeatures}
													disabled={
														!canUpdatePortfolioData || isPortfolioUpdating
													}
													color="primary"
												>
													{isPortfolioUpdating ? (
														<CircularProgress size={24} />
													) : (
														<CloudSyncIcon />
													)}
												</IconButton>
											</span>
										</Tooltip>
									}
								/>
							</Grid>

							<Grid size={{ xs: 12 }}>
								{activeView === "features" && portfolio && (
									<PortfolioForecastView portfolio={portfolio} />
								)}

								{activeView === "deliveries" && portfolio && (
									<PortfolioDeliveryView portfolio={portfolio} />
								)}

								{activeView === "metrics" && portfolio && (
									<PortfolioMetricsView portfolio={portfolio} />
								)}

								{activeView === "settings" && portfolio && (
									<ModifyProjectSettings
										title="Edit Portfolio"
										getWorkTrackingSystems={() =>
											workTrackingSystemService.getConfiguredWorkTrackingSystems()
										}
										getProjectSettings={() =>
											portfolioService.getPortfolioSettings(portfolio.id)
										}
										getAllTeams={() => teamService.getTeams()}
										saveProjectSettings={async (settings) => {
											await portfolioService.updatePortfolio(settings);
										}}
										validateProjectSettings={(settings) =>
											portfolioService.validatePortfolioSettings(settings)
										}
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

export default PortfolioDetail;
