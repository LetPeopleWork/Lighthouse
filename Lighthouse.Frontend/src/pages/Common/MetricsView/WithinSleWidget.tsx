import { Card, CardContent, Typography, useTheme } from "@mui/material";
import type React from "react";
import type { IWithinSleInfo } from "../../../models/Metrics/WithinSleInfo";
import { minimumClosedOf } from "../../../models/Metrics/WithinSleMinimumClosed";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import { useTerminology } from "../../../services/TerminologyContext";

interface WithinSleWidgetProps {
	readonly info: IWithinSleInfo;
}

const WithinSleWidget: React.FC<WithinSleWidgetProps> = ({ info }) => {
	const theme = useTheme();
	const { getTerm } = useTerminology();
	const workItems = getTerm(TERMINOLOGY_KEYS.WORK_ITEMS);
	const sle = getTerm(TERMINOLOGY_KEYS.SLE);

	return (
		<Card sx={{ borderRadius: 2, height: "100%", width: "100%" }}>
			<CardContent
				sx={{
					display: "flex",
					flexDirection: "column",
					alignItems: "center",
					justifyContent: "center",
					height: "100%",
					p: 2,
				}}
			>
				<Typography variant="h6" gutterBottom sx={{ textAlign: "center" }}>
					Within {sle}
				</Typography>

				{info.verdict === "Measured" && (
					<>
						<Typography
							variant="h4"
							data-testid="within-sle-share"
							sx={{ color: theme.palette.primary.main, fontWeight: "bold" }}
						>
							{`${info.sharePercent} % (${info.withinCount} / ${info.closedCount})`}
						</Typography>
						<Typography variant="body2" color="text.secondary" sx={{ mt: 0.5 }}>
							{`of ${workItems} closed within ${info.sleDays} days or less`}
						</Typography>
						<Typography variant="body2" color="text.secondary">
							{`Target ${info.slePercentile} %`}
						</Typography>
					</>
				)}

				{info.verdict === "NoSle" && (
					<Typography variant="body2" color="text.secondary">
						{`No ${sle} set — define one in settings to see how many ${workItems} finish within it`}
					</Typography>
				)}

				{info.verdict === "TooFew" && (
					<Typography variant="body2" color="text.secondary">
						{`${info.closedCount} ${workItems} closed — too few to judge (needs at least ${minimumClosedOf(info)})`}
					</Typography>
				)}
			</CardContent>
		</Card>
	);
};

export default WithinSleWidget;
