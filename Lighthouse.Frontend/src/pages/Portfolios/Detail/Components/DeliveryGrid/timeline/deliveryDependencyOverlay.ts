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
	positionedBelowSentence,
	reasonSentence,
	withheldName,
	withheldSentence,
} from "../../../../../../utils/dependencies/dependencySentences";
import type { DeliveryTimeline } from "./deliveryTimelineModel";

/** One Feature waiting on another, both of them drawn on this timeline. */
export interface DrawnDependency {
	blockerFeatureId: number;
	waitingFeatureId: number;
}

/**
 * One thing a bar has to say. Whether it is worth raising a warning about is decided by the same
 * predicate the Feature table asks, so one Feature cannot read as clean on one screen and marked on
 * the other.
 */
export interface BarNote {
	text: string;
	isWarning: boolean;
}

/** What a bar has to say about dependencies of its own that no line on the chart carries. */
export interface BarMark {
	notes: BarNote[];
}

export interface DependencyOverlay {
	edges: DrawnDependency[];
	/**
	 * Keyed by the waiting Feature's id. A bar with nothing to say is absent from this map rather
	 * than present with an empty list, which a component would draw as a badge with nothing in it.
	 */
	marks: Map<number, BarMark>;
	/** The one thing that is true of the whole chart rather than of any single bar, or nothing. */
	chartNote: string | null;
}

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
): DependencyOverlay {
	const edges: DrawnDependency[] = [];
	const marks = new Map<number, BarMark>();
	let chartNote: string | null = null;

	const featuresById = new Map<number, IFeature>(
		features.map((feature) => [feature.id, feature]),
	);

	const blockersByReference = new Map<string, IFeature>();
	for (const feature of features) {
		// A dependency the reader may not see arrives with an empty reference id. Admitting "" as a key
		// here would join every one of them to whichever Feature happens to carry an empty one.
		if (feature.referenceId) {
			blockersByReference.set(feature.referenceId, feature);
		}
	}

	const drawn = new Set(timeline.bars.map((bar) => bar.featureId));
	const couldNotBePlaced = new Set(
		timeline.unplaceable.map((feature) => feature.featureId),
	);

	const noteOn = (featureId: number, note: BarNote) => {
		const mark = marks.get(featureId);

		if (mark) {
			mark.notes.push(note);
			return;
		}

		marks.set(featureId, { notes: [note] });
	};

	for (const { waitingFeatureId, dependency } of dependenciesOfDrawnBars(
		timeline,
		featuresById,
	)) {
		// Matched as written. Reference ids arrive already normalised, and folding case here in case
		// they did not would hide the day they stop being — a match that fails is silent, and every
		// dependency in the Delivery would read as one pointing outside it.
		const blocker = blockersByReference.get(dependency.referenceId);

		// Where the Feature waited on is one of this Delivery's own, it is called what the board calls
		// it: the dependency entry's copy of that name was written elsewhere and drifts, and a reader
		// matching a sentence against the chart has only what the chart says.
		const waitedOn = dependency.isWithheld
			? withheldName(terms)
			: (blocker?.name ?? dependency.name);

		// Every edge of a Portfolio that has set its dependencies aside comes back saying the same
		// thing, so a mark per bar would put identical words on every bar and teach the reader that
		// marks are not worth reading. Said once, above the chart, instead.
		if (isSetAside(dependency)) {
			chartNote = reasonSentence("IgnoredByPortfolio", waitedOn, terms);
			continue;
		}

		// A line is drawn only where the forecast acted on the wait and both ends have a bar. A line
		// between two bars reads as the reason one of them sits where it does, and for a wait the
		// schedule never took, that reading is false.
		const joinedBlocker =
			dependency.notHonouredReason === null && blocker && drawn.has(blocker.id)
				? blocker
				: undefined;

		if (joinedBlocker) {
			edges.push({ blockerFeatureId: joinedBlocker.id, waitingFeatureId });
		}

		const note = noteFor(dependency, terms, {
			waitedOn,
			hasALine: joinedBlocker !== undefined,
			blockerHasNoForecast:
				blocker !== undefined && couldNotBePlaced.has(blocker.id),
		});

		if (note) {
			noteOn(waitingFeatureId, {
				text: note,
				isWarning: isWorthWarningAbout(dependency),
			});
		}
	}

	return { edges, marks, chartNote };
}

/**
 * Every dependency of every Feature the timeline managed to place, in board order, each paired with
 * the bar that would carry a mark about it. Walking the bars rather than the Features leaves out
 * anything the timeline could not place: a line needs a bar at both of its ends.
 */
function* dependenciesOfDrawnBars(
	timeline: DeliveryTimeline,
	featuresById: Map<number, IFeature>,
): Generator<{ waitingFeatureId: number; dependency: IFeatureDependency }> {
	for (const bar of timeline.bars) {
		const waiting = featuresById.get(bar.featureId);

		for (const dependency of waiting?.dependsOn ?? []) {
			yield { waitingFeatureId: bar.featureId, dependency };
		}
	}
}

/** Where a dependency ended up on the chart, which is what decides what there is left to say. */
interface DependencyPlacement {
	waitedOn: string;
	hasALine: boolean;
	blockerHasNoForecast: boolean;
}

/**
 * What the waiting bar has to say about one dependency, or nothing at all when a line between two bars
 * has already said it.
 */
function noteFor(
	dependency: IFeatureDependency,
	terms: DependencyTerms,
	placement: DependencyPlacement,
): string | null {
	const reason = dependency.notHonouredReason;

	// The refusal is what moved the dates. That there is also nowhere to draw the dependency only
	// explains the picture, and saying both would bury the half the reader can act on.
	if (reason) {
		return reasonSentence(reason, placement.waitedOn, terms);
	}

	// A line was drawn, so the forecast did wait and the picture is complete. It is said again in words
	// only where the board's own order contradicts the wait, because the Feature table already warns
	// about that, and one Feature reading as clean on one screen while it is marked on the other is
	// worse than either answer alone.
	if (placement.hasALine) {
		return dependency.blockerPositionedBelow
			? positionedBelowSentence(placement.waitedOn, terms)
			: null;
	}

	if (dependency.isWithheld) {
		return withheldSentence(terms);
	}

	return placement.blockerHasNoForecast
		? noForecastToPlaceSentence(placement.waitedOn)
		: notOnThisTimelineSentence(placement.waitedOn);
}
