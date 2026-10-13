import {
	Alert,
	Box,
	Button,
	Step,
	StepLabel,
	Stepper,
	Typography,
} from "@mui/material";
import type React from "react";
import ActionButton from "../ActionButton/ActionButton";
import LoadingAnimation from "../LoadingAnimation/LoadingAnimation";

export interface WizardFrameAction {
	label: string;
	onClick: () => void | Promise<void>;
	disabled: boolean;
}

export interface WizardFrameError {
	message: string;
	details?: string | null;
}

export interface WizardStepperFrameProps {
	steps: readonly string[];
	activeStep: number;
	busy: boolean;
	onCancel: () => void;
	onBack?: () => void;
	next?: WizardFrameAction;
	primaryAction?: WizardFrameAction;
	error?: WizardFrameError | null;
	children: React.ReactNode;
}

const WizardActionButton: React.FC<{ action: WizardFrameAction }> = ({
	action,
}) => (
	<ActionButton
		buttonText={action.label}
		onClickHandler={() => Promise.resolve(action.onClick())}
		buttonVariant="contained"
		disabled={action.disabled}
	/>
);

const WizardStepperFrame: React.FC<WizardStepperFrameProps> = ({
	steps,
	activeStep,
	busy,
	onCancel,
	onBack,
	next,
	primaryAction,
	error,
	children,
}) => (
	<LoadingAnimation isLoading={busy} hasError={false}>
		<Box sx={{ width: "100%", p: 3 }}>
			<Stepper activeStep={activeStep} sx={{ mb: 3 }}>
				{steps.map((label) => (
					<Step key={label}>
						<StepLabel>{label}</StepLabel>
					</Step>
				))}
			</Stepper>

			{children}

			{error && (
				<Alert severity="error" sx={{ mt: 3 }}>
					<Typography variant="body2">{error.message}</Typography>
					{error.details && (
						<Typography variant="caption" sx={{ display: "block", mt: 1 }}>
							{error.details}
						</Typography>
					)}
				</Alert>
			)}

			<Box sx={{ display: "flex", justifyContent: "flex-end", gap: 1, mt: 3 }}>
				<Button onClick={onCancel} variant="outlined">
					Cancel
				</Button>
				{onBack && (
					<Button onClick={onBack} variant="outlined">
						Back
					</Button>
				)}
				{next && <WizardActionButton action={next} />}
				{primaryAction && <WizardActionButton action={primaryAction} />}
			</Box>
		</Box>
	</LoadingAnimation>
);

export default WizardStepperFrame;
