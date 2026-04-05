import type { Page } from "@playwright/test";

export abstract class BaseWizard<T> {
	constructor(
		public readonly page: Page,
		protected readonly createPageHandler: (page: Page) => T,
	) {}

	abstract selectByName(name: string): Promise<void>;
	abstract confirm(): Promise<T>;
}
