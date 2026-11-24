import type { INamedCycleTimeDefinition } from "../../../models/Metrics/NamedCycleTime";
import {
	type IWorkItem,
	namedCycleTimeValueOf,
} from "../../../models/WorkItem";

/** A Work Item as one Cycle Time plots it: its cycle time is that measure's days, on the date it sits on. */
export type PlottedWorkItem = IWorkItem & { readonly plottedDate: Date };

/** The words a reader has chosen for the things the scatterplot names. */
export interface CycleTimeTerms {
	readonly workItems: string;
	readonly cycleTime: string;
}

/** One Cycle Time the scatterplot can show: where each dot sits, and how its date is named. */
export interface CycleTimeMeasure {
	/** The definition measured, or null for the default Cycle Time. */
	readonly definitionId: number | null;
	readonly dateAxisLabel: string;
	plot(population: readonly IWorkItem[]): PlottedWorkItem[];
	dotListTitle(terms: CycleTimeTerms, dot: PlottedWorkItem): string;
	groupLabel(count: number, workItemsTerm: string): string;
}

/** The default Cycle Time: each closed Work Item on the day it closed. */
export class DefaultCycleTimeMeasure implements CycleTimeMeasure {
	readonly definitionId = null;
	readonly dateAxisLabel = "Date";

	plot(population: readonly IWorkItem[]): PlottedWorkItem[] {
		return population.map((item) => ({
			...item,
			plottedDate: item.closedDate,
		}));
	}

	dotListTitle(terms: CycleTimeTerms, dot: PlottedWorkItem): string {
		return `${terms.workItems} closed on ${dot.plottedDate.toLocaleDateString()} with ${terms.cycleTime} of ${dot.cycleTime} days`;
	}

	groupLabel(count: number, workItemsTerm: string): string {
		return `${count} Closed ${workItemsTerm}`;
	}
}

/**
 * A named Cycle Time: each Work Item that reached the definition's end state, on the day it got
 * there, whether or not it has closed since. One without a value for the definition is not plotted.
 */
export class NamedCycleTimeMeasure implements CycleTimeMeasure {
	constructor(private readonly definition: INamedCycleTimeDefinition) {}

	get definitionId(): number {
		return this.definition.id;
	}

	get dateAxisLabel(): string {
		return `Date reached ${this.definition.endState}`;
	}

	plot(population: readonly IWorkItem[]): PlottedWorkItem[] {
		return population.flatMap((item) => {
			const value = namedCycleTimeValueOf(item, this.definition.id);
			return value
				? [{ ...item, cycleTime: value.days, plottedDate: value.endDate }]
				: [];
		});
	}

	dotListTitle(terms: CycleTimeTerms, dot: PlottedWorkItem): string {
		return `${this.reachedEndState(terms.workItems)} on ${dot.plottedDate.toLocaleDateString()} with ${terms.cycleTime} of ${dot.cycleTime} days`;
	}

	groupLabel(count: number, workItemsTerm: string): string {
		return `${count} ${this.reachedEndState(workItemsTerm)}`;
	}

	private reachedEndState(workItemsTerm: string): string {
		return `${workItemsTerm} that reached ${this.definition.endState}`;
	}
}
