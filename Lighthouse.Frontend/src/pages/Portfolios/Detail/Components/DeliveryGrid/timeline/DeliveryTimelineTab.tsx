import {
	Alert,
	Box,
	Divider,
	FormControlLabel,
	List,
	ListItem,
	ListItemText,
	Switch,
	ToggleButton,
	ToggleButtonGroup,
	Typography,
} from "@mui/material";
import type React from "react";
import { useCallback, useMemo, useState } from "react";
import WorkItemsDialog, {
	type WarningsColumnDescriptor,
} from "../../../../../../components/Common/WorkItemsDialog/WorkItemsDialog";
import { useLicenseRestrictions } from "../../../../../../hooks/useLicenseRestrictions";
import type { IEntityReference } from "../../../../../../models/EntityReference";
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
import { buildDependencyOverlay } from "./deliveryDependencyOverlay";
import {
	buildDeliveryTeamLanes,
	type TeamColour,
	type UnlanedTeam,
} from "./deliveryTeamLanes";
import {
	buildDeliveryTimeline,
	DEFAULT_TIMELINE_PERCENTILE,
	TIMELINE_PERCENTILES,
	type TimelinePercentile,
} from "./deliveryTimelineModel";
import {
	type BarMark,
	type BarNote,
	TimelineBarMarks,
} from "./TimelineBarContent";

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

const PROBABILITY_LABEL_ID = "delivery-timeline-probability";

const SHOW_TEAMS_KEY = "lighthouse:deliveryTimeline:showTeams";

/**
 * Whether this reader has asked to see the Teams behind each bar, remembered for them across
 * visits and across Deliveries.
 *
 * One key for the reader rather than one per Delivery: "show me Teams" is a property of the person
 * reading, the same call column visibility already makes. Three things about how it is read are
 * each the difference between working and quietly wrong.
 *
 * The stored value is **compared as a string and never coerced**. `localStorage` hands back the
 * text "false", and that text is truthy — so a coerced read turns the preference on and then can
 * never turn it off again, for as long as the key exists.
 *
 * It is read while the state is first created rather than in an effect, because an effect applies
 * the stored value one render late. That is invisible when what changes is a colour and very
 * visible when it is the height of a chart that roughly doubles.
 *
 * And every access is wrapped. Private browsing and blocked site data make these calls throw, and
 * an unguarded read takes the whole Portfolio accordion down with it.
 */
function useShowTeams(): { showTeams: boolean; toggleShowTeams: () => void } {
	const [showTeams, setShowTeams] = useState<boolean>(() => {
		try {
			return localStorage.getItem(SHOW_TEAMS_KEY) === "true";
		} catch {
			return false;
		}
	});

	const toggleShowTeams = useCallback(() => {
		setShowTeams((previous) => {
			const next = !previous;

			try {
				localStorage.setItem(SHOW_TEAMS_KEY, String(next));
			} catch {
				// Storage that will not take the choice costs this reader the memory of it, not the view.
			}

			return next;
		});
	}, []);

	return { showTeams, toggleShowTeams };
}

/**
 * Why a Feature's bar reaches past the rows beneath it, at both ends.
 *
 * This is the first thing a reader asks on seeing the split, and without an answer it reads as a
 * defect - the bar looks like it is claiming work nobody is doing. Said plainly, as the thing
 * rather than as the arithmetic behind it.
 */
const barsReachPastTheirTeams = (
	featureTerm: string,
	teamTerm: string,
	teamsTerm: string,
) =>
	`A ${featureTerm} starts when its first ${teamTerm} starts and finishes when its last one finishes, so its bar reaches a little past the ${teamsTerm} beneath it.`;

const premiumNoticeFor = (deliveryTerm: string) =>
	`The ${deliveryTerm} timeline is a premium feature. The forecasts behind it are not — they stay in the table.`;

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

interface TeamNotes {
	byFeature: ReadonlyMap<number, UnlanedTeam[]>;
	/**
	 * Whether those Teams are named along the bar as well as in its hover text. Only while the
	 * Teams are being read: every other Team on that Feature then has a row with its name written
	 * along it, and the one without a row is the only one a reader would have to hover to find.
	 */
	nameThemOnTheBar: boolean;
}

/**
 * What one bar has to say for itself: everything the Feature table would warn about, then what this
 * chart alone knows - where a blocker it waits on ended up, and which of its Teams has no row.
 *
 * All of them, rather than any one. A bar showing only its dependencies would read as clean beside a
 * table row marked for a default size; one showing only the warnings would leave a reader hunting
 * for a line that was never drawn; and one saying nothing about a Team without a row would show
 * fewer Teams than the Feature has and never admit it.
 */
const barMarksFor = (
	features: IFeature[],
	dependencyNotes: ReadonlyMap<number, string[]>,
	terms: FeatureWarningTerms,
	teamNotes: TeamNotes,
): Map<number, BarMark> => {
	const marks = new Map<number, BarMark>();

	for (const feature of features) {
		const unlaned = teamNotes.byFeature.get(feature.id) ?? [];

		const notes: BarNote[] = [
			...featureWarningSentences(warningInputFor(feature), terms).map(
				(text) => ({ text, isWarning: true }),
			),
			// Every dependency worth warning about is already in the sentences above, said in the
			// words the table uses for it. What is left here is the chart's own account of a wait
			// there is nothing wrong with, which no warning should be raised over.
			...(dependencyNotes.get(feature.id) ?? []).map((text) => ({
				text,
				isWarning: false,
			})),
			...unlaned.map((team) => team.note),
		];

		// A bar with nothing to say stays absent rather than arriving with an empty list, which a
		// bar would draw as a symbol with nothing behind it.
		if (notes.length > 0) {
			marks.set(feature.id, {
				notes,
				namesOnTheBar: teamNotes.nameThemOnTheBar
					? unlaned.map((team) => team.teamName)
					: [],
			});
		}
	}

	return marks;
};

/**
 * Which colour stands for which Team.
 *
 * Not decoration, and not redundant with the names written along the rows: a row is only as wide as
 * its Team's span, so at the widths this chart actually gets those names are routinely cut to a few
 * characters. Here every Team is named in full, once, at a width nothing competes for.
 */
const TeamLegend: React.FC<{ teams: TeamColour[] }> = ({ teams }) => (
	<Box
		data-testid="timeline-team-legend"
		sx={{ display: "flex", flexWrap: "wrap", alignItems: "center", gap: 1.5 }}
	>
		{teams.map((team) => (
			<Box
				key={team.teamId}
				sx={{ display: "flex", alignItems: "center", gap: 0.5 }}
			>
				{/* The name beside it carries the meaning, so the patch itself is shown to the eye
				    and hidden from anything reading the page aloud. */}
				<Box
					aria-hidden="true"
					sx={{
						width: 12,
						height: 12,
						borderRadius: 0.5,
						flexShrink: 0,
						backgroundColor: team.color,
					}}
				/>
				<Typography variant="caption" color="text.secondary">
					{team.teamName}
				</Typography>
			</Box>
		))}
	</Box>
);

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
	const { showTeams, toggleShowTeams } = useShowTeams();
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

	const barMarks = useMemo(
		() =>
			barMarksFor(
				features,
				marks,
				{ workItemsTerm, featureTerm, portfolioTerm },
				{
					byFeature: teamsOnTheChart.unlanedTeams,
					nameThemOnTheBar: showTeams,
				},
			),
		[
			features,
			marks,
			workItemsTerm,
			featureTerm,
			portfolioTerm,
			teamsOnTheChart,
			showTeams,
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

				{/* Offered only where showing the Teams would actually change something here. A
				    control that cannot is still worth the click that proves it, and a reader who gets
				    nothing back concludes the chart is broken rather than that the question does not
				    apply. A switch rather than a fourth button beside the three: that group means
				    "pick one of these", and this is on or off - which is also why it is set apart
				    from them rather than sitting flush against the group as a fourth member of it. */}
				{teamsOnTheChart.canShowTeams && (
					<>
						<Divider
							orientation="vertical"
							flexItem
							sx={{ mx: 1.5, my: 0.5 }}
						/>
						<FormControlLabel
							control={
								<Switch
									size="small"
									checked={showTeams}
									onChange={toggleShowTeams}
								/>
							}
							label={`Show ${teamsTerm}`}
						/>
					</>
				)}
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

			{/* Both of these belong above the chart rather than under it: the chart can run to
			    twenty rows, and a key a reader has to scroll past the picture to reach is a key they
			    read once. They are deliberately not the `timeline-chart-note` slot above - that one
			    reports a condition this Delivery happens to be in, and these two are always true
			    while the Teams are shown. */}
			{showTeams && teamsOnTheChart.legend.length > 0 && (
				<Box sx={{ mb: 1.5 }}>
					{teamsOnTheChart.lanes.length > 0 && (
						<Typography
							variant="body2"
							color="text.secondary"
							sx={{ mb: 1 }}
							data-testid="timeline-team-span-note"
						>
							{barsReachPastTheirTeams(featureTerm, teamTerm, teamsTerm)}
						</Typography>
					)}
					<TeamLegend teams={teamsOnTheChart.legend} />
				</Box>
			)}

			{bars.length > 0 ? (
				<TimelineBarMarks marks={barMarks}>
					<DeliveryGanttChart
						bars={bars}
						links={edges}
						// Absent rather than hidden while the switch is off, so the chart is then the
						// same chart it was before any of this existed.
						lanes={showTeams ? teamsOnTheChart.lanes : undefined}
						barTeams={showTeams ? teamsOnTheChart.barTeams : undefined}
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
