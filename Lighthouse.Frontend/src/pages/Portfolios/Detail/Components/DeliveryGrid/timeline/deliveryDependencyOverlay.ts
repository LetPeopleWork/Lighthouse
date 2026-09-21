import type { IFeature } from "../../../../../../models/Feature";
import {
	type IFeatureDependency,
	isWorthWarningAbout,
} from "../../../../../../models/FeatureDependency";
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
): DependencyOverlay {
	const edges: DrawnDependency[] = [];
	const marks = new Map<number, BarMark>();

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

	// Walking the bars rather than the Features keeps board order and leaves out anything the timeline
	// could not place: a line needs a bar at both of its ends.
	for (const bar of timeline.bars) {
		const waiting = featuresById.get(bar.featureId);

		for (const dependency of waiting?.dependsOn ?? []) {
			// Matched as written. Reference ids arrive already normalised, and folding case here in case
			// they did not would hide the day they stop being — a match that fails is silent, and every
			// dependency in the Delivery would read as one pointing outside it.
			const blocker = blockersByReference.get(dependency.referenceId);

			if (blocker && drawn.has(blocker.id)) {
				edges.push({
					blockerFeatureId: blocker.id,
					waitingFeatureId: bar.featureId,
				});
				continue;
			}

			// A dependency the forecast refused to act on is explained by that refusal, which is what moved
			// the dates. That there is also nowhere to draw it only explains the picture, and saying both
			// would bury the half that matters.
			if (dependency.notHonouredReason) {
				continue;
			}

			noteOn(bar.featureId, {
				text: noBarSentence(dependency, blocker, couldNotBePlaced),
				isWarning: isWorthWarningAbout(dependency),
			});
		}
	}

	return { edges, marks };
}

/**
 * Why a dependency has no line, in words the reader can act on.
 *
 * Nothing here names the blocker of a withheld entry, and nothing reads the entry's own name in that
 * case: the point of withholding it is that this reader may not learn what it is, and a sentence is
 * as much of a leak as a link would be.
 *
 * Where the blocker is a Feature of this Delivery it is called what the board calls it. The row's copy
 * of that name was written elsewhere and drifts, and a reader matching this note against the chart has
 * only what the chart says.
 */
function noBarSentence(
	dependency: IFeatureDependency,
	blocker: IFeature | undefined,
	couldNotBePlaced: Set<number>,
): string {
	if (dependency.isWithheld) {
		return "Waiting on something you do not have access to.";
	}

	if (blocker && couldNotBePlaced.has(blocker.id)) {
		return `Waiting on ${blocker.name}, which has no forecast to place on this timeline.`;
	}

	return `Waiting on ${blocker?.name ?? dependency.name}, which is not on this timeline.`;
}
