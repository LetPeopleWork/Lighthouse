import ContactMailIcon from "@mui/icons-material/ContactMail";
import InfoIcon from "@mui/icons-material/Info";
import LoadingButton from "@mui/lab/LoadingButton";
import Alert from "@mui/material/Alert";
import Box from "@mui/material/Box";
import Card from "@mui/material/Card";
import CardActions from "@mui/material/CardActions";
import CardContent from "@mui/material/CardContent";
import Chip from "@mui/material/Chip";
import Grid from "@mui/material/Grid";
import Link from "@mui/material/Link";
import Paper from "@mui/material/Paper";
import Stack from "@mui/material/Stack";
import { useTheme } from "@mui/material/styles";
import Typography from "@mui/material/Typography";
import useMediaQuery from "@mui/material/useMediaQuery";
import type React from "react";
import { useContext, useEffect, useState } from "react";
import { useLicenseRestrictions } from "../../../hooks/useLicenseRestrictions";
import type { IDemoDataScenario } from "../../../models/DemoData/IDemoData";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";

const DemoDataSettings: React.FC = () => {
	const [scenarios, setScenarios] = useState<IDemoDataScenario[]>([]);
	const [loading, setLoading] = useState(false);
	const [loadingScenarioId, setLoadingScenarioId] = useState<string | null>(
		null,
	);
	const [loadingAll, setLoadingAll] = useState(false);
	const theme = useTheme();
	const isMobile = useMediaQuery(theme.breakpoints.down("md"));
	const { demoDataService } = useContext(ApiServiceContext);
	const { licenseStatus } = useLicenseRestrictions();

	const canUsePremiumFeatures = licenseStatus?.canUsePremiumFeatures ?? false;

	useEffect(() => {
		const fetchScenarios = async () => {
			try {
				setLoading(true);
				const availableScenarios =
					await demoDataService.getAvailableScenarios();
				setScenarios(availableScenarios);
			} catch (error) {
				console.error("Failed to fetch demo scenarios:", error);
			} finally {
				setLoading(false);
			}
		};

		fetchScenarios();
	}, [demoDataService]);

	const handleLoadScenario = async (scenarioId: string) => {
		try {
			setLoadingScenarioId(scenarioId);
			await demoDataService.loadScenario(scenarioId);
			// Optionally show success message or redirect
		} catch (error) {
			console.error("Failed to load scenario:", error);
		} finally {
			setLoadingScenarioId(null);
		}
	};

	const handleLoadAllScenarios = async () => {
		try {
			setLoadingAll(true);
			await demoDataService.loadAllScenarios();
			// Optionally show success message or redirect
		} catch (error) {
			console.error("Failed to load all scenarios:", error);
		} finally {
			setLoadingAll(false);
		}
	};

	if (loading) {
		return (
			<Box display="flex" justifyContent="center" p={4}>
				<Typography>Loading demo scenarios...</Typography>
			</Box>
		);
	}

	return (
		<Box sx={{ maxWidth: 1200, mx: "auto" }}>
			<Typography
				variant="h5"
				component="h2"
				sx={{
					fontWeight: 600,
					color: theme.palette.primary.main,
					mb: 3,
					textAlign: isMobile ? "center" : "left",
				}}
			>
				Demo Data Management
			</Typography>

			{/* Warning Alert */}
			<Alert
				severity="warning"
				icon={<InfoIcon fontSize="medium" />}
				sx={{ mb: 4 }}
			>
				<Typography variant="body1" sx={{ fontWeight: "medium", mb: 1 }}>
					Important: Loading demo data will remove all existing teams and
					projects!
				</Typography>
				<Typography variant="body2" color="text.secondary">
					This action cannot be undone. Make sure to export your current
					configuration if you want to preserve it.
				</Typography>
			</Alert>

			{/* Scenarios Grid */}
			<Grid container spacing={3} sx={{ mb: 4 }}>
				{scenarios.map((scenario) => (
					<Grid size={{ xs: 12, md: 6, lg: 4 }} key={scenario.id}>
						<Card
							variant="outlined"
							sx={{
								height: "100%",
								display: "flex",
								flexDirection: "column",
								transition: "all 0.3s ease",
								"&:hover": {
									transform: "translateY(-4px)",
									boxShadow: theme.shadows[8],
								},
								opacity: scenario.isPremium && !canUsePremiumFeatures ? 0.6 : 1,
							}}
						>
							<CardContent sx={{ flexGrow: 1 }}>
								<Stack direction="row" spacing={1} sx={{ mb: 2 }}>
									<Typography variant="h6" component="h3" sx={{ flexGrow: 1 }}>
										{scenario.title}
									</Typography>
									{scenario.isPremium && (
										<Chip
											label="Premium"
											color="primary"
											size="small"
											variant="outlined"
										/>
									)}
								</Stack>

								<Typography
									variant="body2"
									color="text.secondary"
									sx={{ mb: 2 }}
								>
									{scenario.description}
								</Typography>

								<Stack spacing={1}>
									<Typography variant="body2">
										<strong>Teams:</strong> {scenario.numberOfTeams}
									</Typography>
									<Typography variant="body2">
										<strong>Projects:</strong> {scenario.numberOfProjects}
									</Typography>
								</Stack>
							</CardContent>

							<CardActions sx={{ p: 2 }}>
								<LoadingButton
									variant="contained"
									fullWidth
									loading={loadingScenarioId === scenario.id}
									onClick={() => handleLoadScenario(scenario.id)}
									disabled={scenario.isPremium && !canUsePremiumFeatures}
									color="primary"
								>
									{scenario.isPremium && !canUsePremiumFeatures
										? "Premium Required"
										: "Load Scenario"}
								</LoadingButton>
							</CardActions>
						</Card>
					</Grid>
				))}
			</Grid>

			{/* Load All Button */}
			<Paper
				elevation={2}
				sx={{
					p: 3,
					mb: 4,
					backgroundColor: theme.palette.primary.main,
					color: theme.palette.primary.contrastText,
					opacity: canUsePremiumFeatures ? 1 : 0.6,
				}}
			>
				<Stack direction="row" spacing={1} sx={{ mb: 2, alignItems: "center" }}>
					<Typography variant="h6" sx={{ flexGrow: 1 }}>
						Load All Scenarios
					</Typography>
					{!canUsePremiumFeatures && (
						<Chip
							label="Premium"
							color="secondary"
							size="small"
							variant="outlined"
							sx={{
								color: "white",
								borderColor: "white",
							}}
						/>
					)}
				</Stack>
				<Typography variant="body2" sx={{ mb: 2 }}>
					Load all available demo scenarios at once to create a comprehensive
					test environment.
				</Typography>
				<LoadingButton
					variant="contained"
					color="secondary"
					loading={loadingAll}
					onClick={handleLoadAllScenarios}
					disabled={!canUsePremiumFeatures}
					sx={{
						bgcolor: "white",
						color: theme.palette.primary.main,
						"&:hover": {
							bgcolor: "grey.100",
						},
						"&:disabled": {
							bgcolor: "grey.300",
							color: "grey.600",
						},
					}}
				>
					{canUsePremiumFeatures ? "Load All" : "Premium Required"}
				</LoadingButton>
			</Paper>

			{/* Contact Information */}
			<Paper
				elevation={1}
				sx={{
					p: 3,
					backgroundColor: theme.palette.background.paper,
					borderRadius: 2,
					border: `1px solid ${theme.palette.divider}`,
				}}
			>
				<Stack
					direction={isMobile ? "column" : "row"}
					alignItems="center"
					spacing={2}
				>
					<ContactMailIcon color="primary" fontSize="large" />
					<Box sx={{ textAlign: isMobile ? "center" : "left" }}>
						<Typography
							variant="h6"
							sx={{ mb: 1, color: theme.palette.text.primary }}
						>
							Have Feedback or Suggestions?
						</Typography>
						<Typography
							variant="body2"
							sx={{ color: theme.palette.text.secondary }}
						>
							We'd love to hear from you! Reach out to us at{" "}
							<Link
								href="mailto:contact@letpeople.work"
								color="primary"
								underline="hover"
							>
								contact@letpeople.work
							</Link>{" "}
							or through our{" "}
							<Link
								href="https://join.slack.com/t/let-people-work/shared_invite/zt-38df4z4sy-iqJEo6S8kmIgIfsgsV0J1A"
								color="primary"
								underline="hover"
								target="_blank"
								rel="noopener noreferrer"
							>
								Slack Channel
							</Link>{" "}
							if you have feedback on the scenarios or wish something else to be
							covered.
						</Typography>
					</Box>
				</Stack>
			</Paper>
		</Box>
	);
};

export default DemoDataSettings;
