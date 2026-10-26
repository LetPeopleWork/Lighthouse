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
import type { LicenseCheckStatus } from "../../../hooks/useLicenseRestrictions";
import { LicenseTooltip } from "../../App/License/LicenseToolTip";
import ActionButton from "../ActionButton/ActionButton";
import LoadingAnimation from "../LoadingAnimation/LoadingAnimation";

export interface WizardFrameAction {
	label: string;
	onClick: () => void | Promise<void>;
	disabled: boolean;
	/** Set when the action creates something the license limits: it then says why it is disabled. */
	licenseGate?: {
		status: LicenseCheckStatus;
		allowed: boolean;
		/** Said next to the premium pointer when the free limit is what disables the action. */
		premiumExtraInfo?: string;
	};
}

export interface WizardFrameError {
	message: string;
	details?: string | null;
}

export interface WizardStepperFrameProps {
	steps: readonly string[];
	activeStep: number;
	busy: boolean;
	/** Keeps the inputs on screen but stops the user leaving or stepping back, e.g. while a save is in flight. */
	navigationDisabled?: boolean;
	onCancel: () => void;
	onBack?: () => void;
	next?: WizardFrameAction;
	primaryAction?: WizardFrameAction;
	error?: WizardFrameError | null;
	children: React.ReactNode;
}

const WizardActionButton: React.FC<{ action: WizardFrameAction }> = ({
	action,
}) => {
	const button = (
		<ActionButton
			buttonText={action.label}
			onClickHandler={() => Promise.resolve(action.onClick())}
			buttonVariant="contained"
			disabled={action.disabled}
		/>
	);
	if (!action.licenseGate) return button;

	return (
		<LicenseTooltip
			canUseFeature={action.licenseGate.allowed}
			defaultTooltip=""
			checkStatus={action.licenseGate.status}
			premiumExtraInfo={action.licenseGate.premiumExtraInfo}
		>
			<span>{button}</span>
		</LicenseTooltip>
	);
};

const WizardStepperFrame: React.FC<WizardStepperFrameProps> = ({
	steps,
	activeStep,
	busy,
	navigationDisabled = false,
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
				<Button
					onClick={onCancel}
					variant="outlined"
					disabled={navigationDisabled}
				>
					Cancel
				</Button>
				{onBack && (
					<Button
						onClick={onBack}
						variant="outlined"
						disabled={navigationDisabled}
					>
						Back
					</Button>
				)}
				{/* A button holds itself disabled for a moment after its click; keyed by step, that pause stays with the step it was clicked on. */}
				{next && <WizardActionButton key={activeStep} action={next} />}
				{primaryAction && (
					<WizardActionButton key={activeStep} action={primaryAction} />
				)}
			</Box>
		</Box>
	</LoadingAnimation>
);

export default WizardStepperFrame;
