import AddIcon from "@mui/icons-material/Add";
import CheckCircleIcon from "@mui/icons-material/CheckCircle";
import {
	Box,
	Button,
	Step,
	StepLabel,
	Stepper,
	Typography,
	useTheme,
} from "@mui/material";
import type React from "react";
import { useMemo } from "react";
import { useNavigate } from "react-router-dom";

interface OnboardingStepperProps {
	hasConnections: boolean;
	hasTeams: boolean;
	hasPortfolios: boolean;
	canCreateTeam: boolean;
	canCreatePortfolio: boolean;
	teamTerm: string;
	portfolioTerm: string;
}

const OnboardingStepper: React.FC<OnboardingStepperProps> = ({
	hasConnections,
	hasTeams,
	hasPortfolios,
	canCreateTeam,
	canCreatePortfolio,
	teamTerm,
	portfolioTerm,
}) => {
	const navigate = useNavigate();
	const theme = useTheme();

	const activeStep = useMemo(() => {
		if (!hasConnections) return 0;
		if (!hasTeams) return 1;
		if (!hasPortfolios) return 2;
		return 3;
	}, [hasConnections, hasTeams, hasPortfolios]);

	// Don't render when fully onboarded
	if (activeStep === 3) {
		return null;
	}

	const steps = [
		{
			label: "Connect",
			description: "Set up a connection to your work tracking system.",
			action: () => navigate("/connections/new"),
			actionLabel: "Add Connection",
			enabled: true,
		},
		{
			label: `Add ${teamTerm}`,
			description: `Configure a ${teamTerm.toLowerCase()} to start tracking flow metrics.`,
			action: () => navigate("/teams/new"),
			actionLabel: `Add ${teamTerm}`,
			enabled: hasConnections && canCreateTeam,
		},
		{
			label: `Add ${portfolioTerm}`,
			description: `Group ${teamTerm.toLowerCase()}s into a ${portfolioTerm.toLowerCase()} for forecasting.`,
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
			<Typography variant="h6" sx={{ mb: 2, fontWeight: 600 }}>
				Get Started
			</Typography>

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
