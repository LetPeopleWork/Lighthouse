import type { WarningsColumnDescriptor } from "../../../../../../components/Common/WorkItemsDialog/WorkItemsDialog";
import type { IFeature } from "../../../../../../models/Feature";
import type { IWorkItem } from "../../../../../../models/WorkItem";
import {
	type FeatureWarningInput,
	type FeatureWarningTerms,
	featureWarningSentences,
} from "../../../../../../utils/features/featureWarningSentences";
import type { UnlanedTeam } from "./deliveryTeamLanes";

/**
 * One thing a bar has to say, and whether it is worth an alarm. Decided by whoever writes the note
 * rather than by whatever draws it, so a Feature cannot read as clean on the chart and marked in
 * the Feature table.
 */
export interface BarNote {
	text: string;
	isWarning: boolean;
	/**
	 * What the note is about, for the notes whose own words do not say. Two Teams this Portfolio
	 * cannot name produce the same sentence, so a list keyed on the sentence shows one of them and
	 * silently drops the other. Absent where the words are their own identity.
	 */
	subject?: string;
}

/** Everything one bar has to say for itself. */
export interface BarMark {
	notes: BarNote[];
	/**
	 * Names written along the bar rather than left to the symbol and its hover text.
	 *
	 * A name reachable only by hovering does not answer the question the split exists for, which is
	 * seeing which Team it is without opening anything. So while the Teams are being read, the one
	 * Team without a row of its own is named where all the others already are.
	 *
	 * Each carries an id because the names do not have to differ: every Team a Portfolio cannot name
	 * gets the same phrase, and two of them on one Feature would otherwise be one name twice - which
	 * a list keyed by name renders as a single entry, showing fewer Teams than the Feature has.
	 */
	namesOnTheBar?: { teamId: number; name: string }[];
}

export const warningInputFor = (feature: IFeature): FeatureWarningInput => ({
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
export const warningsColumnFor = (
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
 * Whether this Delivery has anything at all to warn about.
 *
 * Asked of the Features rather than of the marks, and that is the point: which marks exist depends
 * on the probability being read, and a control that came and went as the reader worked the
 * probability buttons would read as a fault in the page. A Feature's own warnings and its
 * dependencies are both properties of the Feature.
 */
export const anythingToWarnAbout = (
	features: IFeature[],
	terms: FeatureWarningTerms,
): boolean =>
	features.some(
		(feature) =>
			featureWarningSentences(warningInputFor(feature), terms).length > 0 ||
			(feature.dependsOn?.length ?? 0) > 0,
	);

/**
 * What one bar has to say for itself: everything the Feature table would warn about, then what this
 * chart alone knows - where a blocker it waits on ended up, and which of its Teams has no row.
 *
 * All of them, rather than any one. A bar showing only its dependencies would read as clean beside a
 * table row marked for a default size; one showing only the warnings would leave a reader hunting
 * for a line that was never drawn; and one saying nothing about a Team without a row would show
 * fewer Teams than the Feature has and never admit it.
 *
 * **Two of the chart's views feed this, and which one owns what is the whole point of the split.**
 * Naming a Team that got no row is part of the promise the Teams view makes - that the split never
 * shows fewer Teams than the Feature has - so it arrives with the Teams. Warnings and the account of
 * where a blocker went arrive with the warnings. Carrying the Team's name inside the warnings once
 * meant a reader looking at the Teams was told about one of them and left to guess at the other.
 */
export function barMarksFor(
	features: IFeature[],
	dependencyNotes: ReadonlyMap<number, string[]>,
	terms: FeatureWarningTerms,
	teamsWithoutALane: ReadonlyMap<number, UnlanedTeam[]>,
	showWarnings: boolean,
): Map<number, BarMark> {
	const marks = new Map<number, BarMark>();

	for (const feature of features) {
		const unlaned = teamsWithoutALane.get(feature.id) ?? [];

		const warnings: BarNote[] = showWarnings
			? [
					...featureWarningSentences(warningInputFor(feature), terms).map(
						(text) => ({ text, isWarning: true }),
					),
					// Every dependency worth warning about is already in the sentences above, said
					// in the words the table uses for it. What is left here is the chart's own
					// account of a wait there is nothing wrong with, which no warning should be
					// raised over.
					...(dependencyNotes.get(feature.id) ?? []).map((text) => ({
						text,
						isWarning: false,
					})),
				]
			: [];

		const notes: BarNote[] = [
			...warnings,
			// Tagged with the Team it is about: two Teams this Portfolio cannot name produce the
			// same sentence, and a list keyed on the sentence would show one of them only.
			...unlaned.map((team) => ({
				...team.note,
				subject: `team:${team.teamId}`,
			})),
		];

		// A bar with nothing to say stays absent rather than arriving with an empty list, which a
		// bar would draw as a symbol with nothing behind it.
		if (notes.length > 0) {
			marks.set(feature.id, {
				notes,
				// Named along the bar, not left to the hover: every other Team on this Feature is
				// written in full along a lane of its own, so the one without a lane would be the
				// only Team on the chart a reader had to go looking for.
				namesOnTheBar: unlaned.map((team) => ({
					teamId: team.teamId,
					name: team.teamName,
				})),
			});
		}
	}

	return marks;
}
