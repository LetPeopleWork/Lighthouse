import { z } from "zod";

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
});

export type IArchivedDelivery = z.infer<typeof ArchivedDeliverySchema>;

/**
 * A Delivery that has been retired, as it was written down on the day it closed.
 *
 * This is a type of its own rather than a Delivery with some fields left empty, and that is the
 * whole point: it carries no Features and no forecast, so nothing reading one can reach for a
 * number that only a Delivery still in flight can have. Every value here was worked out once, at
 * closing time, and is never worked out again.
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
	}

	static fromParsed(data: IArchivedDelivery): ArchivedDelivery {
		return new ArchivedDelivery(data);
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
