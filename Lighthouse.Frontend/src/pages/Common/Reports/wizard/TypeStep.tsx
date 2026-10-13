import {
	Alert,
	Box,
	Button,
	CircularProgress,
	FormControlLabel,
	Radio,
	RadioGroup,
	Typography,
} from "@mui/material";
import type React from "react";
import type { ReportTemplateKey } from "../../../../models/Reports/Report";
import { reportTypeSteps } from "./reportTypeSteps";

const TYPES_COULD_NOT_LOAD = "Report types couldn't be loaded.";

export type ReportTypesStatus = "loading" | "error" | "ready";

interface TypeStepProps {
	status: ReportTypesStatus;
	chosen: ReportTemplateKey;
	onRetry: () => void;
}

const typeKeys = Object.keys(reportTypeSteps) as ReportTemplateKey[];

const TypeStep: React.FC<Readonly<TypeStepProps>> = ({
	status,
	chosen,
	onRetry,
}) => {
	if (status === "loading") {
		return (
			<Box sx={{ display: "flex", justifyContent: "center", py: 4 }}>
				<CircularProgress />
			</Box>
		);
	}

	if (status === "error") {
		return (
			<Alert
				severity="error"
				action={
					<Button color="inherit" size="small" onClick={onRetry}>
						Retry
					</Button>
				}
			>
				{TYPES_COULD_NOT_LOAD}
			</Alert>
		);
	}

	return (
		<RadioGroup value={chosen} aria-label="Type">
			{typeKeys.map((key) => (
				<FormControlLabel
					key={key}
					value={key}
					control={<Radio />}
					label={
						<Box>
							<Typography variant="subtitle1">
								{reportTypeSteps[key].title}
							</Typography>
							<Typography variant="body2" color="text.secondary">
								{reportTypeSteps[key].purpose}
							</Typography>
						</Box>
					}
					sx={{
						border: 1,
						borderColor: "primary.main",
						borderRadius: 1,
						m: 0,
						p: 1,
					}}
				/>
			))}
		</RadioGroup>
	);
};

export default TypeStep;
