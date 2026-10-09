import { Box, Card, CardContent, Typography, useTheme } from "@mui/material";
import type React from "react";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import { useTerminology } from "../../../services/TerminologyContext";

interface TotalWorkItemAgeWidgetProps {
	readonly totalAge: number;
}

const TotalWorkItemAgeWidget: React.FC<TotalWorkItemAgeWidgetProps> = ({
	totalAge,
}) => {
	const theme = useTheme();

	const { getTerm } = useTerminology();
	const workItemAgeTerm = getTerm(TERMINOLOGY_KEYS.WORK_ITEM_AGE);

	return (
		<Card
			sx={{
				borderRadius: 2,
				height: "100%",
				width: "100%",
				display: "flex",
				flexDirection: "column",
			}}
		>
			<CardContent
				sx={{
					display: "flex",
					flexDirection: "column",
					flex: "1 1 auto",
					justifyContent: "center",
					alignItems: "center",
					p: 2,
				}}
			>
				<Typography
					variant="h6"
					gutterBottom
					sx={{ textAlign: "center", mb: 2 }}
				>
					Total {workItemAgeTerm}
				</Typography>

				<Box sx={{ textAlign: "center" }}>
					<Box
						sx={{
							display: "flex",
							alignItems: "baseline",
							justifyContent: "center",
							gap: 1,
						}}
					>
						<Typography
							variant="h3"
							sx={{
								color: theme.palette.primary.main,
								fontWeight: "bold",
							}}
						>
							{totalAge}
						</Typography>
						<Typography variant="h6" color="text.secondary">
							days
						</Typography>
					</Box>
				</Box>
			</CardContent>
		</Card>
	);
};

export default TotalWorkItemAgeWidget;
