import { z } from "zod";

export interface IRefreshSettings {
	interval: number;
	refreshAfter: number;
	startDelay: number;
}

export const RefreshSettingsSchema = z.object({
	interval: z.number(),
	refreshAfter: z.number(),
	startDelay: z.number(),
});

export class RefreshSettings implements IRefreshSettings {
	interval: number;
	refreshAfter: number;
	startDelay: number;

	constructor(interval: number, refreshAfter: number, startDelay: number) {
		this.interval = interval;
		this.refreshAfter = refreshAfter;
		this.startDelay = startDelay;
	}
}
