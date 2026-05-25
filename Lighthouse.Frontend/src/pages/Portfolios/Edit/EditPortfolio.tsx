import { Alert, Container, Link, Typography } from "@mui/material";
import type React from "react";
import { useContext } from "react";
import { Link as RouterLink, useNavigate, useParams } from "react-router-dom";
import CreatePortfolioWizard from "../../../components/Common/CreateWizards/CreatePortfolioWizard";
import ModifyProjectSettings from "../../../components/Common/ProjectSettings/ModifyProjectSettings";
import SnackbarErrorHandler from "../../../components/Common/SnackbarErrorHandler/SnackbarErrorHandler";
import { useRbacGate } from "../../../hooks/useRbacGate";
import type { IPortfolioSettings } from "../../../models/Portfolio/PortfolioSettings";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import { useTerminology } from "../../../services/TerminologyContext";

const EditPortfolio: React.FC = () => {
	const { id } = useParams<{ id?: string }>();
	const isNewPortfolio = id === undefined;
	const gate = useRbacGate({ kind: "systemAdmin" });

	const urlParams = new URLSearchParams(globalThis.location.search);
	const hasCloneFrom = urlParams.get("cloneFrom") !== null;
	const useWizard = isNewPortfolio && !hasCloneFrom;

	const navigate = useNavigate();
	const { portfolioService, workTrackingSystemService, teamService } =
		useContext(ApiServiceContext);
	const { getTerm } = useTerminology();
	const portfolioTerm = getTerm(TERMINOLOGY_KEYS.PORTFOLIO);

	const pageTitle = isNewPortfolio
		? `Create ${portfolioTerm}`
		: `Update ${portfolioTerm}`;

	const getPortfolioSettings = async () => {
		const urlParams = new URLSearchParams(globalThis.location.search);
		const cloneFromId = urlParams.get("cloneFrom");

		if (isNewPortfolio && cloneFromId) {
			const cloneId = Number.parseInt(cloneFromId, 10);
			if (!Number.isNaN(cloneId)) {
				const sourceSettings =
					await portfolioService.getPortfolioSettings(cloneId);
				return {
					...sourceSettings,
					id: 0,
					name: `Copy of ${sourceSettings.name}`,
				};
			}
		}

		if (!isNewPortfolio && id) {
			return await portfolioService.getPortfolioSettings(
				Number.parseInt(id, 10),
			);
		}

		const defaultPortfolioSettings: IPortfolioSettings = {
			overrideRealChildCountStates: [],
			usePercentileToCalculateDefaultAmountOfWorkItems: false,
			defaultAmountOfWorkItemsPerFeature: 10,
			defaultWorkItemPercentile: 0,
			percentileHistoryInDays: 0,
			sizeEstimateAdditionalFieldDefinitionId: null,
			featureOwnerAdditionalFieldDefinitionId: null,
			id: 0,
			name: "New Portfolio",
			dataRetrievalValue: "",
			workItemTypes: [],
			toDoStates: [],
			doingStates: [],
			doneStates: [],
			workTrackingSystemConnectionId: 0,
			serviceLevelExpectationProbability: 0,
			serviceLevelExpectationRange: 0,
			systemWIPLimit: 0,
			parentOverrideAdditionalFieldDefinitionId: null,
			blockedStates: [],
			blockedTags: [],
			stateMappings: [],
			doneItemsCutoffDays: 365,
			processBehaviourChartBaselineStartDate: null,
			processBehaviourChartBaselineEndDate: null,
			estimationAdditionalFieldDefinitionId: null,
			estimationUnit: null,
			useNonNumericEstimation: false,
			estimationCategoryValues: [],
			stalenessThresholdDays: 14,
		};

		return defaultPortfolioSettings;
	};

	const getWorkTrackingSystems = async () => {
		return await workTrackingSystemService.getConfiguredWorkTrackingSystems();
	};

	const getAllTeams = async () => {
		return await teamService.getTeams();
	};

	const validateProjectSettings = async (
		updatedProjectSettings: IPortfolioSettings,
	) => {
		return await portfolioService.validatePortfolioSettings(
			updatedProjectSettings,
		);
	};

	const saveProjectSettings = async (updatedSettings: IPortfolioSettings) => {
		let savedSettings: IPortfolioSettings;
		if (isNewPortfolio) {
			savedSettings = await portfolioService.createPortfolio(updatedSettings);
			await portfolioService.refreshFeaturesForPortfolio(savedSettings.id);
			navigate(`/portfolios/${savedSettings.id}/settings`);
		} else {
			savedSettings = await portfolioService.updatePortfolio(updatedSettings);
			navigate(`/portfolios/${savedSettings.id}`);
		}
	};

	const getConnections = async () => {
		return await workTrackingSystemService.getConfiguredWorkTrackingSystems();
	};

	const wizardSavePortfolioSettings = async (
		updatedSettings: IPortfolioSettings,
	) => {
		const savedSettings =
			await portfolioService.createPortfolio(updatedSettings);
		await portfolioService.refreshFeaturesForPortfolio(savedSettings.id);
		navigate(`/portfolios/${savedSettings.id}/metrics`);
	};

	if (gate.isLoading) {
		return null;
	}

	if (!gate.allowed) {
		return (
			<Container maxWidth={false}>
				<Alert
					severity="info"
					sx={{ mb: 2 }}
					data-testid="portfolio-edit-no-access-alert"
				>
					You don't have permission to access this page.{" "}
					<Link component={RouterLink} to="/">
						Back to Overview
					</Link>
				</Alert>
			</Container>
		);
	}

	if (useWizard) {
		return (
			<SnackbarErrorHandler>
				<Container maxWidth={false}>
					<Typography variant="h4" sx={{ mb: 2 }}>
						{pageTitle}
					</Typography>
					<CreatePortfolioWizard
						getConnections={getConnections}
						validatePortfolioSettings={validateProjectSettings}
						savePortfolioSettings={wizardSavePortfolioSettings}
						onCancel={() => navigate("/")}
					/>
				</Container>
			</SnackbarErrorHandler>
		);
	}

	return (
		<SnackbarErrorHandler>
			<ModifyProjectSettings
				title={pageTitle}
				getProjectSettings={getPortfolioSettings}
				getWorkTrackingSystems={getWorkTrackingSystems}
				getAllTeams={getAllTeams}
				validateProjectSettings={validateProjectSettings}
				saveProjectSettings={saveProjectSettings}
			/>
		</SnackbarErrorHandler>
	);
};

export default EditPortfolio;
