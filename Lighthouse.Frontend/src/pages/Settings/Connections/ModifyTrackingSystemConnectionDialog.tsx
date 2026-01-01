import {
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
} from "@mui/material";
import type React from "react";
import { useCallback, useEffect, useState } from "react";
import ValidationActions from "../../../components/Common/ValidationActions/ValidationActions";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import type {
	IAuthenticationMethod,
	IWorkTrackingSystemConnection,
} from "../../../models/WorkTracking/WorkTrackingSystemConnection";
import type { IWorkTrackingSystemOption } from "../../../models/WorkTracking/WorkTrackingSystemOption";
import { useTerminology } from "../../../services/TerminologyContext";

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
	const [selectedOptions, setSelectedOptions] = useState<
		IWorkTrackingSystemOption[]
	>([]);
	const [inputsValid, setInputsValid] = useState<boolean>(false);

	const { getTerm } = useTerminology();
	const workTrackingSystemTerm = getTerm(TERMINOLOGY_KEYS.WORK_TRACKING_SYSTEM);

	const getOptionsForAuthMethod = useCallback(
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

			if (workTrackingSystems.length === 1) {
				setSelectedOptions(
					firstSystem.options.map((option) => ({
						key: option.key,
						value: option.value,
						isSecret: option.isSecret,
						isOptional: option.isOptional,
					})),
				);
			} else {
				setSelectedOptions(getOptionsForAuthMethod(initialMethod));
			}
		}
	}, [open, workTrackingSystems, getOptionsForAuthMethod]);

	useEffect(() => {
		const optionsValid = selectedOptions.every(
			(option) => option.isOptional || option.value !== "",
		);
		const nameValid = name !== "";

		setInputsValid(optionsValid && nameValid);
	}, [name, selectedOptions]);

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
			setSelectedOptions(getOptionsForAuthMethod(defaultMethod));
		}
	};

	const handleAuthMethodChange = (event: SelectChangeEvent<string>) => {
		const availableMethods =
			selectedWorkTrackingSystem?.availableAuthenticationMethods ?? [];
		const method = availableMethods.find((m) => m.key === event.target.value);
		if (method) {
			setSelectedAuthMethod(method);
			setSelectedOptions(getOptionsForAuthMethod(method));
		}
	};

	const handleOptionChange = (
		changedOption: IWorkTrackingSystemOption,
		newValue: string,
	) => {
		setSelectedOptions((prevOptions) =>
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
				dataSourceType: selectedWorkTrackingSystem.dataSourceType,
				workTrackingSystem: selectedWorkTrackingSystem.workTrackingSystem,
				options: selectedOptions,
				authenticationMethodKey: selectedAuthMethod.key,
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
				dataSourceType: selectedWorkTrackingSystem.dataSourceType,
				workTrackingSystem: selectedWorkTrackingSystem.workTrackingSystem,
				options: selectedOptions,
				authenticationMethodKey: selectedAuthMethod.key,
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

				{selectedOptions.map((option) => {
					// Find display name from selected auth method
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
							onChange={(e) => handleOptionChange(option, e.target.value)}
						/>
					);
				})}
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
