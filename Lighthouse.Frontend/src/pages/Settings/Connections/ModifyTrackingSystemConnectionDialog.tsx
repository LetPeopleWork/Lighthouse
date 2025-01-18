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
import { useEffect, useState } from "react";
import ValidationActions from "../../../components/Common/ValidationActions/ValidationActions";
import type { IWorkTrackingSystemConnection } from "../../../models/WorkTracking/WorkTrackingSystemConnection";
import type { IWorkTrackingSystemOption } from "../../../models/WorkTracking/WorkTrackingSystemOption";

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
	const [selectedOptions, setSelectedOptions] = useState<
		IWorkTrackingSystemOption[]
	>([]);
	const [inputsValid, setInputsValid] = useState<boolean>(false);

	useEffect(() => {
		if (open && workTrackingSystems.length > 0) {
			const firstSystem = workTrackingSystems[0];
			setSelectedWorkTrackingSystem(firstSystem);
			setName(firstSystem.name);
			setSelectedOptions(
				firstSystem.options.map((option) => ({
					key: option.key,
					value: option.value,
					isSecret: option.isSecret,
					isOptional: option.isOptional,
				})),
			);
		}
	}, [open, workTrackingSystems]);

	const handleSystemChange = (event: SelectChangeEvent<string>) => {
		const system = workTrackingSystems.find(
			(system) => system.workTrackingSystem === event.target.value,
		);
		if (system) {
			setSelectedWorkTrackingSystem(system);
			setName(system.name);
			setSelectedOptions(
				system.options.map((option) => ({
					key: option.key,
					value: option.value,
					isSecret: option.isSecret,
					isOptional: option.isOptional,
				})),
			);
		}
	};

	const onInputsChanged = () => {
		const optionsValid = selectedOptions.every(
			(option) => option.isOptional || option.value !== "",
		);
		const nameValid = name !== "";

		setInputsValid(optionsValid && nameValid);
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

		onInputsChanged();
	};

	const handleNameChange = (event: React.ChangeEvent<HTMLInputElement>) => {
		setName(event.target.value);
		onInputsChanged();
	};

	const handleValidate = async () => {
		if (selectedWorkTrackingSystem) {
			const settings: IWorkTrackingSystemConnection = {
				id: selectedWorkTrackingSystem.id,
				name,
				workTrackingSystem: selectedWorkTrackingSystem.workTrackingSystem,
				options: selectedOptions,
			};

			return await validateSettings(settings);
		}

		return false;
	};

	const handleSubmit = () => {
		if (selectedWorkTrackingSystem) {
			const updatedSystem: IWorkTrackingSystemConnection = {
				id: selectedWorkTrackingSystem.id,
				name: name,
				workTrackingSystem: selectedWorkTrackingSystem.workTrackingSystem,
				options: selectedOptions,
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
					<InputLabel>Select Work Tracking System</InputLabel>
					<Select
						value={selectedWorkTrackingSystem?.workTrackingSystem ?? ""}
						onChange={handleSystemChange}
						label="Select Work Tracking System"
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

				{selectedOptions.map((option) => (
					<TextField
						key={option.key}
						label={option.key}
						type={option.isSecret ? "password" : "text"}
						fullWidth
						margin="normal"
						value={option.value}
						onChange={(e) => handleOptionChange(option, e.target.value)}
					/>
				))}
			</DialogContent>
			<DialogActions>
				<ValidationActions
					onCancel={handleClose}
					onValidate={handleValidate}
					onSave={handleSubmit}
					inputsValid={inputsValid}
					validationFailedMessage="Could not connect to the work tracking system with the provided settings. Please review and try again."
					saveButtonText={workTrackingSystems.length === 1 ? "Save" : "Create"}
				/>
			</DialogActions>
		</Dialog>
	);
};

export default ModifyTrackingSystemConnectionDialog;
