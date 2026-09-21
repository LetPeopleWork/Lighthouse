import type { IFeature } from "../../../../../../models/Feature";
import {
	type IFeatureDependency,
	isSetAside,
	isWorthWarningAbout,
} from "../../../../../../models/FeatureDependency";
import {
	type DependencyTerms,
	noForecastToPlaceSentence,
	notOnThisTimelineSentence,
	reasonSentence,
	waitedOnName,
	waitingSentence,
	withheldSentence,
} from "../../../../../../utils/dependencies/dependencySentences";
import type { DeliveryTimeline } from "./deliveryTimelineModel";

/** One Feature waiting on another, both of them drawn on this timeline. */
export interface DrawnDependency {
	blockerFeatureId: number;
	waitingFeatureId: number;
}

export interface DependencyOverlay {
	edges: DrawnDependency[];
	/**
	 * What a bar has to say about a wait there is nothing wrong with, keyed by the waiting Feature's
	 * id. Nothing here is a warning: anything worth warning about is written by the Feature table's
	 * own sentences, and saying it twice in two voices is how the two screens come to disagree.
	 *
	 * A bar with nothing to say is absent from this map rather than present with an empty list,
	 * which a component would draw as a badge with nothing in it.
	 */
	marks: Map<number, string[]>;
	/** The one thing that is true of the whole chart rather than of any single bar, or nothing. */
	chartNote: string | null;
}

/**
 * How many lines the chart will draw before it gives up on lines and says it in words instead.
 *
 * Counted off the live instance this was sized against: 83 Features, twelve dependencies between them,
 * and no Feature waiting on more than two others. Reaching forty would take twenty dependency-carrying
 * Features in one Delivery, which is more than that entire instance has — so this is insurance against
 * a chart nobody has seen yet, not a case the drawing is designed around.
 */
const DEFAULT_DRAWN_EDGE_LIMIT = 40;

/**
 * A Feature is waiting on another Feature of this Delivery, and both of them have a bar.
 *
 * The blocker is named by its reference id and nothing else — the two names are written by different
 * people at different times and drift apart, so joining on them would connect the wrong pair or, more
 * often, no pair at all.
 */
export function buildDependencyOverlay(
	features: IFeature[],
	timeline: DeliveryTimeline,
	terms: DependencyTerms,
	drawnEdgeLimit = DEFAULT_DRAWN_EDGE_LIMIT,
): DependencyOverlay {
	const delivery = indexDelivery(features, timeline);
	const barDependencies = [...dependenciesOfDrawnBars(timeline, delivery)];

	// Too many lines is a thicket, and a thicket says less than no lines at all. Decided once for the
	// whole chart: were it decided bar by bar, a bar without lines would mean either "nothing to draw"
	// or "too much to draw", and the reader would have no way of telling which.
	const linesAreSuppressed =
		barDependencies.filter((barDependency) =>
			joinedBlockerFor(barDependency, delivery),
		).length > drawnEdgeLimit;

	const edges: DrawnDependency[] = [];
	const marks = new Map<number, string[]>();
	let chartNote: string | null = null;

	for (const barDependency of barDependencies) {
		const { waitingFeatureId, dependency, blocker } = barDependency;

		// Where the Feature waited on is one of this Delivery's own, it is called what the board calls
		// it: the dependency entry's copy of that name was written elsewhere and drifts, and a reader
		// matching a sentence against the chart has only what the chart says.
		const waitedOn = waitedOnName(dependency, terms, blocker?.name);

		// Every edge of a Portfolio that has set its dependencies aside comes back saying the same
		// thing, so a mark per bar would put identical words on every bar and teach the reader that
		// marks are not worth reading. Said once, above the chart, instead.
		if (isSetAside(dependency)) {
			chartNote = reasonSentence("IgnoredByPortfolio", waitedOn, terms);
			continue;
		}

		const joinedBlocker = joinedBlockerFor(barDependency, delivery);
		const drawnBlocker = linesAreSuppressed ? undefined : joinedBlocker;

		if (drawnBlocker) {
			edges.push({ blockerFeatureId: drawnBlocker.id, waitingFeatureId });
		}

		// What is wrong with a dependency is said by the sentences the Feature table writes, which
		// this bar already carries. Saying it a second time in words chosen here is how the bar and
		// the table come to describe the same wait differently.
		if (isWorthWarningAbout(dependency)) {
			continue;
		}

		const note = noteFor(dependency, terms, {
			waitedOn,
			hasALine: drawnBlocker !== undefined,
			blockerIsDrawnHere: joinedBlocker !== undefined,
			blockerHasNoForecast:
				blocker !== undefined && delivery.unplaceable.has(blocker.id),
		});

		if (note) {
			addNote(marks, waitingFeatureId, note);
		}
	}

	return { edges, marks, chartNote };
}

/** The Features of this Delivery, indexed every way the overlay has to ask about them. */
interface DeliveryIndex {
	byId: Map<number, IFeature>;
	/**
	 * Keyed as the reference id is written. Those ids arrive already normalised, and folding case here
	 * in case they did not would hide the day they stop being — a match that fails is silent, and every
	 * dependency in the Delivery would read as one pointing outside it.
	 *
	 * A dependency the reader may not see arrives with an empty reference id, and it is left out
	 * entirely: admitting "" as a key would join every one of them to whichever Feature happens to
	 * carry an empty one.
	 */
	byReferenceId: Map<string, IFeature>;
	/** Which Features the timeline placed and which it could not, named as the timeline names them. */
	drawn: Set<number>;
	unplaceable: Set<number>;
}

function indexDelivery(
	features: IFeature[],
	timeline: DeliveryTimeline,
): DeliveryIndex {
	const byReferenceId = new Map<string, IFeature>();

	for (const feature of features) {
		if (feature.referenceId) {
			byReferenceId.set(feature.referenceId, feature);
		}
	}

	return {
		byId: new Map(features.map((feature) => [feature.id, feature])),
		byReferenceId,
		drawn: new Set(timeline.bars.map((bar) => bar.featureId)),
		unplaceable: new Set(
			timeline.unplaceable.map((feature) => feature.featureId),
		),
	};
}

/** One dependency of one drawn bar, already resolved against the Delivery it was declared in. */
interface BarDependency {
	waitingFeatureId: number;
	dependency: IFeatureDependency;
	/** The Feature this Delivery holds under that reference id, or nothing where it holds none. */
	blocker?: IFeature;
}

/**
 * Every dependency of every Feature the timeline managed to place, in board order, each paired with
 * the bar that would carry a mark about it. Walking the bars rather than the Features leaves out
 * anything the timeline could not place: a line needs a bar at both of its ends.
 */
function* dependenciesOfDrawnBars(
	timeline: DeliveryTimeline,
	delivery: DeliveryIndex,
): Generator<BarDependency> {
	for (const bar of timeline.bars) {
		const waiting = delivery.byId.get(bar.featureId);

		for (const dependency of waiting?.dependsOn ?? []) {
			yield {
				waitingFeatureId: bar.featureId,
				dependency,
				blocker: delivery.byReferenceId.get(dependency.referenceId),
			};
		}
	}
}

/**
 * The blocker a line would run to, or nothing at all. One is drawn only where the forecast acted on the
 * wait and both ends have a bar: a line between two bars reads as the reason one of them sits where it
 * does, and for a wait the schedule never took, that reading is false.
 */
function joinedBlockerFor(
	{ dependency, blocker }: BarDependency,
	delivery: DeliveryIndex,
): IFeature | undefined {
	return dependency.notHonouredReason === null &&
		blocker &&
		delivery.drawn.has(blocker.id)
		? blocker
		: undefined;
}

function addNote(
	marks: Map<number, string[]>,
	featureId: number,
	note: string,
): void {
	const notes = marks.get(featureId);

	if (notes) {
		notes.push(note);
		return;
	}

	marks.set(featureId, [note]);
}

/** Where a dependency ended up on the chart, which is what decides what there is left to say. */
interface DependencyPlacement {
	waitedOn: string;
	hasALine: boolean;
	/** The blocker has a bar here, whether or not a line was actually drawn to it. */
	blockerIsDrawnHere: boolean;
	blockerHasNoForecast: boolean;
}

/**
 * Where a wait there is nothing wrong with ended up, or nothing at all when a line between two bars
 * has already said it. Only this chart knows any of it; everything else a reader should be told about
 * a dependency is said by the sentences the Feature table writes.
 */
function noteFor(
	dependency: IFeatureDependency,
	terms: DependencyTerms,
	placement: DependencyPlacement,
): string | null {
	// A line was drawn, so the forecast did wait and the picture is complete.
	if (placement.hasALine) {
		return null;
	}

	// Both bars are here and the forecast did wait, but the chart stopped drawing its lines. Said in
	// words instead, because a bar with neither a line nor a mark reads as a bar that waits on nothing.
	if (placement.blockerIsDrawnHere) {
		return waitingSentence(placement.waitedOn);
	}

	if (dependency.isWithheld) {
		return withheldSentence(terms);
	}

	return placement.blockerHasNoForecast
		? noForecastToPlaceSentence(placement.waitedOn)
		: notOnThisTimelineSentence(placement.waitedOn);
}
