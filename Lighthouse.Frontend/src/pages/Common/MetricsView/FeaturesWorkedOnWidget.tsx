import { Card, CardContent, Typography, useTheme } from "@mui/material";
import type React from "react";

type FeaturesWorkedOnWidgetProps = {
	readonly featureCount: number;
	readonly featureWip?: number;
	readonly title?: string;
};

const FeaturesWorkedOnWidget: React.FC<FeaturesWorkedOnWidgetProps> = ({
	featureCount,
	featureWip,
	title = "Features being Worked On",
}) => {
	const theme = useTheme();
	const hasLimit = featureWip != null && featureWip > 0;

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
					{title}
				</Typography>

				<Typography
					variant="h3"
					data-testid="features-worked-on-count"
					sx={{ color: theme.palette.primary.main, fontWeight: "bold" }}
				>
					{featureCount}
				</Typography>

				{hasLimit && (
					<Typography
						variant="body2"
						color="text.secondary"
						data-testid="features-worked-on-limit"
						sx={{ mt: 0.5 }}
					>
						Limit: {featureWip}
					</Typography>
				)}
			</CardContent>
		</Card>
	);
};

export default FeaturesWorkedOnWidget;
