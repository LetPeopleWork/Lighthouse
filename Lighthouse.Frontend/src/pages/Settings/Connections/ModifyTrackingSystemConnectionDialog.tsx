import {
	Box,
	Dialog,
	DialogActions,
	DialogContent,
	DialogTitle,
	FormControl,
	InputLabel,
	MenuItem,
	Select,
	type SelectChangeEvent,
	TextField,
	Typography,
} from "@mui/material";
import type React from "react";
import { useCallback, useEffect, useMemo, useState } from "react";
import ValidationActions from "../../../components/Common/ValidationActions/ValidationActions";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import type { IAdditionalFieldDefinition } from "../../../models/WorkTracking/AdditionalFieldDefinition";
import type {
	IAuthenticationMethod,
	IWorkTrackingSystemConnection,
} from "../../../models/WorkTracking/WorkTrackingSystemConnection";
import type { IWorkTrackingSystemOption } from "../../../models/WorkTracking/WorkTrackingSystemOption";
import { useTerminology } from "../../../services/TerminologyContext";
import AdditionalFieldsEditor from "./AdditionalFieldsEditor";

interface ModifyWorkTrackingSystemConnectionDialogProps {
	open: boolean;
	onClose: (value: IWorkTrackingSystemConnection | null) => void;
	workTrackingSystems: IWorkTrackingSystemConnection[];
	validateSettings: (
		connection: IWorkTrackingSystemConnection,
	) => Promise<boolean>;
}

const ModifyTrackingSystemConnectionDialog: React.FC<
	ModifyWorkTrackingSystemConnectionDialogProps
> = ({ open, onClose, workTrackingSystems, validateSettings }) => {
	const [name, setName] = useState<string>("");
	const [selectedWorkTrackingSystem, setSelectedWorkTrackingSystem] =
		useState<IWorkTrackingSystemConnection | null>(null);
	const [selectedAuthMethod, setSelectedAuthMethod] =
		useState<IAuthenticationMethod | null>(null);

	const [authOptions, setAuthOptions] = useState<IWorkTrackingSystemOption[]>(
		[],
	);

	const [otherOptions, setOtherOptions] = useState<IWorkTrackingSystemOption[]>(
		[],
	);
	const [additionalFields, setAdditionalFields] = useState<
		IAdditionalFieldDefinition[]
	>([]);
	const [inputsValid, setInputsValid] = useState<boolean>(false);

	const { getTerm } = useTerminology();
	const workTrackingSystemTerm = getTerm(TERMINOLOGY_KEYS.WORK_TRACKING_SYSTEM);

	const getAuthOptionKeys = useCallback(
		(authMethod: IAuthenticationMethod | null): Set<string> => {
			if (!authMethod) return new Set();
			return new Set(authMethod.options.map((opt) => opt.key));
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
		if (open && workTrackingSystems.length > 0) {
			const firstSystem = workTrackingSystems[0];
			setSelectedWorkTrackingSystem(firstSystem);
			setName(firstSystem.name);

			const availableMethods = firstSystem.availableAuthenticationMethods ?? [];
			const initialMethod =
				availableMethods.find(
					(m) => m.key === firstSystem.authenticationMethodKey,
				) ??
				availableMethods[0] ??
				null;

			setSelectedAuthMethod(initialMethod);

			const authKeys = getAuthOptionKeys(initialMethod);

			if (workTrackingSystems.length === 1) {
				const existingAuthOptions: IWorkTrackingSystemOption[] = [];
				const existingOtherOptions: IWorkTrackingSystemOption[] = [];

				for (const option of firstSystem.options) {
					const mappedOption: IWorkTrackingSystemOption = {
						key: option.key,
						value: option.value,
						isSecret: option.isSecret,
						isOptional: option.isOptional,
					};
					if (authKeys.has(option.key)) {
						existingAuthOptions.push(mappedOption);
					} else {
						existingOtherOptions.push(mappedOption);
					}
				}

				setAuthOptions(existingAuthOptions);
				setOtherOptions(existingOtherOptions);
				setAdditionalFields(firstSystem.additionalFieldDefinitions ?? []);
			} else {
				setAuthOptions(getEmptyAuthOptions(initialMethod));

				const nonAuthOptions = firstSystem.options
					.filter((opt) => !authKeys.has(opt.key))
					.map((opt) => ({
						key: opt.key,
						value: opt.value,
						isSecret: opt.isSecret,
						isOptional: opt.isOptional,
					}));
				setOtherOptions(nonAuthOptions);
				setAdditionalFields(firstSystem.additionalFieldDefinitions ?? []);
			}
		}
	}, [open, workTrackingSystems, getAuthOptionKeys, getEmptyAuthOptions]);

	const allOptions = useMemo(
		() => [...authOptions, ...otherOptions],
		[authOptions, otherOptions],
	);

	useEffect(() => {
		const optionsValid = allOptions.every(
			(option) => option.isOptional || option.value !== "",
		);
		const nameValid = name !== "";

		setInputsValid(optionsValid && nameValid);
	}, [name, allOptions]);

	const handleSystemChange = (event: SelectChangeEvent<string>) => {
		const system = workTrackingSystems.find(
			(system) => system.workTrackingSystem === event.target.value,
		);
		if (system) {
			setSelectedWorkTrackingSystem(system);
			setName(system.name);

			const availableMethods = system.availableAuthenticationMethods ?? [];
			const defaultMethod = availableMethods[0] ?? null;
			setSelectedAuthMethod(defaultMethod);

			const authKeys = getAuthOptionKeys(defaultMethod);

			setAuthOptions(getEmptyAuthOptions(defaultMethod));

			const nonAuthOptions = system.options
				.filter((opt) => !authKeys.has(opt.key))
				.map((opt) => ({
					key: opt.key,
					value: opt.value,
					isSecret: opt.isSecret,
					isOptional: opt.isOptional,
				}));
			setOtherOptions(nonAuthOptions);
			setAdditionalFields(system.additionalFieldDefinitions ?? []);
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
		setAuthOptions((prevOptions) =>
			prevOptions.map((option) =>
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
		setOtherOptions((prevOptions) =>
			prevOptions.map((option) =>
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
			};

			return await validateSettings(settings);
		}

		return false;
	};

	const handleSubmit = () => {
		if (selectedWorkTrackingSystem && selectedAuthMethod) {
			const updatedSystem: IWorkTrackingSystemConnection = {
				id: selectedWorkTrackingSystem.id,
				name: name,
				workTrackingSystem: selectedWorkTrackingSystem.workTrackingSystem,
				options: allOptions,
				authenticationMethodKey: selectedAuthMethod.key,
				workTrackingSystemGetDataRetrievalDisplayName:
					selectedWorkTrackingSystem.workTrackingSystemGetDataRetrievalDisplayName,
				additionalFieldDefinitions: additionalFields,
			};
			onClose(updatedSystem);
		} else {
			onClose(null);
		}
	};

	const handleClose = () => {
		onClose(null);
	};

	return (
		<Dialog onClose={handleClose} open={open} fullWidth>
			<DialogTitle>
				{workTrackingSystems.length === 1
					? "Edit Connection"
					: "Create New Connection"}
			</DialogTitle>
			<DialogContent>
				<TextField
					label="Connection Name"
					fullWidth
					margin="normal"
					value={name}
					onChange={handleNameChange}
				/>

				<FormControl fullWidth margin="normal">
					<InputLabel>{`Select ${workTrackingSystemTerm}`}</InputLabel>
					<Select
						value={selectedWorkTrackingSystem?.workTrackingSystem ?? ""}
						onChange={handleSystemChange}
						label={`Select ${workTrackingSystemTerm}`}
					>
						{workTrackingSystems.map((system) => (
							<MenuItem
								key={system.workTrackingSystem}
								value={system.workTrackingSystem}
							>
								{system.workTrackingSystem}
							</MenuItem>
						))}
					</Select>
				</FormControl>

				{(selectedWorkTrackingSystem?.availableAuthenticationMethods?.length ??
					0) > 1 && (
					<FormControl fullWidth margin="normal">
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
				)}

				{/* Auth Options Section - only show when there are auth fields */}
				{showAuthSection && authOptions.length > 0 && (
					<Box sx={{ mt: 2 }}>
						<Typography
							variant="subtitle2"
							color="text.secondary"
							sx={{ mb: 1 }}
						>
							Authentication
						</Typography>
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
									margin="normal"
									value={option.value}
									onChange={(e) =>
										handleAuthOptionChange(option, e.target.value)
									}
								/>
							);
						})}
					</Box>
				)}

				{/* Additional Fields Section */}
				<AdditionalFieldsEditor
					workTrackingSystemType={
						selectedWorkTrackingSystem
							? selectedWorkTrackingSystem.workTrackingSystem
							: null
					}
					fields={additionalFields}
					onChange={setAdditionalFields}
				/>

				{/* Other Options Section - only show when there are non-auth options */}
				{otherOptions.length > 0 && (
					<Box sx={{ mt: 2 }}>
						<Typography
							variant="subtitle2"
							color="text.secondary"
							sx={{ mb: 1 }}
						>
							Options
						</Typography>
						{otherOptions.map((option) => (
							<TextField
								key={option.key}
								label={option.key}
								type={option.isSecret ? "password" : "text"}
								fullWidth
								margin="normal"
								value={option.value}
								onChange={(e) =>
									handleOtherOptionChange(option, e.target.value)
								}
							/>
						))}
					</Box>
				)}
			</DialogContent>
			<DialogActions>
				<ValidationActions
					onCancel={handleClose}
					onValidate={handleValidate}
					onSave={handleSubmit}
					inputsValid={inputsValid}
					validationFailedMessage={`Could not connect to the ${workTrackingSystemTerm} with the provided settings. Please review and try again.`}
					saveButtonText={workTrackingSystems.length === 1 ? "Save" : "Create"}
				/>
			</DialogActions>
		</Dialog>
	);
};

export default ModifyTrackingSystemConnectionDialog;
