import {
	Alert,
	Box,
	List,
	ListItem,
	ListItemText,
	Typography,
} from "@mui/material";
import type React from "react";
import { useMemo, useState } from "react";
import WorkItemsDialog from "../../../../../../components/Common/WorkItemsDialog/WorkItemsDialog";
import { useLicenseRestrictions } from "../../../../../../hooks/useLicenseRestrictions";
import type { IEntityReference } from "../../../../../../models/EntityReference";
import type { IFeature } from "../../../../../../models/Feature";
import { TERMINOLOGY_KEYS } from "../../../../../../models/TerminologyKeys";
import { useTerminology } from "../../../../../../services/TerminologyContext";
import { getWorkItemName } from "../../../../../../utils/featureName";
import { cannotBeForecast } from "../../../../../../utils/forecast/cannotForecast";
import DeliveryGanttChart from "./DeliveryGanttChart";
import {
	anythingToWarnAbout,
	barMarksFor,
	warningsColumnFor,
} from "./deliveryBarMarks";
import { buildDeliveryBarStatuses } from "./deliveryBarStatus";
import { buildDependencyOverlay } from "./deliveryDependencyOverlay";
import { buildDeliveryTeamLanes, type UnlanedTeam } from "./deliveryTeamLanes";
import {
	buildDeliveryTimeline,
	DEFAULT_TIMELINE_PERCENTILE,
	type TimelinePercentile,
} from "./deliveryTimelineModel";
import { TimelineBarMarks } from "./TimelineBarContent";
import TimelineControls, { type TimelineViewOption } from "./TimelineControls";
import TimelineLegend, { type LegendEntry } from "./TimelineLegend";
import { STATUS_COLORS } from "./timelineMarkers";
import { type TimelineView, useTimelineView } from "./timelineView";

export interface DeliveryTimelineTabProps {
	features: IFeature[];
	/** The Delivery's target, as the backend stored it. Absent when the Delivery has none. */
	targetDate?: Date;
	featuresTerm: string;
	/**
	 * The Portfolio's Teams, which is the only place a Team's name exists on this screen: a
	 * Feature's forecast carries Team ids and no names at all.
	 */
	teams: IEntityReference[];
}

/** One empty map rather than a fresh one per render, which would re-run every memo below it. */
const NO_TEAM_NOTES: ReadonlyMap<number, UnlanedTeam[]> = new Map();

/**
 * What each colour on a bar means, in the order a reader meets trouble in.
 *
 * All three every time the key is shown, rather than only the ones this Delivery happens to be
 * wearing. A key that listed different things at 70 and at 95 would teach the reader a different
 * scheme each time they moved the probability, and the point of a key is that it does not move.
 *
 * It is not dropped as self-explanatory, and the reason is the second green: a finished bar and an
 * on-track bar are both green, and nothing but this says which is which.
 */
const STATUS_LEGEND: LegendEntry[] = [
	{
		id: "finished",
		label: "Finished",
		color: STATUS_COLORS.finished,
	},
	{
		id: "endsAfterTarget",
		label: "Finishes after the target date",
		color: STATUS_COLORS.endsAfterTarget,
	},
	{
		id: "startsAfterTarget",
		label: "Not even started by the target date",
		color: STATUS_COLORS.startsAfterTarget,
	},
];

const premiumNoticeFor = (deliveryTerm: string) =>
	`The ${deliveryTerm} timeline is a premium feature. The forecasts behind it are not — they stay in the table.`;

const DeliveryTimelineTab: React.FC<DeliveryTimelineTabProps> = ({
	features,
	targetDate,
	featuresTerm,
	teams,
}) => {
	const { licenseStatus } = useLicenseRestrictions();
	const { getTerm } = useTerminology();
	const featureTerm = getTerm(TERMINOLOGY_KEYS.FEATURE);
	const portfolioTerm = getTerm(TERMINOLOGY_KEYS.PORTFOLIO);
	const workItemsTerm = getTerm(TERMINOLOGY_KEYS.WORK_ITEMS);
	const deliveryTerm = getTerm(TERMINOLOGY_KEYS.DELIVERY);
	const teamTerm = getTerm(TERMINOLOGY_KEYS.TEAM);
	const teamsTerm = getTerm(TERMINOLOGY_KEYS.TEAMS);
	const { view, chooseView } = useTimelineView();
	const [percentile, setPercentile] = useState<TimelinePercentile>(
		DEFAULT_TIMELINE_PERCENTILE,
	);
	const [selectedFeatureId, setSelectedFeatureId] = useState<number | null>(
		null,
	);

	// One reading of the clock for the whole tab, so nothing that marks today can disagree with
	// anything else about which day it is. Fixed for as long as the tab is open: a fresh Date on
	// every render is a fresh identity and would invalidate everything memoised against it, and the
	// cost is only that a tab left open across midnight keeps yesterday's marker until something
	// redraws it.
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

	const teamsOnTheChart = useMemo(
		() =>
			buildDeliveryTeamLanes(features, timeline, teams, percentile, {
				teamTerm,
				portfolioTerm,
			}),
		[features, timeline, teams, percentile, teamTerm, portfolioTerm],
	);

	const barStatusColors = useMemo(() => {
		const colours = new Map<number, string>();

		for (const [featureId, status] of buildDeliveryBarStatuses(
			bars,
			targetDate,
		)) {
			colours.set(featureId, STATUS_COLORS[status]);
		}

		return colours;
	}, [bars, targetDate]);

	// The Features a bar can be drawn for at all.
	//
	// Both switches below are counted over these rather than over every Feature, because a Feature
	// with no bar can carry no mark: counting it offers a control that does nothing when it is used,
	// which is the one thing the row of controls promises never to do.
	//
	// Asked as "could this ever be placed" rather than "is it placed right now". The second reads
	// the drawn bars, and which bars are drawn - and which of them cross the target - is exactly what
	// the probability buttons change. A control that came and went as the reader worked those
	// buttons would read as a fault in the page rather than as an answer to anything.
	const placeable = useMemo(
		() =>
			features.filter(
				(feature) =>
					!cannotBeForecast({
						teamsWithoutForecast: feature.teamsWithoutForecast ?? [],
					}),
			),
		[features],
	);

	const canShowStatus =
		targetDate !== undefined ||
		placeable.some((feature) => feature.closedDate != null);

	const canShowWarnings = useMemo(
		() =>
			anythingToWarnAbout(placeable, {
				workItemsTerm,
				featureTerm,
				portfolioTerm,
			}),
		[placeable, workItemsTerm, featureTerm, portfolioTerm],
	);

	const offeredViews: TimelineViewOption[] = [
		{ view: "none", label: "Nothing", offered: true },
		{
			view: "teams",
			label: teamsTerm,
			offered: teamsOnTheChart.canShowTeams,
		},
		{ view: "status", label: "Status", offered: canShowStatus },
		{ view: "warnings", label: "Warnings", offered: canShowWarnings },
	];

	// What this Delivery is actually showing, which is not always what the reader asked for. The
	// choice is one value for the whole page and the Deliveries on it differ: someone who asked for
	// the Teams and then opens one whose Features have a single Team each is shown nothing rather
	// than something they did not ask for. Their choice stays in storage untouched, so the Delivery
	// that can honour it still does.
	const showing: TimelineView = offeredViews.some(
		(option) => option.offered && option.view === view,
	)
		? view
		: "none";

	const barMarks = useMemo(
		() =>
			barMarksFor(
				features,
				marks,
				{ workItemsTerm, featureTerm, portfolioTerm },
				// Only while the Teams are shown. What these notes serve is the promise that a
				// split never shows fewer Teams than the Feature has - and with the Teams hidden
				// there is no split to disagree with. A sentence about a missing lane, on a chart
				// that has no lanes, names something the reader cannot see and contradicts the
				// one thing the switch promises: off is the chart exactly as it was.
				showing === "teams" ? teamsOnTheChart.unlanedTeams : NO_TEAM_NOTES,
				showing === "warnings",
			),
		[
			features,
			marks,
			workItemsTerm,
			featureTerm,
			portfolioTerm,
			teamsOnTheChart,
			showing,
		],
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
					{premiumNoticeFor(deliveryTerm)}
				</Alert>
			</Box>
		);
	}

	return (
		<Box sx={{ p: 2 }} data-testid="delivery-timeline-tab">
			<TimelineControls
				percentile={percentile}
				onPercentileChosen={setPercentile}
				view={showing}
				onViewChosen={chooseView}
				views={offeredViews}
			/>

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

			{/* Above the chart rather than under it: the chart can run to twenty rows, and a key a
			    reader has to scroll past the picture to reach is a key they read once. Deliberately
			    not the `timeline-chart-note` slot above - that one reports a condition this Delivery
			    happens to be in, and this is always true while the Teams are shown. */}
			{showing === "teams" && teamsOnTheChart.legend.length > 0 && (
				<Box sx={{ mb: 1.5 }}>
					<TimelineLegend
						testId="timeline-team-legend"
						entries={teamsOnTheChart.legend.map((team) => ({
							id: `team:${team.teamId}`,
							label: team.teamName,
							color: team.color,
						}))}
					/>
				</Box>
			)}

			{showing === "status" && (
				<Box sx={{ mb: 1.5 }}>
					<TimelineLegend
						testId="timeline-status-legend"
						entries={STATUS_LEGEND}
					/>
				</Box>
			)}

			{bars.length > 0 ? (
				<TimelineBarMarks marks={barMarks}>
					<DeliveryGanttChart
						bars={bars}
						links={edges}
						// Absent rather than hidden while the switch is off, so the chart is then the
						// same chart it was before any of this existed.
						lanes={showing === "teams" ? teamsOnTheChart.lanes : undefined}
						barTeams={
							showing === "teams" ? teamsOnTheChart.barTeams : undefined
						}
						// Absent rather than empty for anything the reader has not asked for, for
						// the same reason the lanes are: unasked is the chart exactly as it was.
						barStatusColors={showing === "status" ? barStatusColors : undefined}
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
