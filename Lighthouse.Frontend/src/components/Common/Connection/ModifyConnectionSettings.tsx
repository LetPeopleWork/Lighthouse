import {
	Alert,
	Container,
	FormControl,
	InputLabel,
	MenuItem,
	Select,
	type SelectChangeEvent,
	TextField,
	Typography,
} from "@mui/material";
import Grid from "@mui/material/Grid";
import type React from "react";
import { useCallback, useEffect, useMemo, useState } from "react";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import type { IAdditionalFieldDefinition } from "../../../models/WorkTracking/AdditionalFieldDefinition";
import type {
	IAuthenticationMethod,
	IWorkTrackingSystemConnection,
} from "../../../models/WorkTracking/WorkTrackingSystemConnection";
import type { IWorkTrackingSystemOption } from "../../../models/WorkTracking/WorkTrackingSystemOption";
import type { IWriteBackMappingDefinition } from "../../../models/WorkTracking/WriteBackMappingDefinition";
import AdditionalFieldsEditor from "../../../pages/Settings/Connections/AdditionalFieldsEditor";
import WriteBackMappingsEditor from "../../../pages/Settings/Connections/WriteBackMappingsEditor";
import { ApiError } from "../../../services/Api/ApiError";
import { useTerminology } from "../../../services/TerminologyContext";
import LoadingAnimation from "../LoadingAnimation/LoadingAnimation";
import ValidationActions from "../ValidationActions/ValidationActions";

interface ModifyConnectionSettingsProps {
	title: string;
	getSupportedSystems: () => Promise<IWorkTrackingSystemConnection[]>;
	getConnectionSettings: () => Promise<IWorkTrackingSystemConnection | null>;
	saveConnectionSettings: (
		connection: IWorkTrackingSystemConnection,
	) => Promise<void>;
	validateConnectionSettings: (
		connection: IWorkTrackingSystemConnection,
	) => Promise<boolean>;
	disableSave?: boolean;
	saveTooltip?: string;
}

const ModifyConnectionSettings: React.FC<ModifyConnectionSettingsProps> = ({
	title,
	getSupportedSystems,
	getConnectionSettings,
	saveConnectionSettings,
	validateConnectionSettings,
	disableSave = false,
	saveTooltip = "",
}) => {
	const [loading, setLoading] = useState(true);
	const [name, setName] = useState("");
	const [selectedWorkTrackingSystem, setSelectedWorkTrackingSystem] =
		useState<IWorkTrackingSystemConnection | null>(null);
	const [selectedAuthMethod, setSelectedAuthMethod] =
		useState<IAuthenticationMethod | null>(null);
	const [originalAuthMethodKey, setOriginalAuthMethodKey] = useState<
		string | null
	>(null);
	const [authOptions, setAuthOptions] = useState<IWorkTrackingSystemOption[]>(
		[],
	);
	const [otherOptions, setOtherOptions] = useState<IWorkTrackingSystemOption[]>(
		[],
	);
	const [additionalFields, setAdditionalFields] = useState<
		IAdditionalFieldDefinition[]
	>([]);
	const [writeBackMappings, setWriteBackMappings] = useState<
		IWriteBackMappingDefinition[]
	>([]);
	const [supportedSystems, setSupportedSystems] = useState<
		IWorkTrackingSystemConnection[]
	>([]);
	const [isEditMode, setIsEditMode] = useState(false);
	const [inputsValid, setInputsValid] = useState(false);
	const [validationKey, setValidationKey] = useState(0);
	const [fieldsModified, setFieldsModified] = useState(false);
	const [validationErrorMessage, setValidationErrorMessage] = useState<
		string | null
	>(null);

	const { getTerm } = useTerminology();
	const workTrackingSystemTerm = getTerm(TERMINOLOGY_KEYS.WORK_TRACKING_SYSTEM);

	const getAuthOptionKeys = useCallback(
		(authMethod: IAuthenticationMethod | null): Set<string> => {
			if (!authMethod) return new Set();
			return new Set(authMethod.options.map((opt) => opt.key));
		},
		[],
	);

	const getAllAuthOptionKeys = useCallback(
		(connection: IWorkTrackingSystemConnection | null): Set<string> => {
			if (!connection) return new Set();
			const allKeys = new Set<string>();
			for (const method of connection.availableAuthenticationMethods ?? []) {
				for (const option of method.options) {
					allKeys.add(option.key);
				}
			}
			return allKeys;
		},
		[],
	);

	const getEmptyAuthOptions = useCallback(
		(authMethod: IAuthenticationMethod | null): IWorkTrackingSystemOption[] => {
			if (!authMethod) return [];
			return authMethod.options.map((opt) => ({
				key: opt.key,
				value: "",
				isSecret: opt.isSecret,
				isOptional: opt.isOptional,
			}));
		},
		[],
	);

	const showAuthSection = useMemo(() => {
		const methods =
			selectedWorkTrackingSystem?.availableAuthenticationMethods ?? [];
		if (methods.length === 0) return false;
		if (methods.length === 1 && methods[0].options.length === 0) return false;
		return true;
	}, [selectedWorkTrackingSystem]);

	useEffect(() => {
		const fetchData = async () => {
			setLoading(true);
			try {
				const [systems, existingConnection] = await Promise.all([
					getSupportedSystems(),
					getConnectionSettings(),
				]);

				setSupportedSystems(systems);

				if (existingConnection) {
					// Edit mode
					setIsEditMode(true);
					setName(existingConnection.name);
					setSelectedWorkTrackingSystem(existingConnection);
					setOriginalAuthMethodKey(existingConnection.authenticationMethodKey);

					const availableMethods =
						existingConnection.availableAuthenticationMethods ?? [];
					const initialMethod =
						availableMethods.find(
							(m) => m.key === existingConnection.authenticationMethodKey,
						) ??
						availableMethods[0] ??
						null;
					setSelectedAuthMethod(initialMethod);

					const currentAuthKeys = getAuthOptionKeys(initialMethod);
					const allAuthKeys = getAllAuthOptionKeys(existingConnection);

					// Partition existing options
					const existingAuthOptions: IWorkTrackingSystemOption[] = [];
					const existingOtherOptions: IWorkTrackingSystemOption[] = [];

					for (const option of existingConnection.options) {
						const mappedOption: IWorkTrackingSystemOption = {
							key: option.key,
							value: option.value,
							isSecret: option.isSecret,
							isOptional: option.isOptional,
						};
						if (currentAuthKeys.has(option.key)) {
							existingAuthOptions.push(mappedOption);
						} else if (!allAuthKeys.has(option.key)) {
							existingOtherOptions.push(mappedOption);
						}
					}

					setAuthOptions(existingAuthOptions);
					setOtherOptions(existingOtherOptions);
					setAdditionalFields(
						existingConnection.additionalFieldDefinitions ?? [],
					);
					setWriteBackMappings(
						existingConnection.writeBackMappingDefinitions ?? [],
					);
				} else if (systems.length > 0) {
					// Create mode — select the first system as default
					setIsEditMode(false);
					const firstSystem = systems[0];
					setSelectedWorkTrackingSystem(firstSystem);
					setName(firstSystem.name);
					setOriginalAuthMethodKey(null);

					const availableMethods =
						firstSystem.availableAuthenticationMethods ?? [];
					const defaultMethod = availableMethods[0] ?? null;
					setSelectedAuthMethod(defaultMethod);

					setAuthOptions(getEmptyAuthOptions(defaultMethod));

					const allAuthKeys = getAllAuthOptionKeys(firstSystem);
					const nonAuthOptions = firstSystem.options
						.filter((opt) => !allAuthKeys.has(opt.key))
						.map((opt) => ({
							key: opt.key,
							value: opt.value,
							isSecret: opt.isSecret,
							isOptional: opt.isOptional,
						}));
					setOtherOptions(nonAuthOptions);
					setAdditionalFields(firstSystem.additionalFieldDefinitions ?? []);
					setWriteBackMappings(firstSystem.writeBackMappingDefinitions ?? []);
				}
			} catch (error) {
				console.error("Error fetching connection data:", error);
			} finally {
				setLoading(false);
			}
		};

		fetchData();
	}, [
		getSupportedSystems,
		getConnectionSettings,
		getAuthOptionKeys,
		getAllAuthOptionKeys,
		getEmptyAuthOptions,
	]);

	const allOptions = useMemo(
		() => [...authOptions, ...otherOptions],
		[authOptions, otherOptions],
	);

	useEffect(() => {
		const authMethodChanged =
			isEditMode &&
			originalAuthMethodKey !== null &&
			selectedAuthMethod?.key !== originalAuthMethodKey;

		const optionsValid = allOptions.every((option) => {
			if (
				isEditMode &&
				option.isSecret &&
				option.value === "" &&
				!authMethodChanged
			) {
				return true;
			}
			return option.isOptional || option.value !== "";
		});
		const nameValid = name !== "";

		setInputsValid(optionsValid && nameValid);
	}, [name, allOptions, isEditMode, originalAuthMethodKey, selectedAuthMethod]);

	const handleSystemChange = (event: SelectChangeEvent<string>) => {
		const system = supportedSystems.find(
			(s) => s.workTrackingSystem === event.target.value,
		);
		if (system) {
			setSelectedWorkTrackingSystem(system);
			setName(system.name);

			const availableMethods = system.availableAuthenticationMethods ?? [];
			const defaultMethod = availableMethods[0] ?? null;
			setSelectedAuthMethod(defaultMethod);

			const allAuthKeys = getAllAuthOptionKeys(system);
			setAuthOptions(getEmptyAuthOptions(defaultMethod));

			const nonAuthOptions = system.options
				.filter((opt) => !allAuthKeys.has(opt.key))
				.map((opt) => ({
					key: opt.key,
					value: opt.value,
					isSecret: opt.isSecret,
					isOptional: opt.isOptional,
				}));
			setOtherOptions(nonAuthOptions);
			setAdditionalFields(system.additionalFieldDefinitions ?? []);
			setWriteBackMappings(system.writeBackMappingDefinitions ?? []);
		}
	};

	const handleAuthMethodChange = (event: SelectChangeEvent<string>) => {
		const availableMethods =
			selectedWorkTrackingSystem?.availableAuthenticationMethods ?? [];
		const method = availableMethods.find((m) => m.key === event.target.value);
		if (method) {
			setSelectedAuthMethod(method);
			setAuthOptions(getEmptyAuthOptions(method));
		}
	};

	const handleAuthOptionChange = (
		changedOption: IWorkTrackingSystemOption,
		newValue: string,
	) => {
		setAuthOptions((prev) =>
			prev.map((option) =>
				option.key === changedOption.key
					? { ...option, value: newValue }
					: option,
			),
		);
	};

	const handleOtherOptionChange = (
		changedOption: IWorkTrackingSystemOption,
		newValue: string,
	) => {
		setOtherOptions((prev) =>
			prev.map((option) =>
				option.key === changedOption.key
					? { ...option, value: newValue }
					: option,
			),
		);
	};

	const handleNameChange = (event: React.ChangeEvent<HTMLInputElement>) => {
		setName(event.target.value);
	};

	const handleValidate = async () => {
		setValidationErrorMessage(null);

		if (selectedWorkTrackingSystem && selectedAuthMethod) {
			const settings: IWorkTrackingSystemConnection = {
				id: selectedWorkTrackingSystem.id,
				name,
				workTrackingSystem: selectedWorkTrackingSystem.workTrackingSystem,
				options: allOptions,
				authenticationMethodKey: selectedAuthMethod.key,
				workTrackingSystemGetDataRetrievalDisplayName:
					selectedWorkTrackingSystem.workTrackingSystemGetDataRetrievalDisplayName,
				additionalFieldDefinitions: additionalFields,
				writeBackMappingDefinitions: writeBackMappings,
			};

			try {
				return await validateConnectionSettings(settings);
			} catch (error) {
				if (error instanceof ApiError && error.code === 403) {
					setValidationErrorMessage(
						"You've exceeded the number of additional fields allowed on your plan.",
					);
				}
				return false;
			}
		}

		return false;
	};

	const handleSave = async () => {
		if (selectedWorkTrackingSystem && selectedAuthMethod) {
			const connection: IWorkTrackingSystemConnection = {
				id: selectedWorkTrackingSystem.id,
				name,
				workTrackingSystem: selectedWorkTrackingSystem.workTrackingSystem,
				options: allOptions,
				authenticationMethodKey: selectedAuthMethod.key,
				workTrackingSystemGetDataRetrievalDisplayName:
					selectedWorkTrackingSystem.workTrackingSystemGetDataRetrievalDisplayName,
				additionalFieldDefinitions: additionalFields,
				writeBackMappingDefinitions: writeBackMappings,
			};
			await saveConnectionSettings(connection);
		}
	};

	return (
		<LoadingAnimation isLoading={loading} hasError={false}>
			<Container maxWidth={false}>
				<Grid container spacing={3}>
					<Grid size={{ xs: 12 }}>
						<Typography variant="h4">{title}</Typography>
					</Grid>

					<Grid size={{ xs: 12, md: 6 }}>
						<TextField
							label="Connection Name"
							fullWidth
							value={name}
							onChange={handleNameChange}
						/>
					</Grid>

					{!isEditMode && (
						<Grid size={{ xs: 12, md: 6 }}>
							<FormControl fullWidth>
								<InputLabel>{`Select ${workTrackingSystemTerm}`}</InputLabel>
								<Select
									value={selectedWorkTrackingSystem?.workTrackingSystem ?? ""}
									onChange={handleSystemChange}
									label={`Select ${workTrackingSystemTerm}`}
								>
									{supportedSystems.map((system) => (
										<MenuItem
											key={system.workTrackingSystem}
											value={system.workTrackingSystem}
										>
											{system.workTrackingSystem}
										</MenuItem>
									))}
								</Select>
							</FormControl>
						</Grid>
					)}

					{isEditMode && (
						<Grid size={{ xs: 12, md: 6 }}>
							<TextField
								label={workTrackingSystemTerm}
								fullWidth
								disabled
								value={selectedWorkTrackingSystem?.workTrackingSystem ?? ""}
								slotProps={{ input: { readOnly: true } }}
							/>
						</Grid>
					)}

					{(selectedWorkTrackingSystem?.availableAuthenticationMethods
						?.length ?? 0) > 1 && (
						<Grid size={{ xs: 12, md: 6 }}>
							<FormControl fullWidth>
								<InputLabel>Authentication Method</InputLabel>
								<Select
									value={selectedAuthMethod?.key ?? ""}
									onChange={handleAuthMethodChange}
									label="Authentication Method"
								>
									{selectedWorkTrackingSystem?.availableAuthenticationMethods?.map(
										(method) => (
											<MenuItem key={method.key} value={method.key}>
												{method.displayName}
											</MenuItem>
										),
									)}
								</Select>
							</FormControl>
						</Grid>
					)}

					{/* Auth Options */}
					{showAuthSection && authOptions.length > 0 && (
						<Grid size={{ xs: 12 }}>
							<Typography
								variant="subtitle2"
								color="text.secondary"
								sx={{ mb: 1 }}
							>
								Authentication
							</Typography>
							<Grid container spacing={2}>
								{authOptions.map((option) => {
									const displayName =
										selectedAuthMethod?.options.find(
											(o) => o.key === option.key,
										)?.displayName ?? option.key;
									return (
										<Grid size={{ xs: 12, md: 6 }} key={option.key}>
											<TextField
												label={displayName}
												type={option.isSecret ? "password" : "text"}
												fullWidth
												value={option.value}
												placeholder={
													isEditMode && option.isSecret && option.value === ""
														? "Leave empty to keep existing value"
														: ""
												}
												onChange={(e) =>
													handleAuthOptionChange(option, e.target.value)
												}
											/>
										</Grid>
									);
								})}
							</Grid>
						</Grid>
					)}

					{/* Additional Fields */}
					<Grid size={{ xs: 12 }}>
						<AdditionalFieldsEditor
							workTrackingSystemType={
								selectedWorkTrackingSystem
									? selectedWorkTrackingSystem.workTrackingSystem
									: null
							}
							fields={additionalFields}
							onChange={setAdditionalFields}
							onFieldsChanged={() => {
								setValidationKey((prev) => prev + 1);
								setFieldsModified(true);
							}}
						/>
					</Grid>

					{fieldsModified && (
						<Grid size={{ xs: 12 }}>
							<Alert severity="info">
								Additional Fields will be loaded with the next refresh of data.
							</Alert>
						</Grid>
					)}

					{/* Sync with Source (Write-Back Mappings) */}
					<Grid size={{ xs: 12 }}>
						<WriteBackMappingsEditor
							additionalFields={additionalFields}
							mappings={writeBackMappings}
							onChange={setWriteBackMappings}
							workTrackingSystemType={
								selectedWorkTrackingSystem
									? selectedWorkTrackingSystem.workTrackingSystem
									: null
							}
						/>
					</Grid>

					{/* Other Options */}
					{otherOptions.length > 0 && (
						<Grid size={{ xs: 12 }}>
							<Typography
								variant="subtitle2"
								color="text.secondary"
								sx={{ mb: 1 }}
							>
								Options
							</Typography>
							<Grid container spacing={2}>
								{otherOptions.map((option) => (
									<Grid size={{ xs: 12, md: 6 }} key={option.key}>
										<TextField
											label={option.key}
											type={option.isSecret ? "password" : "text"}
											fullWidth
											value={option.value}
											onChange={(e) =>
												handleOtherOptionChange(option, e.target.value)
											}
										/>
									</Grid>
								))}
							</Grid>
						</Grid>
					)}

					{/* Validation + Save Actions */}
					<Grid
						size={{ xs: 12 }}
						sx={{
							display: "flex",
							gap: 2,
							justifyContent: "flex-end",
						}}
					>
						<ValidationActions
							onValidate={handleValidate}
							onSave={handleSave}
							inputsValid={inputsValid}
							validationFailedMessage={
								validationErrorMessage ??
								`Could not connect to the ${workTrackingSystemTerm} with the provided settings. Please review and try again.`
							}
							disableSave={disableSave}
							saveTooltip={saveTooltip}
							key={validationKey}
						/>
					</Grid>
				</Grid>
			</Container>
		</LoadingAnimation>
	);
};

export default ModifyConnectionSettings;
