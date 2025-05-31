import WarningIcon from "@mui/icons-material/Warning";
import {
	Alert,
	Button,
	Checkbox,
	Chip,
	Dialog,
	DialogActions,
	DialogContent,
	DialogTitle,
	FormControlLabel,
	Tooltip,
	Typography,
} from "@mui/material";
import Grid from "@mui/material/Grid";
import type React from "react";
import { useCallback, useContext, useEffect, useState } from "react";
import LoadingAnimation from "../../../../components/Common/LoadingAnimation/LoadingAnimation";
import type { ConfigurationExport } from "../../../../models/Configuration/ConfigurationExport";
import type {
	ConfigurationValidation,
	ConfigurationValidationItem,
} from "../../../../models/Configuration/ConfigurationValidation";
import { ApiServiceContext } from "../../../../services/Api/ApiServiceContext";

interface ImportConfigurationDialogProps {
	open: boolean;
	onClose: () => void;
}

const ImportConfigurationDialog: React.FC<ImportConfigurationDialogProps> = ({
	open,
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
	const [isImporting, setIsImporting] = useState<boolean>(false);
	const [clearConfiguration] = useState<boolean>(true);

	const { configurationService } = useContext(ApiServiceContext);

	const resetDialogState = () => {
		setFile(null);
		setParsedConfig(null);
		setValidationResults(null);
		setIsValidating(false);
		setHasError(false);
		setErrorMessage("");
		setIsImporting(false);
	};

	const handleCloseDialog = () => {
		resetDialogState();
		onClose();
	};

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

	// Transform status from "Update" to "New" if clearConfiguration is checked
	const transformStatus = useCallback(
		(items: ConfigurationValidationItem[]): ConfigurationValidationItem[] => {
			return items.map((item) => ({
				...item,
				// Only change "Update" to "New", keep "Error" as is
				status: item.status === "Update" ? "New" : item.status,
			}));
		},
		[],
	);

	useEffect(() => {
		const validateFile = async () => {
			if (!file) {
				return;
			}

			setIsValidating(true);
			setHasError(false);
			setErrorMessage("");

			try {
				// Read the file content as text
				const fileContent = await file.text();

				try {
					// Parse the file content as JSON
					const configData = JSON.parse(fileContent) as ConfigurationExport;
					setParsedConfig(configData);

					// Validate the configuration
					const validation =
						await configurationService.validateConfiguration(configData);

					// If clearConfiguration is checked, transform all "Update" status to "New"
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

	const handleImportClick = async () => {
		if (!parsedConfig || hasValidationErrors()) return;

		setIsImporting(true);
		try {
			// In a real implementation, we would call configurationService.importConfiguration(parsedConfig)
			// For now, let's simulate a successful import with a delay
			await new Promise((resolve) => setTimeout(resolve, 1000));
			handleCloseDialog();
		} catch (error: unknown) {
			setHasError(true);
			const errorMessage =
				error instanceof Error
					? error.message
					: "Error importing configuration. Please try again.";
			setErrorMessage(errorMessage);
		} finally {
			setIsImporting(false);
		}
	};

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
					<Typography variant="body2">No items to validate</Typography>
				) : (
					items.map((item) => (
						<Grid container key={item.id} sx={{ my: 0.5 }}>
							<Grid size={{ xs: 6 }}>
								<Typography variant="body2">{item.name}</Typography>
							</Grid>
							<Grid size={{ xs: 6 }}>
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

	return (
		<Dialog
			open={open}
			onClose={handleCloseDialog}
			aria-labelledby="import-configuration-title"
			data-testid="import-configuration-dialog"
			maxWidth="md"
			fullWidth
		>
			<DialogTitle id="import-configuration-title">
				Import Configuration
			</DialogTitle>
			<DialogContent>
				{/* File selection */}
				<Grid container spacing={2} sx={{ mb: 2 }}>
					<Grid size={{ xs: 12 }}>
						<input
							type="file"
							id="configuration-file"
							accept=".json"
							onChange={handleFileChange}
							style={{ display: "none" }}
							data-testid="file-input"
						/>
						<label htmlFor="configuration-file">
							<Button
								variant="contained"
								component="span"
								disabled={isValidating || isImporting}
							>
								Select Configuration File
							</Button>
						</label>
						<FormControlLabel
							control={
								<Checkbox
									checked={clearConfiguration}
									disabled={true} // Read-only for now
									data-testid="clear-configuration-checkbox"
								/>
							}
							label={
								<div style={{ display: "flex", alignItems: "center" }}>
									Clear Configuration
									<Tooltip
										title="If this option is selected, all existing work tracking systems, teams, and projects will be removed"
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
						{file && (
							<>
								<Typography variant="body1" sx={{ mt: 1 }}>
									Selected Configuration File:
								</Typography>
								<Typography variant="body2">{file.name}</Typography>
							</>
						)}
					</Grid>
				</Grid>

				{/* Error messages */}
				{hasError && (
					<Alert severity="error" sx={{ mb: 2 }}>
						{errorMessage}
					</Alert>
				)}

				<LoadingAnimation isLoading={isValidating} hasError={false}>
					{validationResults && (
						<div data-testid="validation-results">
							<Typography variant="h6" sx={{ mb: 1 }}>
								Configuration Details
							</Typography>

							{renderValidationSection(
								"Work Tracking Systems",
								validationResults.workTrackingSystems,
							)}
							{renderValidationSection("Teams", validationResults.teams)}
							{renderValidationSection("Projects", validationResults.projects)}

							{hasValidationErrors() && (
								<Alert severity="error" sx={{ mt: 2 }}>
									There are validation errors in your configuration. Please fix
									them and try again.
								</Alert>
							)}
						</div>
					)}
				</LoadingAnimation>
			</DialogContent>
			<DialogActions>
				<Button
					onClick={handleCloseDialog}
					color="primary"
					disabled={isValidating || isImporting}
				>
					Cancel
				</Button>
				{validationResults && !hasValidationErrors() && (
					<Button
						onClick={handleImportClick}
						color="primary"
						variant="contained"
						disabled={isImporting}
						data-testid="import-button"
					>
						{isImporting ? "Importing..." : "Import"}
					</Button>
				)}
			</DialogActions>
		</Dialog>
	);
};

export default ImportConfigurationDialog;
