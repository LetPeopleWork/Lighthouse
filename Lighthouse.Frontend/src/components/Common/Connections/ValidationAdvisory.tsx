import { Alert, type SxProps, type Theme, Typography } from "@mui/material";
import type React from "react";

interface ValidationAdvisoryProps {
	advisory: string | null;
	testId: string;
	sx?: SxProps<Theme>;
}

// ADR-118 D5: a working connection can still have something worth saying, and the backend owns the copy.
const ValidationAdvisory: React.FC<ValidationAdvisoryProps> = ({
	advisory,
	testId,
	sx,
}) => {
	if (advisory === null) {
		return null;
	}

	return (
		<Alert severity="info" sx={sx} data-testid={testId}>
			<Typography variant="body2">{advisory}</Typography>
		</Alert>
	);
};

export default ValidationAdvisory;
