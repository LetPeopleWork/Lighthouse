import { Alert, Container, Link, Typography } from "@mui/material";
import type React from "react";
import { useContext } from "react";
import { Link as RouterLink, useNavigate, useParams } from "react-router";
import CreatePortfolioWizard from "../../../components/Common/CreateWizards/CreatePortfolioWizard";
import ModifyProjectSettings from "../../../components/Common/ProjectSettings/ModifyProjectSettings";
import SnackbarErrorHandler from "../../../components/Common/SnackbarErrorHandler/SnackbarErrorHandler";
import { useRbacGate } from "../../../hooks/useRbacGate";
import type { IPortfolioSettings } from "../../../models/Portfolio/PortfolioSettings";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import { UsageDataEventName } from "../../../services/Api/UsageDataService";
import { useTerminology } from "../../../services/TerminologyContext";
import { useUsageDataReporter } from "../../../services/UsageData/usageDataReporter";

const EditPortfolio: React.FC = () => {
	const { id } = useParams<{ id?: string }>();
	const isNewPortfolio = id === undefined;
	const gate = useRbacGate({ kind: "systemAdmin" });
	const reportUsage = useUsageDataReporter();

	const cloneFromId = Number.parseInt(
		new URLSearchParams(globalThis.location.search).get("cloneFrom") ?? "",
		10,
	);
	const hasCloneSource = !Number.isNaN(cloneFromId);
	const useWizard = isNewPortfolio && !hasCloneSource;

	const navigate = useNavigate();
	const { portfolioService, workTrackingSystemService, teamService } =
		useContext(ApiServiceContext);
	const { getTerm } = useTerminology();
	const portfolioTerm = getTerm(TERMINOLOGY_KEYS.PORTFOLIO);

	const pageTitle = isNewPortfolio
		? `Create ${portfolioTerm}`
		: `Update ${portfolioTerm}`;

	const getPortfolioSettings = async (): Promise<IPortfolioSettings> => {
		if (id !== undefined) {
			return await portfolioService.getPortfolioSettings(
				Number.parseInt(id, 10),
			);
		}

		const sourceSettings =
			await portfolioService.getPortfolioSettings(cloneFromId);
		return {
			...sourceSettings,
			id: 0,
			name: `Copy of ${sourceSettings.name}`,
		};
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
			reportUsage({ name: UsageDataEventName.PortfolioCreated });
			await portfolioService.refreshFeaturesForPortfolio(savedSettings.id);
			navigate(`/portfolios/${savedSettings.id}/settings`);
		} else {
			savedSettings = await portfolioService.updatePortfolio(updatedSettings);
			navigate(`/portfolios/${savedSettings.id}`);
		}
		return savedSettings;
	};

	const getConnections = async () => {
		return await workTrackingSystemService.getConfiguredWorkTrackingSystems();
	};

	const wizardSavePortfolioSettings = async (
		updatedSettings: IPortfolioSettings,
	) => {
		const savedSettings =
			await portfolioService.createPortfolio(updatedSettings);
		reportUsage({ name: UsageDataEventName.PortfolioCreated });
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
