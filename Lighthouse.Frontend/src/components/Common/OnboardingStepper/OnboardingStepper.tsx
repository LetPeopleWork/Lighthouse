import AddIcon from "@mui/icons-material/Add";
import CheckCircleIcon from "@mui/icons-material/CheckCircle";
import CloseIcon from "@mui/icons-material/Close";
import {
	Box,
	Button,
	IconButton,
	Step,
	StepLabel,
	Stepper,
	Typography,
	useTheme,
} from "@mui/material";
import type React from "react";
import { useMemo, useState } from "react";
import { useNavigate } from "react-router";

const DISMISSED_KEY = "lighthouse-hide-onboarding-stepper";

// Storage is unavailable in private browsing and can be disabled by policy. Guidance is not worth
// taking the Overview down for, so both directions degrade to "not dismissed".
const readDismissed = (): boolean => {
	try {
		return localStorage.getItem(DISMISSED_KEY) === "true";
	} catch {
		return false;
	}
};

const rememberDismissed = (): void => {
	try {
		localStorage.setItem(DISMISSED_KEY, "true");
	} catch {
		// Nothing to recover: the panel is already gone for this session.
	}
};

interface OnboardingStepperProps {
	hasConnections: boolean;
	hasTeams: boolean;
	hasPortfolios: boolean;
	canCreateTeam: boolean;
	canCreatePortfolio: boolean;
	teamTerm: string;
	portfolioTerm: string;
	connectionTerm: string;
}

const OnboardingStepper: React.FC<OnboardingStepperProps> = ({
	hasConnections,
	hasTeams,
	hasPortfolios,
	canCreateTeam,
	canCreatePortfolio,
	teamTerm,
	portfolioTerm,
	connectionTerm,
}) => {
	const navigate = useNavigate();
	const theme = useTheme();

	// Read before the first paint, so a dismissed panel never flashes.
	const [isDismissed, setIsDismissed] = useState(readDismissed);

	const activeStep = useMemo(() => {
		if (!hasConnections) return 0;
		if (!hasTeams) return 1;
		if (!hasPortfolios) return 2;
		return 3;
	}, [hasConnections, hasTeams, hasPortfolios]);

	// Don't render when fully onboarded, or once the user has closed it for good
	if (activeStep === 3 || isDismissed) {
		return null;
	}

	const handleDismiss = () => {
		rememberDismissed();
		setIsDismissed(true);
	};

	const steps = [
		{
			label: "Connect",
			description: `Set up a ${connectionTerm} to connect with your data.`,
			action: () => navigate("/connections/new"),
			actionLabel: `Add ${connectionTerm}`,
			enabled: true,
		},
		{
			label: `Add ${teamTerm}`,
			description: `Configure a ${teamTerm} to start tracking flow metrics and run manual forecasts.`,
			action: () => navigate("/teams/new"),
			actionLabel: `Add ${teamTerm}`,
			enabled: hasConnections && canCreateTeam,
		},
		{
			label: `Add ${portfolioTerm}`,
			description: `Group ${teamTerm}s into a ${portfolioTerm} for Flight Level II metrics and continuous forecasting.`,
			action: () => navigate("/portfolios/new"),
			actionLabel: `Add ${portfolioTerm}`,
			enabled: hasTeams && canCreatePortfolio,
		},
	];

	return (
		<Box
			sx={{
				mb: 4,
				p: 3,
				borderRadius: 2,
				border: `1px solid ${theme.palette.divider}`,
				backgroundColor: theme.palette.background.paper,
			}}
			data-testid="onboarding-stepper"
		>
			<Box
				sx={{
					mb: 2,
					display: "flex",
					alignItems: "flex-start",
					justifyContent: "space-between",
					gap: 1,
				}}
			>
				<Typography variant="h6" sx={{ fontWeight: 600 }}>
					Get Started
				</Typography>
				<IconButton
					size="small"
					onClick={handleDismiss}
					aria-label="Hide Get Started and don't show it again"
					data-testid="onboarding-dismiss"
				>
					<CloseIcon fontSize="small" />
				</IconButton>
			</Box>

			<Stepper activeStep={activeStep} alternativeLabel>
				{steps.map((step, index) => (
					<Step key={step.label} completed={index < activeStep}>
						<StepLabel
							icon={
								index < activeStep ? (
									<CheckCircleIcon color="success" />
								) : undefined
							}
						>
							{step.label}
						</StepLabel>
					</Step>
				))}
			</Stepper>

			<Box
				sx={{
					mt: 3,
					display: "flex",
					flexDirection: "column",
					alignItems: "center",
					gap: 1,
				}}
			>
				<Typography
					variant="body2"
					color="text.secondary"
					sx={{ textAlign: "center" }}
				>
					{steps[activeStep].description}
				</Typography>
				<Button
					variant="contained"
					startIcon={<AddIcon />}
					onClick={steps[activeStep].action}
					disabled={!steps[activeStep].enabled}
					data-testid="onboarding-cta"
				>
					{steps[activeStep].actionLabel}
				</Button>
			</Box>
		</Box>
	);
};

export default OnboardingStepper;
