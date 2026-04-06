import {
	Alert,
	Box,
	Button,
	FormControl,
	InputLabel,
	MenuItem,
	Select,
	type SelectChangeEvent,
	Step,
	StepLabel,
	Stepper,
	TextField,
	Typography,
} from "@mui/material";
import type React from "react";
import { useCallback, useEffect, useMemo, useState } from "react";
import type {
	IAuthenticationMethod,
	IWorkTrackingSystemConnection,
} from "../../../models/WorkTracking/WorkTrackingSystemConnection";
import type { IWorkTrackingSystemOption } from "../../../models/WorkTracking/WorkTrackingSystemOption";
import { ApiError } from "../../../services/Api/ApiError";
import ActionButton from "../ActionButton/ActionButton";
import LoadingAnimation from "../LoadingAnimation/LoadingAnimation";

const STEPS = ["Choose Type", "Configuration", "Name & Create"];

interface CreateConnectionWizardProps {
	getSupportedSystems: () => Promise<IWorkTrackingSystemConnection[]>;
	validateConnection: (
		connection: IWorkTrackingSystemConnection,
	) => Promise<boolean>;
	saveConnection: (connection: IWorkTrackingSystemConnection) => Promise<void>;
	onCancel: () => void;
}

const CreateConnectionWizard: React.FC<CreateConnectionWizardProps> = ({
	getSupportedSystems,
	validateConnection,
	saveConnection,
	onCancel,
}) => {
	const [activeStep, setActiveStep] = useState(0);
	const [loading, setLoading] = useState(true);
	const [supportedSystems, setSupportedSystems] = useState<
		IWorkTrackingSystemConnection[]
	>([]);
	const [selectedSystem, setSelectedSystem] =
		useState<IWorkTrackingSystemConnection | null>(null);
	const [selectedAuthMethod, setSelectedAuthMethod] =
		useState<IAuthenticationMethod | null>(null);
	const [authOptions, setAuthOptions] = useState<IWorkTrackingSystemOption[]>(
		[],
	);
	const [requiredOtherOptions, setRequiredOtherOptions] = useState<
		IWorkTrackingSystemOption[]
	>([]);
	const [optionalOtherOptions, setOptionalOtherOptions] = useState<
		IWorkTrackingSystemOption[]
	>([]);
	const [connectionName, setConnectionName] = useState("");
	const [validating, setValidating] = useState(false);
	const [validationError, setValidationError] = useState<string | null>(null);
	const [validationTechnicalDetails, setValidationTechnicalDetails] = useState<
		string | null
	>(null);
	const [saving, setSaving] = useState(false);

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

	useEffect(() => {
		const fetchSystems = async () => {
			setLoading(true);
			try {
				const systems = await getSupportedSystems();
				setSupportedSystems(systems);
			} finally {
				setLoading(false);
			}
		};
		fetchSystems();
	}, [getSupportedSystems]);

	const selectSystem = (system: IWorkTrackingSystemConnection) => {
		setSelectedSystem(system);
		setConnectionName(system.name);
		setValidationError(null);
		setValidationTechnicalDetails(null);

		const availableMethods = system.availableAuthenticationMethods ?? [];
		const defaultMethod = availableMethods[0] ?? null;
		setSelectedAuthMethod(defaultMethod);

		// Set up auth options (empty values for user input)
		if (defaultMethod) {
			setAuthOptions(
				defaultMethod.options.map((opt) => ({
					key: opt.key,
					value: "",
					isSecret: opt.isSecret,
					isOptional: opt.isOptional,
				})),
			);
		} else {
			setAuthOptions([]);
		}

		// Set up non-auth options, split into required and optional
		const allAuthKeys = getAllAuthOptionKeys(system);
		const nonAuthOptions = system.options
			.filter((opt) => !allAuthKeys.has(opt.key))
			.map((opt) => ({
				key: opt.key,
				value: opt.value,
				isSecret: opt.isSecret,
				isOptional: opt.isOptional,
			}));
		setRequiredOtherOptions(nonAuthOptions.filter((opt) => !opt.isOptional));
		setOptionalOtherOptions(nonAuthOptions.filter((opt) => opt.isOptional));

		setActiveStep(1);
	};

	const handleAuthMethodChange = (event: SelectChangeEvent<string>) => {
		const availableMethods =
			selectedSystem?.availableAuthenticationMethods ?? [];
		const method = availableMethods.find((m) => m.key === event.target.value);
		if (method) {
			setSelectedAuthMethod(method);
			setAuthOptions(
				method.options.map((opt) => ({
					key: opt.key,
					value: "",
					isSecret: opt.isSecret,
					isOptional: opt.isOptional,
				})),
			);
		}
	};

	const handleAuthOptionChange = (key: string, value: string) => {
		setAuthOptions((prev) =>
			prev.map((opt) => (opt.key === key ? { ...opt, value } : opt)),
		);
	};

	const handleRequiredOtherOptionChange = (key: string, value: string) => {
		setRequiredOtherOptions((prev) =>
			prev.map((opt) => (opt.key === key ? { ...opt, value } : opt)),
		);
	};

	const allOptions = useMemo(
		() => [...authOptions, ...requiredOtherOptions, ...optionalOtherOptions],
		[authOptions, requiredOtherOptions, optionalOtherOptions],
	);

	const configInputsValid = useMemo(() => {
		const authValid = authOptions.every(
			(opt) => opt.isOptional || opt.value !== "",
		);
		const requiredOtherValid = requiredOtherOptions.every(
			(opt) => opt.value !== "",
		);
		return authValid && requiredOtherValid;
	}, [authOptions, requiredOtherOptions]);

	const buildConnectionDto =
		useCallback((): IWorkTrackingSystemConnection | null => {
			if (!selectedSystem || !selectedAuthMethod) return null;
			return {
				id: 0,
				name: connectionName,
				workTrackingSystem: selectedSystem.workTrackingSystem,
				options: allOptions,
				authenticationMethodKey: selectedAuthMethod.key,
				workTrackingSystemGetDataRetrievalDisplayName:
					selectedSystem.workTrackingSystemGetDataRetrievalDisplayName,
				additionalFieldDefinitions: [],
				writeBackMappingDefinitions: [],
			};
		}, [selectedSystem, selectedAuthMethod, connectionName, allOptions]);

	const runValidation = useCallback(async (): Promise<boolean> => {
		const dto = buildConnectionDto();
		if (!dto) return false;

		return await validateConnection(dto);
	}, [buildConnectionDto, validateConnection]);

	const handleCreate = async () => {
		const dto = buildConnectionDto();
		if (!dto) return;
		setSaving(true);
		try {
			await saveConnection(dto);
		} finally {
			setSaving(false);
		}
	};

	const handleNext = async () => {
		if (activeStep === 1) {
			setValidating(true);
			setValidationError(null);
			setValidationTechnicalDetails(null);
			try {
				const isValid = await runValidation();
				if (isValid) {
					setActiveStep(2);
				} else {
					setValidationError(
						"Could not validate the connection. Check your settings and try again.",
					);
				}
			} catch (error) {
				if (error instanceof ApiError && error.code !== 403) {
					setValidationError(error.message);
					setValidationTechnicalDetails(error.technicalDetails ?? null);
				} else {
					setValidationError(
						"Could not validate the connection. Check your settings and try again.",
					);
				}
			} finally {
				setValidating(false);
			}
		}
	};

	const handleBack = () => {
		setValidationError(null);
		setValidationTechnicalDetails(null);
		setActiveStep((prev) => prev - 1);
	};

	const showAuthMethodSelector =
		(selectedSystem?.availableAuthenticationMethods?.length ?? 0) > 1;

	const renderStep1 = () => (
		<Box sx={{ display: "flex", flexDirection: "column", gap: 2, mt: 2 }}>
			<Typography variant="body1">
				Select the type of work tracking system to connect:
			</Typography>
			{supportedSystems.map((system) => (
				<Button
					key={system.workTrackingSystem}
					variant="outlined"
					onClick={() => selectSystem(system)}
					sx={{ justifyContent: "flex-start", textTransform: "none" }}
				>
					{system.name}
				</Button>
			))}
		</Box>
	);

	const renderStep2 = () => (
		<Box sx={{ display: "flex", flexDirection: "column", gap: 2, mt: 2 }}>
			{showAuthMethodSelector && (
				<FormControl fullWidth>
					<InputLabel>Authentication Method</InputLabel>
					<Select
						value={selectedAuthMethod?.key ?? ""}
						onChange={handleAuthMethodChange}
						label="Authentication Method"
					>
						{selectedSystem?.availableAuthenticationMethods?.map((method) => (
							<MenuItem key={method.key} value={method.key}>
								{method.displayName}
							</MenuItem>
						))}
					</Select>
				</FormControl>
			)}

			{authOptions.map((option) => {
				const displayName =
					selectedAuthMethod?.options.find((o) => o.key === option.key)
						?.displayName ?? option.key;
				return (
					<TextField
						key={option.key}
						label={displayName}
						type={option.isSecret ? "password" : "text"}
						fullWidth
						value={option.value}
						onChange={(e) => handleAuthOptionChange(option.key, e.target.value)}
					/>
				);
			})}

			{requiredOtherOptions.map((option) => (
				<TextField
					key={option.key}
					label={option.key}
					type={option.isSecret ? "password" : "text"}
					fullWidth
					value={option.value}
					onChange={(e) =>
						handleRequiredOtherOptionChange(option.key, e.target.value)
					}
				/>
			))}

			{validationError && (
				<Alert severity="error" sx={{ width: "100%" }}>
					<Typography variant="body2">{validationError}</Typography>
					{validationTechnicalDetails && (
						<Typography variant="caption" sx={{ display: "block", mt: 1 }}>
							{validationTechnicalDetails}
						</Typography>
					)}
				</Alert>
			)}
		</Box>
	);

	const renderStep3 = () => (
		<Box sx={{ display: "flex", flexDirection: "column", gap: 2, mt: 2 }}>
			<TextField
				label="Connection Name"
				fullWidth
				value={connectionName}
				onChange={(e) => setConnectionName(e.target.value)}
			/>
		</Box>
	);

	const renderStepContent = () => {
		switch (activeStep) {
			case 0:
				return renderStep1();
			case 1:
				return renderStep2();
			case 2:
				return renderStep3();
			default:
				return null;
		}
	};

	const isNextDisabled = () => {
		if (activeStep === 1) return !configInputsValid || validating;
		return false;
	};

	return (
		<LoadingAnimation isLoading={loading} hasError={false}>
			<Box sx={{ width: "100%", p: 3 }}>
				<Stepper activeStep={activeStep} alternativeLabel>
					{STEPS.map((label) => (
						<Step key={label}>
							<StepLabel>{label}</StepLabel>
						</Step>
					))}
				</Stepper>

				{renderStepContent()}

				<Box
					sx={{
						display: "flex",
						justifyContent: "flex-end",
						gap: 1,
						mt: 3,
					}}
				>
					<Button variant="outlined" onClick={onCancel}>
						Cancel
					</Button>

					{activeStep > 0 && (
						<Button variant="outlined" onClick={handleBack}>
							Back
						</Button>
					)}

					{activeStep === 1 && (
						<Button
							variant="contained"
							onClick={handleNext}
							disabled={isNextDisabled()}
						>
							{validating ? "Validating..." : "Next"}
						</Button>
					)}

					{activeStep === 2 && (
						<ActionButton
							buttonText="Create"
							onClickHandler={handleCreate}
							buttonVariant="contained"
							disabled={connectionName === "" || saving}
						/>
					)}
				</Box>
			</Box>
		</LoadingAnimation>
	);
};

export default CreateConnectionWizard;
