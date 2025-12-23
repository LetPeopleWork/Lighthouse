import type React from "react";
import { useContext } from "react";
import { useNavigate, useParams } from "react-router-dom";
import ModifyProjectSettings from "../../../components/Common/ProjectSettings/ModifyProjectSettings";
import SnackbarErrorHandler from "../../../components/Common/SnackbarErrorHandler/SnackbarErrorHandler";
import type { IPortfolioSettings } from "../../../models/Portfolio/PortfolioSettings";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import { useTerminology } from "../../../services/TerminologyContext";

const EditPortfolio: React.FC = () => {
	const { id } = useParams<{ id?: string }>();
	const isNewPortfolio = id === undefined;

	const navigate = useNavigate();
	const {
		settingsService,
		portfolioService,
		workTrackingSystemService,
		teamService,
	} = useContext(ApiServiceContext);
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
		return await settingsService.getDefaultProjectSettings();
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
