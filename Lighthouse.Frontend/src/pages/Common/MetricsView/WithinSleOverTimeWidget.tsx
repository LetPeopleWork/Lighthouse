import {
	Box,
	Card,
	CardContent,
	ToggleButton,
	ToggleButtonGroup,
	Typography,
	useTheme,
} from "@mui/material";
import { LineChart } from "@mui/x-charts";
import type React from "react";
import type { MetricsOwnerKey } from "../../../hooks/metricsOwnerKey";
import type { IFeature } from "../../../models/Feature";
import { minimumClosedOf } from "../../../models/Metrics/WithinSleMinimumClosed";
import type { WithinSleHorizon } from "../../../models/Metrics/WithinSleOverTime";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import type { IWorkItem } from "../../../models/WorkItem";
import type { IMetricsService } from "../../../services/Api/MetricsService";
import { UsageDataEventName } from "../../../services/Api/UsageDataService";
import { useTerminology } from "../../../services/TerminologyContext";
import { useUsageDataReporter } from "../../../services/UsageData/usageDataReporter";
import { OVER_TIME_EMPTY_COPY } from "./overTimeEmptyState";
import { useWithinSleOverTime } from "./useWithinSleOverTime";
import { FilledInDaysContext, WithinSleShareMark } from "./WithinSleShareMark";
import { useReportWidgetStatus } from "./widgetStatus";
import { withinSleLinesOf } from "./withinSleLines";

interface WithinSleOverTimeWidgetProps {
	owner: MetricsOwnerKey;
	metricsService: IMetricsService<IWorkItem | IFeature>;
	startDate: Date;
	endDate: Date;
}

const HORIZONS: readonly WithinSleHorizon[] = [30, 60, 90];

const WithinSleOverTimeWidget: React.FC<WithinSleOverTimeWidgetProps> = ({
	owner,
	metricsService,
	startDate,
	endDate,
}) => {
	const theme = useTheme();
	const { getTerm } = useTerminology();
	const sle = getTerm(TERMINOLOGY_KEYS.SLE);
	const workItems = getTerm(TERMINOLOGY_KEYS.WORK_ITEMS);
	const reportUsage = useUsageDataReporter();
	const { horizon, setHorizon, answer, status } = useWithinSleOverTime(
		owner,
		metricsService,
		startDate,
		endDate,
	);
	useReportWidgetStatus(status);

	const switchTo = (next: WithinSleHorizon) => {
		if (next === horizon) return;
		setHorizon(next);
		reportUsage({ name: UsageDataEventName.WithinSleHorizonSwitched });
	};

	const days = answer?.days ?? [];
	const lines = withinSleLinesOf(days);
	const anyTooFew = days.some((day) => day.verdict === "TooFew");
	const anyFilledIn = lines.filledIn.some(Boolean);

	const sentence = (text: string) => (
		<Typography
			variant="body2"
			color="text.secondary"
			sx={{ py: 4, textAlign: "center" }}
		>
			{text}
		</Typography>
	);

	const body = () => {
		if (answer === undefined) return null;
		if (answer.currentSlePercentile === null) {
			return sentence(
				`No ${sle} set — define one in settings to see how many ${workItems} finish within it over time`,
			);
		}
		if (lines.dates.length === 0) return sentence(OVER_TIME_EMPTY_COPY);
		return (
			<>
				<Box sx={{ flex: 1, minHeight: 0 }}>
					<FilledInDaysContext value={lines.filledIn}>
						<LineChart
							style={{ height: "100%", width: "100%" }}
							xAxis={[{ data: lines.dates, scaleType: "point", label: "Date" }]}
							yAxis={[{ min: 0, max: 100, label: `Within ${sle} (%)` }]}
							series={[
								{
									label: `Within ${sle}`,
									data: lines.share,
									color: theme.palette.primary.main,
									showMark: true,
								},
								{
									label: "Target",
									data: lines.target,
									color: theme.palette.text.secondary,
									curve: "stepAfter",
									showMark: false,
								},
							]}
							slots={{ mark: WithinSleShareMark }}
						/>
					</FilledInDaysContext>
				</Box>
				{anyFilledIn && (
					<Typography variant="caption" color="text.secondary">
						{`○ filled in from history (measured against today's ${sle})`}
					</Typography>
				)}
				{anyTooFew && (
					<Typography variant="caption" color="text.secondary">
						{`No point is drawn on a day with fewer than ${minimumClosedOf(answer)} closed ${workItems}.`}
					</Typography>
				)}
			</>
		);
	};

	return (
		<Card sx={{ p: 2, borderRadius: 2, height: "100%" }}>
			<CardContent
				sx={{ height: "100%", display: "flex", flexDirection: "column" }}
			>
				<Box
					sx={{
						display: "flex",
						justifyContent: "space-between",
						alignItems: "center",
						gap: 1,
						flexWrap: "wrap",
					}}
				>
					<Typography variant="h6">{`Within ${sle} Over Time`}</Typography>
					<ToggleButtonGroup
						size="small"
						exclusive
						value={horizon}
						aria-label={`Within ${sle} horizon`}
					>
						{HORIZONS.map((horizonDays) => (
							<ToggleButton
								key={horizonDays}
								size="small"
								value={horizonDays}
								selected={horizon === horizonDays}
								onClick={() => switchTo(horizonDays)}
							>
								{`${horizonDays} days`}
							</ToggleButton>
						))}
					</ToggleButtonGroup>
				</Box>
				{body()}
			</CardContent>
		</Card>
	);
};

export default WithinSleOverTimeWidget;
