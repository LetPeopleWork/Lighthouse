import BiotechIcon from "@mui/icons-material/Biotech";
import Alert from "@mui/material/Alert";
import Box from "@mui/material/Box";
import Chip from "@mui/material/Chip";
import Container from "@mui/material/Container";
import Grid from "@mui/material/Grid";
import Switch from "@mui/material/Switch";
import Table from "@mui/material/Table";
import TableBody from "@mui/material/TableBody";
import TableCell from "@mui/material/TableCell";
import TableContainer from "@mui/material/TableContainer";
import TableHead from "@mui/material/TableHead";
import TableRow from "@mui/material/TableRow";
import TextField from "@mui/material/TextField";
import Tooltip from "@mui/material/Tooltip";
import type React from "react";
import { useCallback, useContext, useEffect, useState } from "react";
import ActionButton from "../../../components/Common/ActionButton/ActionButton";
import InputGroup from "../../../components/Common/InputGroup/InputGroup";
import { TerminologyConfiguration } from "../../../components/TerminologyConfiguration";
import type { IDataRetentionSettings } from "../../../models/AppSettings/DataRetentionSettings";
import type { ILicenseStatus } from "../../../models/ILicenseStatus";
import type { IOptionalFeature } from "../../../models/OptionalFeatures/OptionalFeature";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import { useTerminology } from "../../../services/TerminologyContext";
import RefreshSettingUpdater from "../Refresh/RefreshSettingUpdater";
import ImportConfigurationDialog from "./ImportConfiguration/ImportConfigurationDialog";

const SystemSettingsTab: React.FC = () => {
	// Data Retention state
	const [dataRetentionSettings, setDataRetentionSettings] =
		useState<IDataRetentionSettings | null>(null);

	// Optional Features state
	const [optionalFeatures, setOptionalFeatures] = useState<IOptionalFeature[]>(
		[],
	);

	// Import Configuration Dialog state
	const [importDialogOpen, setImportDialogOpen] = useState(false);

	// License status state
	const [licenseStatus, setLicenseStatus] = useState<ILicenseStatus | null>(
		null,
	);

	const {
		settingsService,
		optionalFeatureService,
		configurationService,
		licensingService,
	} = useContext(ApiServiceContext);

	// Data Retention functions
	const fetchDataRetentionSettings = useCallback(async () => {
		const loadedSettings = await settingsService.getDataRetentionSettings();
		setDataRetentionSettings(loadedSettings);
	}, [settingsService]);

	const updateDataRetentionSettings = async () => {
		if (dataRetentionSettings == null) {
			return;
		}

		await settingsService.updateDataRetentionSettings(dataRetentionSettings);
	};

	const { getTerm } = useTerminology();
	const featureTerm = getTerm(TERMINOLOGY_KEYS.FEATURE);
	const featuresTerm = getTerm(TERMINOLOGY_KEYS.FEATURES);
	const teamTerm = getTerm(TERMINOLOGY_KEYS.TEAM);

	const handleInputChange = (event: React.ChangeEvent<HTMLInputElement>) => {
		if (dataRetentionSettings) {
			setDataRetentionSettings({
				...dataRetentionSettings,
				maxStorageTimeInDays: Number.parseInt(event.target.value, 10),
			});
		}
	};

	// Optional Features functions
	const fetchOptionalFeatures = useCallback(async () => {
		const optionalFeatureData = await optionalFeatureService.getAllFeatures();
		if (optionalFeatureData) {
			setOptionalFeatures(optionalFeatureData);
		}
	}, [optionalFeatureService]);

	// License status functions
	const fetchLicenseStatus = useCallback(async () => {
		try {
			const licenseData = await licensingService.getLicenseStatus();
			setLicenseStatus(licenseData);
		} catch (error) {
			console.error("Failed to fetch license status:", error);
			setLicenseStatus(null);
		}
	}, [licensingService]);

	const onExportConfiguration = async () => {
		await configurationService.exportConfiguration();
	};

	const onOpenImportDialog = () => {
		setImportDialogOpen(true);
	};

	const onCloseImportDialog = () => {
		setImportDialogOpen(false);
	};

	const onToggleOptionalFeature = async (toggledFeature: IOptionalFeature) => {
		const updatedFeatures = optionalFeatures.map((feature) =>
			feature.id === toggledFeature.id
				? { ...feature, enabled: !feature.enabled }
				: feature,
		);
		setOptionalFeatures(updatedFeatures);

		try {
			await optionalFeatureService.updateFeature({
				...toggledFeature,
				enabled: !toggledFeature.enabled,
			});
		} catch {
			// Revert the local state in case of error
			await fetchOptionalFeatures();
		}
	};

	useEffect(() => {
		fetchDataRetentionSettings();
		fetchOptionalFeatures();
		fetchLicenseStatus();
	}, [fetchDataRetentionSettings, fetchOptionalFeatures, fetchLicenseStatus]);

	return (
		<Box sx={{ mb: 4 }}>
			<ImportConfigurationDialog
				open={importDialogOpen}
				onClose={onCloseImportDialog}
			/>
			<InputGroup title="Lighthouse Configuration" initiallyExpanded={true}>
				{licenseStatus?.canUsePremiumFeatures ? (
					<Box sx={{ display: "flex", gap: 2 }}>
						<ActionButton
							buttonVariant="contained"
							onClickHandler={onExportConfiguration}
							buttonText="Export Configuration"
						/>
						<ActionButton
							buttonVariant="contained"
							onClickHandler={() => {
								onOpenImportDialog();
								return Promise.resolve();
							}}
							buttonText="Import Configuration"
						/>
					</Box>
				) : (
					<Alert severity="info" sx={{ m: 2 }}>
						Configuration export and import are premium features. Please obtain
						a valid license to access these features.
					</Alert>
				)}
			</InputGroup>

			<InputGroup title="Optional Features" initiallyExpanded={true}>
				<TableContainer>
					<Table data-testid="optional-features-table">
						<TableHead>
							<TableRow>
								<TableCell>Name</TableCell>
								<TableCell>Description</TableCell>
								<TableCell>Enabled</TableCell>
							</TableRow>
						</TableHead>
						<TableBody>
							{optionalFeatures.map((feature) => (
								<TableRow
									key={feature.id}
									data-testid={`feature-row-${feature.key}`}
								>
									<TableCell>
										<Box sx={{ display: "flex", alignItems: "center" }}>
											{feature.name}
											{feature.isPreview && (
												<Tooltip title="This feature is in preview and may change or be removed in future versions">
													<Chip
														icon={<BiotechIcon />}
														label="Preview"
														size="small"
														color="warning"
														sx={{ ml: 1 }}
														data-testid={`${feature.key}-preview-indicator`}
													/>
												</Tooltip>
											)}
										</Box>
									</TableCell>
									<TableCell>{feature.description}</TableCell>
									<TableCell>
										<Switch
											checked={feature.enabled}
											data-testid={`${feature.key}-toggle`}
											onChange={() => onToggleOptionalFeature(feature)}
											color="primary"
										/>
									</TableCell>
								</TableRow>
							))}
						</TableBody>
					</Table>
				</TableContainer>
			</InputGroup>

			<InputGroup title="Terminology Configuration" initiallyExpanded={true}>
				<TerminologyConfiguration />
			</InputGroup>

			<InputGroup title={`${teamTerm} Refresh`}>
				<RefreshSettingUpdater title={teamTerm} settingName="Team" />
			</InputGroup>
			<InputGroup title={`${featureTerm} Refresh`}>
				<RefreshSettingUpdater title={featureTerm} settingName="Feature" />
			</InputGroup>

			<InputGroup title="Data Retention Settings" initiallyExpanded={true}>
				<Container maxWidth={false}>
					<Grid container spacing={3}>
						<Grid size={{ xs: 12 }}>
							<TextField
								label="Maximum Data Retention Time (Days)"
								type="number"
								value={dataRetentionSettings?.maxStorageTimeInDays ?? ""}
								onChange={handleInputChange}
								fullWidth
								slotProps={{
									htmlInput: {
										min: 30,
									},
								}}
								helperText={`After this many days the archived data for ${featuresTerm} is removed.`}
								data-testid="data-retention-days-input"
							/>
						</Grid>
						<Grid size={{ xs: 12 }}>
							<ActionButton
								buttonVariant="contained"
								onClickHandler={updateDataRetentionSettings}
								buttonText="Update Data Retention Settings"
								data-testid="update-data-retention-button"
							/>
						</Grid>
					</Grid>
				</Container>
			</InputGroup>
		</Box>
	);
};

export default SystemSettingsTab;
