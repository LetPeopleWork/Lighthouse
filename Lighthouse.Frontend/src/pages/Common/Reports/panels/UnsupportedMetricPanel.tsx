import { Paper, Typography } from "@mui/material";
import type React from "react";

const METRIC_FROM_A_NEWER_VERSION =
	"This metric can't be shown in this version of Lighthouse.";

/** Stands where a metric added by a later version would be, so the rest of the report still reads. */
const UnsupportedMetricPanel: React.FC = () => (
	<Paper variant="outlined" sx={{ p: 2, width: "100%" }}>
		<Typography color="text.secondary">
			{METRIC_FROM_A_NEWER_VERSION}
		</Typography>
	</Paper>
);

export default UnsupportedMetricPanel;
