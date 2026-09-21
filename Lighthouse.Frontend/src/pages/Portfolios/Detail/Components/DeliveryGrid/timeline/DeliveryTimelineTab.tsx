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
import WorkItemsDialog, {
	type WarningsColumnDescriptor,
} from "../../../../../../components/Common/WorkItemsDialog/WorkItemsDialog";
import { useLicenseRestrictions } from "../../../../../../hooks/useLicenseRestrictions";
import type { IFeature } from "../../../../../../models/Feature";
import { TERMINOLOGY_KEYS } from "../../../../../../models/TerminologyKeys";
import type { IWorkItem } from "../../../../../../models/WorkItem";
import { useTerminology } from "../../../../../../services/TerminologyContext";
import { getWorkItemName } from "../../../../../../utils/featureName";
import {
	type FeatureWarningInput,
	type FeatureWarningTerms,
	featureWarningSentences,
} from "../../../../../../utils/features/featureWarningSentences";
import DeliveryGanttChart from "./DeliveryGanttChart";
import {
	type BarMark,
	type BarNote,
	buildDependencyOverlay,
} from "./deliveryDependencyOverlay";
import {
	buildDeliveryTimeline,
	DEFAULT_TIMELINE_PERCENTILE,
	TIMELINE_PERCENTILES,
	type TimelinePercentile,
} from "./deliveryTimelineModel";
import { TimelineBarMarks } from "./TimelineBarContent";

export interface DeliveryTimelineTabProps {
	features: IFeature[];
	/** The Delivery's target, as the backend stored it. Absent when the Delivery has none. */
	targetDate?: Date;
	featuresTerm: string;
}

const PROBABILITY_LABEL_ID = "delivery-timeline-probability";

const PREMIUM_NOTICE =
	"The delivery timeline is a premium feature. The forecasts behind it are not — they stay in the table.";

const warningInputFor = (feature: IFeature): FeatureWarningInput => ({
	isDoneWithRemainingWork:
		feature.stateCategory === "Done" &&
		feature.getRemainingWorkForFeature() > 0,
	isUsingDefaultFeatureSize: feature.isUsingDefaultFeatureSize,
	dependencies: feature.dependsOn,
});

/**
 * The same sentences the Feature table shows, asked for in the same way, so a Feature cannot read as
 * clean in one place and marked in the other. What the bar says about where a blocker was drawn stays
 * on the bar: this column is shown by fifteen other screens that have no timeline, and a sound
 * dependency listed under a heading that says "Warnings" is a false alarm on every one of them.
 */
const warningsColumnFor = (
	features: IFeature[],
	terms: FeatureWarningTerms,
): WarningsColumnDescriptor => ({
	headerName: "Warnings",
	description: `What is worth checking about this ${terms.featureTerm}`,
	warningsFor: (item: IWorkItem) => {
		const feature = features.find((candidate) => candidate.id === item.id);

		if (!feature) {
			return [];
		}

		return featureWarningSentences(warningInputFor(feature), terms);
	},
});

/**
 * What one bar has to say for itself: everything the Feature table would warn about, and then what
 * this chart alone knows - where a blocker it waits on ended up.
 *
 * Both, rather than either. A bar showing only its dependencies would read as clean beside a table
 * row marked for a default size, and a bar showing only the warnings would leave a reader hunting
 * for a line that was never drawn.
 */
const barMarksFor = (
	features: IFeature[],
	dependencyMarks: ReadonlyMap<number, BarMark>,
	terms: FeatureWarningTerms,
): Map<number, BarMark> => {
	const marks = new Map<number, BarMark>();

	for (const feature of features) {
		const notes: BarNote[] = [
			...featureWarningSentences(warningInputFor(feature), terms).map(
				(text) => ({ text, isWarning: true }),
			),
			// Every dependency worth warning about is already in the sentences above, said in the
			// words the table uses for it. What is left here is the chart's own account of a wait
			// there is nothing wrong with, which no warning should be raised over.
			...(dependencyMarks.get(feature.id)?.notes ?? []).filter(
				(note) => !note.isWarning,
			),
		];

		// A bar with nothing to say stays absent rather than arriving with an empty list, which a
		// bar would draw as a symbol with nothing behind it.
		if (notes.length > 0) {
			marks.set(feature.id, { notes });
		}
	}

	return marks;
};

const DeliveryTimelineTab: React.FC<DeliveryTimelineTabProps> = ({
	features,
	targetDate,
	featuresTerm,
}) => {
	const { licenseStatus } = useLicenseRestrictions();
	const { getTerm } = useTerminology();
	const featureTerm = getTerm(TERMINOLOGY_KEYS.FEATURE);
	const portfolioTerm = getTerm(TERMINOLOGY_KEYS.PORTFOLIO);
	const workItemsTerm = getTerm(TERMINOLOGY_KEYS.WORK_ITEMS);
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
	const timeline = useMemo(
		() => buildDeliveryTimeline(features, percentile),
		[features, percentile],
	);
	const { bars, unplaceable } = timeline;

	const { edges, marks, chartNote } = useMemo(
		() =>
			buildDependencyOverlay(features, timeline, {
				featureTerm,
				portfolioTerm,
			}),
		[features, timeline, featureTerm, portfolioTerm],
	);

	const barMarks = useMemo(
		() =>
			barMarksFor(features, marks, {
				workItemsTerm,
				featureTerm,
				portfolioTerm,
			}),
		[features, marks, workItemsTerm, featureTerm, portfolioTerm],
	);

	const warningsColumn = useMemo(
		() =>
			warningsColumnFor(features, {
				workItemsTerm,
				featureTerm,
				portfolioTerm,
			}),
		[features, workItemsTerm, featureTerm, portfolioTerm],
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

			{/* Said once, above the chart, because every bar would otherwise carry the same words -
			    and a reader who is told nothing sees a chart whose lines silently vanished. */}
			{chartNote && (
				<Typography
					variant="body2"
					color="text.secondary"
					sx={{ mb: 1 }}
					data-testid="timeline-chart-note"
				>
					{chartNote}
				</Typography>
			)}

			{bars.length > 0 ? (
				<TimelineBarMarks marks={barMarks}>
					<DeliveryGanttChart
						bars={bars}
						links={edges}
						targetDate={targetDate}
						today={today}
						onBarSelected={setSelectedFeatureId}
					/>
				</TimelineBarMarks>
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
				warningsColumn={warningsColumn}
				open={selectedFeature !== undefined}
				onClose={() => setSelectedFeatureId(null)}
			/>
		</Box>
	);
};

export default DeliveryTimelineTab;
