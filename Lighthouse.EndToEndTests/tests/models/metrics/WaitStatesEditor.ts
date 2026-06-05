import type { Locator, Page } from "@playwright/test";

// POM scaffold for the Wait States config control (US-01) that lives in the state-config cluster next
// to State Mappings (ADR-056 Option (b) — a sibling WaitStatesEditor, NOT a relocation of StateMappingsEditor).
// DISTILL authors the POM; the WaitStatesEditor component lands in DELIVER, which un-fixmes the walking-skeleton
// spec and validates these locators against the running app. The accessor labels below are the proposed contract.
export class WaitStatesEditor {
	constructor(public readonly page: Page) {}

	get configureWaitStatesToggle(): Locator {
		return this.page.getByLabel("Configure Wait States");
	}

	get addWaitStateInput(): Locator {
		return this.page.getByRole("combobox", { name: /add wait state/i });
	}

	async enable(): Promise<void> {
		if (!(await this.configureWaitStatesToggle.isChecked())) {
			await this.configureWaitStatesToggle.check();
		}
	}

	async addWaitState(stateOrMappingName: string): Promise<void> {
		await this.addWaitStateInput.click();
		await this.addWaitStateInput.fill(stateOrMappingName);
		await this.page.getByRole("option").first().click();
	}

	get waitStateChips(): Locator {
		return this.page.getByTestId("wait-state-chip");
	}

	async countWaitStateChips(): Promise<number> {
		return this.waitStateChips.count();
	}

	get suggestionOptions(): Locator {
		return this.page.getByRole("option");
	}
}
