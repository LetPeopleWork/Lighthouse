export interface IRefreshSettings {
	interval: number;
	refreshAfter: number;
	startDelay: number;
}

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
