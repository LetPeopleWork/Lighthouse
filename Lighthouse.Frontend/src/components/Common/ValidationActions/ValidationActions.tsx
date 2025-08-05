import CheckIcon from "@mui/icons-material/Check";
import ErrorIcon from "@mui/icons-material/Error";
import PendingIcon from "@mui/icons-material/HourglassEmpty";
import { Alert, Button, Stack, Tooltip } from "@mui/material";
import type React from "react";
import { useEffect, useState } from "react";
import ActionButton from "../ActionButton/ActionButton";

export type ValidationState = "pending" | "success" | "failed";

interface ValidationActionsProps {
	onCancel?: () => void;
	onValidate?: () => Promise<boolean>;
	onSave: () => void;
	inputsValid: boolean;
	validationFailedMessage?: string;
	saveButtonText?: string;
	cancelButtonText?: string;
	disableSave?: boolean;
	saveTooltip?: string;
}

const ValidationActions: React.FC<ValidationActionsProps> = ({
	onCancel,
	onValidate,
	onSave,
	inputsValid,
	validationFailedMessage,
	saveButtonText = "Save",
	cancelButtonText = "Cancel",
	disableSave = false,
	saveTooltip = "",
}) => {
	const [validationState, setValidationState] = useState<ValidationState>(
		inputsValid ? "success" : "pending",
	);

	useEffect(() => {
		setValidationState("pending");
	}, []);

	const getValidationIcon = () => {
		switch (validationState) {
			case "success":
				return <CheckIcon color="success" />;
			case "failed":
				return <ErrorIcon color="error" />;
			default:
				return <PendingIcon color="action" />;
		}
	};

	const handleValidation = async () => {
		setValidationState("pending");
		if (onValidate) {
			const isValid = await onValidate();
			setValidationState(isValid ? "success" : "failed");
		}
	};

	const handleSave = async () => {
		onSave();
		setValidationState("pending");
	};

	return (
		<Stack direction="row" spacing={1} alignItems="center">
			{validationState === "failed" && validationFailedMessage && (
				<Alert severity="error" sx={{ mt: 2 }}>
					{validationFailedMessage}
				</Alert>
			)}

			{onCancel && (
				<Button onClick={onCancel} variant="outlined">
					{cancelButtonText}
				</Button>
			)}

			{onValidate && (
				<>
					{getValidationIcon()}
					<ActionButton
						buttonText="Validate"
						onClickHandler={handleValidation}
						buttonVariant="outlined"
						disabled={!inputsValid}
					/>
				</>
			)}

			<Tooltip title={saveTooltip} arrow>
				<span>
					<ActionButton
						buttonText={saveButtonText}
						onClickHandler={handleSave}
						buttonVariant="contained"
						disabled={
							disableSave ||
							(!onValidate && !inputsValid) ||
							(onValidate && validationState !== "success")
						}
					/>
				</span>
			</Tooltip>
		</Stack>
	);
};

export default ValidationActions;
