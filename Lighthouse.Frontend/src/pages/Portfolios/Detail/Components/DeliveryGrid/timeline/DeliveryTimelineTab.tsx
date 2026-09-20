import {
	Alert,
	Box,
	List,
	ListItem,
	ListItemText,
	ToggleButton,
	ToggleButtonGroup,
	Typography,
	useMediaQuery,
} from "@mui/material";
import type React from "react";
import { useMemo, useState } from "react";
import { useLicenseRestrictions } from "../../../../../../hooks/useLicenseRestrictions";
import type { IFeature } from "../../../../../../models/Feature";
import DeliveryGanttChart from "./DeliveryGanttChart";
import {
	buildDeliveryTimeline,
	DEFAULT_TIMELINE_PERCENTILE,
	TASK_PANE_MINIMUM_WIDTH,
	TIMELINE_PERCENTILES,
	type TimelinePercentile,
} from "./deliveryTimelineModel";

export interface DeliveryTimelineTabProps {
	features: IFeature[];
	/** The Delivery's target, as the backend stored it. Absent when the Delivery has none. */
	targetDate?: Date;
	featuresTerm: string;
}

const PREMIUM_NOTICE =
	"The delivery timeline is a premium feature. The forecasts behind it are not — they stay in the table.";

const DeliveryTimelineTab: React.FC<DeliveryTimelineTabProps> = ({
	features,
	targetDate,
	featuresTerm,
}) => {
	const { licenseStatus } = useLicenseRestrictions();
	const [percentile, setPercentile] = useState<TimelinePercentile>(
		DEFAULT_TIMELINE_PERCENTILE,
	);
	const hasRoomForTaskPane = useMediaQuery(
		`(min-width:${TASK_PANE_MINIMUM_WIDTH}px)`,
	);

	const { bars, unplaceable } = useMemo(
		() => buildDeliveryTimeline(features, percentile),
		[features, percentile],
	);

	if (!licenseStatus?.canUsePremiumFeatures) {
		return (
			<Box sx={{ p: 2 }}>
				<Alert severity="info" data-testid="premium-feature-notice">
					{PREMIUM_NOTICE}
				</Alert>
			</Box>
		);
	}

	return (
		<Box sx={{ p: 2 }} data-testid="delivery-timeline-tab">
			<ToggleButtonGroup
				exclusive
				size="small"
				value={percentile}
				onChange={(_, chosen: TimelinePercentile | null) => {
					// Null arrives when the active button is clicked again. A timeline with no
					// percentile selected would have nothing to draw, so the choice stands.
					if (chosen !== null) {
						setPercentile(chosen);
					}
				}}
				aria-label="Confidence level"
				sx={{ mb: 2 }}
			>
				{TIMELINE_PERCENTILES.map((option) => (
					<ToggleButton key={option} value={option}>
						{`${option}%`}
					</ToggleButton>
				))}
			</ToggleButtonGroup>

			{bars.length > 0 ? (
				<DeliveryGanttChart
					bars={bars}
					targetDate={targetDate}
					showTaskPane={hasRoomForTaskPane}
				/>
			) : (
				<Typography variant="body2" color="text.secondary">
					{`None of these ${featuresTerm} can be placed on a timeline yet.`}
				</Typography>
			)}

			{unplaceable.length > 0 && (
				<Box sx={{ mt: 2 }} data-testid="timeline-unplaceable">
					<Typography variant="subtitle2">
						{`Not on the timeline (${unplaceable.length})`}
					</Typography>
					<List dense disablePadding>
						{unplaceable.map((feature) => (
							<ListItem key={feature.featureId} disableGutters>
								<ListItemText
									primary={feature.name}
									secondary={feature.reason}
								/>
							</ListItem>
						))}
					</List>
				</Box>
			)}
		</Box>
	);
};

export default DeliveryTimelineTab;
