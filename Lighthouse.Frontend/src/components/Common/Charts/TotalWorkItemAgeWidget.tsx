import {
	Box,
	Card,
	CardContent,
	CircularProgress,
	Typography,
	useTheme,
} from "@mui/material";
import type React from "react";
import { useEffect, useState } from "react";
import type { IFeature } from "../../../models/Feature";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import type { IWorkItem } from "../../../models/WorkItem";
import type { IMetricsService } from "../../../services/Api/MetricsService";
import { useTerminology } from "../../../services/TerminologyContext";

interface TotalWorkItemAgeWidgetProps {
	entityId: number;
	metricsService: IMetricsService<IWorkItem | IFeature>;
}

const TotalWorkItemAgeWidget: React.FC<TotalWorkItemAgeWidgetProps> = ({
	entityId,
	metricsService,
}) => {
	const [totalAge, setTotalAge] = useState<number | null>(null);
	const [loading, setLoading] = useState<boolean>(true);
	const [error, setError] = useState<string | null>(null);
	const theme = useTheme();

	const { getTerm } = useTerminology();
	const workItemAgeTerm = getTerm(TERMINOLOGY_KEYS.WORK_ITEM_AGE);

	useEffect(() => {
		const fetchTotalAge = async () => {
			setLoading(true);
			setError(null);
			try {
				const age = await metricsService.getTotalWorkItemAge(entityId);
				setTotalAge(age);
			} catch (err) {
				console.error("Error fetching total work item age:", err);
				setError("Failed to load data");
				setTotalAge(null);
			} finally {
				setLoading(false);
			}
		};

		fetchTotalAge();
	}, [entityId, metricsService]);

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

				{loading && (
					<Box
						sx={{
							display: "flex",
							justifyContent: "center",
							alignItems: "center",
							minHeight: 80,
						}}
					>
						<CircularProgress />
					</Box>
				)}

				{error && !loading && (
					<Typography
						variant="body2"
						color="error"
						sx={{ textAlign: "center" }}
					>
						{error}
					</Typography>
				)}

				{!loading && !error && totalAge !== null && (
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
				)}
			</CardContent>
		</Card>
	);
};

export default TotalWorkItemAgeWidget;
