import {
	Alert,
	Box,
	List,
	ListItem,
	ListItemText,
	ToggleButton,
	ToggleButtonGroup,
	Typography,
} from "@mui/material";
import type React from "react";
import { useMemo, useState } from "react";
import WorkItemsDialog from "../../../../../../components/Common/WorkItemsDialog/WorkItemsDialog";
import { useLicenseRestrictions } from "../../../../../../hooks/useLicenseRestrictions";
import type { IFeature } from "../../../../../../models/Feature";
import { getWorkItemName } from "../../../../../../utils/featureName";
import DeliveryGanttChart from "./DeliveryGanttChart";
import {
	buildDeliveryTimeline,
	DEFAULT_TIMELINE_PERCENTILE,
	TIMELINE_PERCENTILES,
	type TimelinePercentile,
} from "./deliveryTimelineModel";

export interface DeliveryTimelineTabProps {
	features: IFeature[];
	/** The Delivery's target, as the backend stored it. Absent when the Delivery has none. */
	targetDate?: Date;
	featuresTerm: string;
}

const PROBABILITY_LABEL_ID = "delivery-timeline-probability";

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
	const [selectedFeatureId, setSelectedFeatureId] = useState<number | null>(
		null,
	);

	// One reading of the clock, handed to both the legend and the chart, so they cannot disagree
	// about which day today is. Fixed for as long as the tab is open: a fresh Date on every render
	// is a fresh identity and would invalidate everything memoised against it, and the cost is only
	// that a tab left open across midnight keeps yesterday's marker until something redraws it.
	const today = useMemo(() => new Date(), []);

	// Held by id rather than by object, so the dialog follows a refreshed Feature instead of
	// showing the one that was on screen when it was opened.
	const selectedFeature = features.find(
		(feature) => feature.id === selectedFeatureId,
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
			<Box sx={{ display: "flex", alignItems: "center", gap: 1, mb: 2 }}>
				{/* "Probability" is what Settings already calls this number. Three buttons rather
				    than a dropdown because the whole value here is flicking between them and
				    watching every bar move; a dropdown hides two of the three behind a click. */}
				<Typography
					variant="body2"
					color="text.secondary"
					id={PROBABILITY_LABEL_ID}
				>
					Probability
				</Typography>
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
					aria-labelledby={PROBABILITY_LABEL_ID}
				>
					{TIMELINE_PERCENTILES.map((option) => (
						<ToggleButton key={option} value={option}>
							{`${option}%`}
						</ToggleButton>
					))}
				</ToggleButtonGroup>
			</Box>

			{bars.length > 0 ? (
				<DeliveryGanttChart
					bars={bars}
					targetDate={targetDate}
					today={today}
					onBarSelected={setSelectedFeatureId}
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

			<WorkItemsDialog
				title={
					selectedFeature
						? getWorkItemName(selectedFeature.name, selectedFeature.referenceId)
						: ""
				}
				items={selectedFeature ? [selectedFeature] : []}
				open={selectedFeature !== undefined}
				onClose={() => setSelectedFeatureId(null)}
			/>
		</Box>
	);
};

export default DeliveryTimelineTab;
