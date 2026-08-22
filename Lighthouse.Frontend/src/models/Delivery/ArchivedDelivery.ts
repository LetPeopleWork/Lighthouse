import { z } from "zod";
import {
	DeliverySelectionMode,
	type IWorkItemRuleCondition,
	ruleConditionSchema,
} from "../WorkItemRules";
import type {
	FeatureMetric,
	WhenDistributionPoint,
} from "./DeliveryMetricsHistory";

/**
 * A Feature as the Delivery had it on its last day. There is no id and no link on it, and there is
 * not meant to be: the Feature may have been renamed, moved to another Team or deleted since, and a
 * row that could reach it would show today's answer under a heading promising the closing day's.
 */
const ArchivedFeatureRowSchema = z.object({
	referenceId: z.string(),
	name: z.string(),
	completion: z.number(),
	likelihood: z
		.number()
		.nullish()
		.transform((value) => value ?? null),
	totalItems: z
		.number()
		.nullish()
		.transform((value) => value ?? null),
	isUsingDefaultSize: z
		.boolean()
		.nullish()
		.transform((value) => value ?? null),
});

export const ArchivedDeliverySchema = z.object({
	id: z.number(),
	name: z.string(),
	date: z.string(),
	portfolioId: z.number(),
	archivedOn: z.string(),
	progress: z.number(),
	totalWork: z.number(),
	doneWork: z.number(),
	remainingWork: z.number(),
	likelihoodPercentage: z.number().nullable(),
	hasSufficientData: z.boolean(),
	teamsWithoutForecast: z.array(z.string()),
	selectionMode: z.union([z.string(), z.number()]),
	concurrencyToken: z.string(),
	featureBreakdown: z.array(ArchivedFeatureRowSchema),
	whenDistribution: z.array(
		z.object({ probability: z.number(), expectedDate: z.coerce.date() }),
	),
	rules: z.array(ruleConditionSchema).default([]),
	mode: z.string().default("and"),
	metricSnapshotCount: z.number(),
});

export type IArchivedDelivery = z.infer<typeof ArchivedDeliverySchema>;

/**
 * A Delivery that has been retired, as it was written down on the day it closed.
 *
 * This is a type of its own rather than a Delivery with some fields left empty, and that is the
 * whole point: every value here was worked out once, at closing time, and is never worked out
 * again. Its Feature rows travel with it in full rather than as ids to look up, so there is nothing
 * on it to fetch a live Feature by even if somebody wanted to.
 */
export class ArchivedDelivery {
	readonly id: number;
	readonly name: string;
	readonly date: string;
	readonly portfolioId: number;
	readonly archivedOn: string;
	readonly progress: number;
	readonly totalWork: number;
	readonly doneWork: number;
	readonly remainingWork: number;
	readonly likelihoodPercentage: number | null;
	readonly hasSufficientData: boolean;
	readonly teamsWithoutForecast: string[];
	readonly selectionMode: string | number;
	readonly concurrencyToken: string;
	readonly featureBreakdown: FeatureMetric[];
	readonly whenDistribution: WhenDistributionPoint[];
	readonly rules: IWorkItemRuleCondition[];
	readonly mode: "and" | "or";
	readonly metricSnapshotCount: number;

	private constructor(data: IArchivedDelivery) {
		this.id = data.id;
		this.name = data.name;
		this.date = data.date;
		this.portfolioId = data.portfolioId;
		this.archivedOn = data.archivedOn;
		this.progress = data.progress;
		this.totalWork = data.totalWork;
		this.doneWork = data.doneWork;
		this.remainingWork = data.remainingWork;
		this.likelihoodPercentage = data.likelihoodPercentage;
		this.hasSufficientData = data.hasSufficientData;
		this.teamsWithoutForecast = data.teamsWithoutForecast;
		this.selectionMode = data.selectionMode;
		this.concurrencyToken = data.concurrencyToken;
		this.featureBreakdown = data.featureBreakdown;
		this.whenDistribution = data.whenDistribution;
		this.rules = data.rules;
		this.mode = data.mode.toLowerCase() === "or" ? "or" : "and";
		this.metricSnapshotCount = data.metricSnapshotCount;
	}

	static fromParsed(data: IArchivedDelivery): ArchivedDelivery {
		return new ArchivedDelivery(data);
	}

	get isRuleBased(): boolean {
		return (
			this.selectionMode === DeliverySelectionMode.RuleBased ||
			this.selectionMode === "RuleBased"
		);
	}

	getFormattedDate(): string {
		return ArchivedDelivery.formatUtcDay(this.date);
	}

	getFormattedArchivedOn(): string {
		return ArchivedDelivery.formatUtcDay(this.archivedOn);
	}

	// Both days are read in UTC, so the day a Delivery closed reads the same to everyone looking
	// at the same record from a different offset.
	private static formatUtcDay(value: string): string {
		return new Date(value).toLocaleDateString(undefined, { timeZone: "UTC" });
	}
}
