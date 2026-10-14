import { Paper, Typography } from "@mui/material";
import type React from "react";
import { useId } from "react";

const METRIC_FROM_A_NEWER_VERSION =
	"This metric can't be shown in this version of Lighthouse.";

/** Stands where a metric this version has no panel for would be, headed by its name, so the rest of the report still reads. */
const UnsupportedMetricPanel: React.FC<{ readonly name: string }> = ({
	name,
}) => {
	const titleId = useId();
	return (
		<Paper
			component="section"
			aria-labelledby={titleId}
			variant="outlined"
			sx={{ p: 2, width: "100%" }}
		>
			<Typography id={titleId} variant="h6" component="h2" gutterBottom>
				{name}
			</Typography>
			<Typography color="text.secondary">
				{METRIC_FROM_A_NEWER_VERSION}
			</Typography>
		</Paper>
	);
};

export default UnsupportedMetricPanel;
