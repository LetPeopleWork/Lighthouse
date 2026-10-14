import {
	Alert,
	Box,
	Button,
	CircularProgress,
	FormControlLabel,
	Radio,
	RadioGroup,
	Stack,
	Typography,
} from "@mui/material";
import type React from "react";
import type { ReportTemplateKey } from "../../../../models/Reports/Report";
import type { WidgetStatus } from "../../MetricsView/widgetStatus";
import { FROZEN_NOTE } from "./frozenNote";
import { reportTypes } from "./reportTypes";

const TYPES_COULD_NOT_LOAD = "Report types couldn't be loaded.";

interface TypeStepProps {
	status: WidgetStatus;
	chosen: ReportTemplateKey;
	onRetry: () => void;
	/** A report being edited keeps the Type it was created with. */
	frozen?: boolean;
}

const typeKeys = Object.keys(reportTypes) as ReportTemplateKey[];

const TypeStep: React.FC<Readonly<TypeStepProps>> = ({
	status,
	chosen,
	onRetry,
	frozen = false,
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
		<Stack spacing={1}>
			<RadioGroup value={chosen} aria-label="Type">
				{typeKeys.map((key) => (
					<FormControlLabel
						key={key}
						value={key}
						control={<Radio disabled={frozen} />}
						label={
							<Box>
								<Typography variant="subtitle1">
									{reportTypes[key].title}
								</Typography>
								<Typography variant="body2" color="text.secondary">
									{reportTypes[key].purpose}
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
			{frozen && (
				<Typography variant="body2" color="text.secondary">
					{`Type · ${FROZEN_NOTE}`}
				</Typography>
			)}
		</Stack>
	);
};

export default TypeStep;
