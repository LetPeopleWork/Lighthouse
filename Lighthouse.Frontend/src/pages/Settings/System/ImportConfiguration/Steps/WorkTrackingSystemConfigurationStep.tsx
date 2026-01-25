import {
	Alert,
	Box,
	Button,
	CircularProgress,
	Grid,
	TextField,
	Typography,
} from "@mui/material";
import { useState } from "react";
import type { IWorkTrackingSystemConnection } from "../../../../../models/WorkTracking/WorkTrackingSystemConnection";
import type { IWorkTrackingSystemService } from "../../../../../services/Api/WorkTrackingSystemService";

interface WorkTrackingSystemConfigurationStepProps {
	newWorkTrackingSystems: IWorkTrackingSystemConnection[];
	workTrackingSystemService: IWorkTrackingSystemService;
	onNext: (newWorkTrackingSystems: IWorkTrackingSystemConnection[]) => void;
	onCancel: () => void;
}

const WorkTrackingSystemConfigurationStep: React.FC<
	WorkTrackingSystemConfigurationStepProps
> = ({
	newWorkTrackingSystems,
	workTrackingSystemService,
	onNext,
	onCancel,
}) => {
	const [workTrackingSystems, setWorkTrackingSystems] = useState<
		IWorkTrackingSystemConnection[]
	>(newWorkTrackingSystems);
	const [validating, setValidating] = useState<boolean>(false);
	const [validationSuccessful, setValidationSuccessful] = useState<boolean>(
		newWorkTrackingSystems.length === 0,
	);
	const [validationError, setValidationError] = useState<string | null>(null);
	const onValidate = async () => {
		setValidating(true);
		setValidationError(null);

		let validationPassed = true;

		try {
			for (const system of workTrackingSystems) {
				const isValid =
					await workTrackingSystemService.validateWorkTrackingSystemConnection(
						system,
					);
				if (!isValid) {
					validationPassed = false;
					if (!validationPassed) {
						setValidationError(
							`Connection validation failed for ${system.name}. Please check your credentials and try again.`,
						);
					}
					break;
				}
			}

			setValidationSuccessful(validationPassed);
		} catch (error) {
			setValidationSuccessful(false);
			setValidationError(
				error instanceof Error
					? error.message
					: "An unexpected error occurred during validation.",
			);
		} finally {
			setValidating(false);
		}
	};

	const handleNext = () => {
		onNext(workTrackingSystems);
	};

	const handleOptionChange = (
		systemId: number | null,
		systemName: string,
		optionKey: string,
		newValue: string,
	) => {
		const updatedSystems = workTrackingSystems.map((sys) => {
			if (sys.id === systemId && sys.name === systemName) {
				const updatedOptions = sys.options.map((opt) =>
					opt.key === optionKey ? { ...opt, value: newValue } : opt,
				);
				return { ...sys, options: updatedOptions };
			}
			return sys;
		});
		setWorkTrackingSystems(updatedSystems);
	};

	return (
		<Box sx={{ width: "100%" }}>
			<Typography variant="h6" gutterBottom>
				Work Tracking System Configuration
			</Typography>
			<Grid container spacing={2}>
				{workTrackingSystems.length === 0 && (
					<Typography variant="body1" sx={{ mt: 2 }}>
						No new work tracking Systems.
					</Typography>
				)}

				{workTrackingSystems.map((system) => (
					<Grid size={{ xs: 12 }} key={system.name}>
						<Typography variant="subtitle1">{system.name}</Typography>
						{system.options
							.filter((option) => option.isSecret)
							.map((option) => (
								<TextField
									key={option.key}
									label={`${option.key} (required)`}
									type="password"
									fullWidth
									margin="normal"
									value={option.value}
									onChange={(e) =>
										handleOptionChange(
											system.id,
											system.name,
											option.key,
											e.target.value,
										)
									}
									required
								/>
							))}
					</Grid>
				))}
			</Grid>
			{validationError && (
				<Box sx={{ mt: 2 }}>
					<Alert severity="error">{validationError}</Alert>
				</Box>
			)}
			{!validating && validationSuccessful && (
				<Box sx={{ mt: 2 }}>
					<Alert severity="success">
						Validation successful! You can proceed to the next step.
					</Alert>
				</Box>
			)}{" "}
			<Box
				sx={{
					mt: 4,
					display: "flex",
					justifyContent: "space-between",
					alignItems: "center",
				}}
			>
				<Box>
					<Button
						variant="outlined"
						color="secondary"
						onClick={onCancel}
						disabled={validating}
					>
						Cancel
					</Button>
				</Box>
				<Box sx={{ display: "flex", alignItems: "center" }}>
					<Button
						variant="outlined"
						onClick={onValidate}
						disabled={validating}
						sx={{ mr: 2 }}
					>
						{validating ? "Validating..." : "Validate"}
					</Button>
					{validating && <CircularProgress size={24} sx={{ mr: 2 }} />}
					<Button
						variant="contained"
						color="primary"
						disabled={validating || !validationSuccessful}
						onClick={handleNext}
					>
						Next
					</Button>
				</Box>
			</Box>
		</Box>
	);
};

export default WorkTrackingSystemConfigurationStep;
