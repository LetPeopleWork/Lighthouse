import type { IFeature } from "../../../../../../models/Feature";
import type { DeliveryTimeline } from "./deliveryTimelineModel";

/** One Feature waiting on another, both of them drawn on this timeline. */
export interface DrawnDependency {
	blockerFeatureId: number;
	waitingFeatureId: number;
}

/** What a bar has to say about dependencies of its own that no line on the chart carries. */
export interface BarMark {
	notes: string[];
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
			}
		}
	}

	return { edges, marks };
}
