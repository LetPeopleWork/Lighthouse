import WarningIcon from "@mui/icons-material/Warning";
import {
	Alert,
	Box,
	Button,
	Checkbox,
	Chip,
	FormControlLabel,
	Grid,
	Tooltip,
	Typography,
} from "@mui/material";
import camelcaseKeys from "camelcase-keys";
import type React from "react";
import { useCallback, useEffect, useId, useState } from "react";
import LoadingAnimation from "../../../../../components/Common/LoadingAnimation/LoadingAnimation";
import type { ConfigurationExport } from "../../../../../models/Configuration/ConfigurationExport";
import type {
	ConfigurationValidation,
	ConfigurationValidationItem,
} from "../../../../../models/Configuration/ConfigurationValidation";
import type { IProjectSettings } from "../../../../../models/Project/ProjectSettings";
import type { ITeamSettings } from "../../../../../models/Team/TeamSettings";
import { TERMINOLOGY_KEYS } from "../../../../../models/TerminologyKeys";
import type { IWorkTrackingSystemConnection } from "../../../../../models/WorkTracking/WorkTrackingSystemConnection";
import type { IConfigurationService } from "../../../../../services/Api/ConfigurationService";
import { useTerminology } from "../../../../../services/TerminologyContext";

interface ImportSettingsStepProps {
	configurationService: IConfigurationService;
	onNext: (
		newWorkTrackingSystems: IWorkTrackingSystemConnection[],
		updatedWorkTrackingSystems: IWorkTrackingSystemConnection[],
		newTeams: ITeamSettings[],
		updatedTeams: ITeamSettings[],
		newProjects: IProjectSettings[],
		updatedProjects: IProjectSettings[],
		workTrackingSystemsIdMapping: Map<number, number>,
		teamIdMapping: Map<number, number>,
		clearConfiguration: boolean,
	) => void;
	onClose: () => void;
}

const ImportSettingsStep: React.FC<ImportSettingsStepProps> = ({
	configurationService,
	onNext,
	onClose,
}) => {
	const [file, setFile] = useState<File | null>(null);
	const [parsedConfig, setParsedConfig] = useState<ConfigurationExport | null>(
		null,
	);
	const [validationResults, setValidationResults] =
		useState<ConfigurationValidation | null>(null);
	const [isValidating, setIsValidating] = useState<boolean>(false);
	const [hasError, setHasError] = useState<boolean>(false);
	const [errorMessage, setErrorMessage] = useState<string>("");
	const [clearConfiguration, setClearConfiguration] = useState<boolean>(false);

	const { getTerm } = useTerminology();
	const teamsTerm = getTerm(TERMINOLOGY_KEYS.TEAMS);
	const workItemsTerm = getTerm(TERMINOLOGY_KEYS.WORK_ITEMS);
	const workTrackingSystemsTerm = getTerm(
		TERMINOLOGY_KEYS.WORK_TRACKING_SYSTEMS,
	);

	const handleFileChange = (event: React.ChangeEvent<HTMLInputElement>) => {
		const selectedFile = event.target.files?.[0] || null;
		if (selectedFile) {
			setFile(selectedFile);
			setParsedConfig(null);
			setValidationResults(null);
			setHasError(false);
			setErrorMessage("");
		}
	};

	const transformStatus = useCallback(
		(items: ConfigurationValidationItem[]): ConfigurationValidationItem[] => {
			return items.map((item) => ({
				...item,
				status: item.status === "Update" ? "New" : item.status,
			}));
		},
		[],
	);

	const workTrackingSystemsIdMapping = new Map<number, number>();
	const teamIdMapping = new Map<number, number>();

	useEffect(() => {
		const validateFile = async () => {
			if (!file) {
				return;
			}

			setIsValidating(true);
			setHasError(false);
			setErrorMessage("");

			try {
				const fileContent = await file.text();

				try {
					const rawData = JSON.parse(fileContent);
					const configData = camelcaseKeys(rawData, {
						deep: true,
					}) as ConfigurationExport;
					setParsedConfig(configData);

					const validation =
						await configurationService.validateConfiguration(configData);

					if (clearConfiguration) {
						const transformedValidation = {
							...validation,
							workTrackingSystems: transformStatus(
								validation.workTrackingSystems,
							),
							teams: transformStatus(validation.teams),
							projects: transformStatus(validation.projects),
						};

						setValidationResults(transformedValidation);
					} else {
						setValidationResults(validation);
					}
				} catch (error: unknown) {
					setHasError(true);
					const errorMsg =
						error instanceof Error
							? `Invalid JSON format: ${error.message}`
							: "Invalid JSON format. Please select a valid configuration file.";
					setErrorMessage(errorMsg);
				}
			} catch (error: unknown) {
				setHasError(true);
				const errorMsg =
					error instanceof Error
						? `Error reading file: ${error.message}`
						: "Error reading file. Please try again.";
				setErrorMessage(errorMsg);
			} finally {
				setIsValidating(false);
			}
		};

		validateFile();
	}, [file, configurationService, clearConfiguration, transformStatus]);

	const hasValidationErrors = (): boolean => {
		if (!validationResults) return true;

		const hasErrors = [
			...validationResults.workTrackingSystems,
			...validationResults.teams,
			...validationResults.projects,
		].some((item) => item.status === "Error");

		return hasErrors;
	};

	const renderValidationSection = (
		title: string,
		items: ConfigurationValidationItem[],
	) => {
		return (
			<>
				<Typography variant="subtitle1" sx={{ mt: 2, fontWeight: "bold" }}>
					{title}
				</Typography>
				{items.length === 0 ? (
					<Typography variant="body2">
						No {workItemsTerm} to validate
					</Typography>
				) : (
					items.map((item) => (
						<Grid container key={item.id} sx={{ my: 0.5 }}>
							<Grid size={{ xs: 6 }}>
								<Typography variant="body2">{item.name}</Typography>
							</Grid>
							<Grid size={{ xs: 6 }}>
								{" "}
								{item.status === "Error" ? (
									<>
										<Chip
											label={item.status}
											color="error"
											size="small"
											sx={{ mr: 1 }}
										/>
										<Typography variant="body2" color="error" component="span">
											{item.errorMessage}
										</Typography>
									</>
								) : (
									<Chip
										label={item.status}
										color={item.status === "New" ? "primary" : "success"}
										size="small"
										variant="outlined"
									/>
								)}
							</Grid>
						</Grid>
					))
				)}
			</>
		);
	};

	const fetchNewAndUpdatedElements = (
		validationItems: ConfigurationValidationItem[],
		configItems: { id: number | null; name: string }[],
		newList: { id: number | null; name: string }[],
		updatedList: { id: number | null; name: string }[],
		idMappingTable: Map<number, number>,
	) => {
		for (const validationResult of validationItems) {
			const configItem = configItems.find(
				(wts) => wts.name === validationResult.name,
			);

			if (configItem) {
				if (validationResult.status === "New") {
					newList.push(configItem);
				} else if (validationResult.status === "Update") {
					idMappingTable.set(configItem.id ?? 0, validationResult.id);
					configItem.id = validationResult.id;
					updatedList.push(configItem);
				}
			}
		}
	};

	const removeSecretOptionsFromUpdatedWorkTrackingSystems = (
		updatedWorkTrackingSystems: IWorkTrackingSystemConnection[],
	) => {
		for (const wts of updatedWorkTrackingSystems) {
			const nonSecretOptions = wts.options.filter((option) => !option.isSecret);

			wts.options = nonSecretOptions;
		}
	};

	const handleNextClick = () => {
		if (parsedConfig && validationResults && !hasValidationErrors()) {
			const newWorkTrackingSystems: IWorkTrackingSystemConnection[] = [];
			const updatedWorkTrackingSystems: IWorkTrackingSystemConnection[] = [];
			const newTeams: ITeamSettings[] = [];
			const updatedTeams: ITeamSettings[] = [];
			const newProjects: IProjectSettings[] = [];
			const updatedProjects: IProjectSettings[] = [];

			fetchNewAndUpdatedElements(
				validationResults.workTrackingSystems,
				parsedConfig.workTrackingSystems,
				newWorkTrackingSystems,
				updatedWorkTrackingSystems,
				workTrackingSystemsIdMapping,
			);

			fetchNewAndUpdatedElements(
				validationResults.teams,
				parsedConfig.teams,
				newTeams,
				updatedTeams,
				teamIdMapping,
			);

			fetchNewAndUpdatedElements(
				validationResults.projects,
				parsedConfig.projects,
				newProjects,
				updatedProjects,
				new Map<number, number>(),
			);

			for (const project of newProjects) {
				for (const milestone of project.milestones) {
					milestone.id = 0;
				}
			}

			removeSecretOptionsFromUpdatedWorkTrackingSystems(
				updatedWorkTrackingSystems,
			);

			onNext(
				newWorkTrackingSystems,
				updatedWorkTrackingSystems,
				newTeams,
				updatedTeams,
				newProjects,
				updatedProjects,
				workTrackingSystemsIdMapping,
				teamIdMapping,
				clearConfiguration,
			);
		}
	};

	const configFileId = useId();

	return (
		<>
			<Grid container spacing={2} sx={{ mb: 2 }}>
				<Grid size={{ xs: 12 }}>
					<input
						type="file"
						id={configFileId}
						accept=".json"
						onChange={handleFileChange}
						style={{ display: "none" }}
						data-testid="file-input"
					/>
					<label htmlFor={configFileId}>
						<Button
							variant="contained"
							component="span"
							disabled={isValidating}
						>
							Select Configuration File
						</Button>
					</label>
					<FormControlLabel
						control={
							<Checkbox
								checked={clearConfiguration}
								onChange={(e) => setClearConfiguration(e.target.checked)}
								data-testid="clear-configuration-checkbox"
							/>
						}
						label={
							<div style={{ display: "flex", alignItems: "center" }}>
								Delete Existing Configuration
								<Tooltip
									title={`If this option is selected, all existing ${workTrackingSystemsTerm}, ${teamsTerm}, and projects will be removed`}
									arrow
									placement="top"
								>
									<WarningIcon
										color="warning"
										sx={{ ml: 1, fontSize: "1rem" }}
									/>
								</Tooltip>
							</div>
						}
						sx={{ ml: 2 }}
					/>
				</Grid>
			</Grid>

			{/* Error messages */}
			{hasError && (
				<Alert severity="error" sx={{ mb: 2 }}>
					{errorMessage}
				</Alert>
			)}

			{file && validationResults && (
				<Box
					sx={{
						height: "calc(100vh - 350px)",
						overflow: "auto",
						mb: 2,
						border: (theme) => `1px solid ${theme.palette.divider}`,
						borderRadius: 1,
						p: 2,
					}}
				>
					<LoadingAnimation isLoading={isValidating} hasError={false}>
						<div data-testid="validation-results">
							<Typography variant="body1" sx={{ mt: 1 }}>
								{file.name}
							</Typography>

							{renderValidationSection(
								workTrackingSystemsTerm,
								validationResults.workTrackingSystems,
							)}
							{renderValidationSection(teamsTerm, validationResults.teams)}
							{renderValidationSection("Projects", validationResults.projects)}

							{hasValidationErrors() && (
								<Alert severity="error" sx={{ mt: 2 }}>
									There are validation errors in your configuration. Please fix
									them and try again.
								</Alert>
							)}
						</div>
					</LoadingAnimation>
				</Box>
			)}

			<Box
				sx={{
					display: "flex",
					justifyContent: "space-between",
					alignItems: "center",
					mt: 2,
					position: "sticky",
					bottom: 0,
					padding: 2,
					borderTop: (theme) => `1px solid ${theme.palette.divider}`,
					zIndex: 10,
				}}
			>
				<Box>
					<Button
						variant="outlined"
						color="secondary"
						onClick={onClose}
						disabled={isValidating}
					>
						Close
					</Button>
				</Box>
				<Box>
					<Button
						onClick={handleNextClick}
						color="primary"
						variant="contained"
						disabled={
							isValidating || !file || hasValidationErrors() || hasError
						}
						data-testid="next-button"
					>
						Next
					</Button>
				</Box>
			</Box>
		</>
	);
};

export default ImportSettingsStep;
