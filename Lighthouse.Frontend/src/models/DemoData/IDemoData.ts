export interface IDemoDataScenario {
	id: string;
	title: string;
	description: string;
	isPremium: boolean;
}

export interface IDemoDataService {
	getAvailableScenarios(): Promise<IDemoDataScenario[]>;
	loadScenario(scenarioId: string): Promise<void>;
}
