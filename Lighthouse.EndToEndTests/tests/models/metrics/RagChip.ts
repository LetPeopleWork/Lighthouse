import type { Locator, Page } from "@playwright/test";

export type RagStatus = "red" | "amber" | "green";

/**
 * What a POM actually observed in the DOM. Deliberately wider than RagStatus:
 * an absent or unrecognised attribute must NOT collapse into a legitimate
 * status, otherwise locator drift would read as a product regression — or,
 * worse, pass vacuously.
 */
export type ObservedRagStatus = RagStatus | "unknown";

const RAG_STATUS_LABELS: Record<RagStatus, string> = {
	red: "Act",
	amber: "Observe",
	green: "Sustain",
};

export function toRagStatus(raw: string | null): ObservedRagStatus {
	if (raw === "red" || raw === "amber" || raw === "green") {
		return raw;
	}

	return "unknown";
}

export function ragLabelFor(status: RagStatus): string {
	return RAG_STATUS_LABELS[status];
}

/**
 * The RAG chip the shared WidgetShell header paints for a widget. Every Flow
 * Overview widget renders the same chrome, so the reading logic lives here once
 * and each widget's POM composes it with its own widget key.
 */
export class RagChip {
	constructor(
		private readonly widget: Locator,
		private readonly widgetKey: string,
	) {}

	static forWidget(page: Page, widgetKey: string): RagChip {
		return new RagChip(
			page.locator(`[data-testid="dashboard-item-${widgetKey}"]`),
			widgetKey,
		);
	}

	get chip(): Locator {
		return this.widget.getByTestId(`widget-rag-${this.widgetKey}`);
	}

	/**
	 * `rag-status` is a shared test id across every widget, so it is scoped
	 * inside this widget's chip to avoid a strict-mode violation.
	 */
	get statusValue(): Locator {
		return this.chip.getByTestId("rag-status");
	}

	async readStatus(): Promise<ObservedRagStatus> {
		return toRagStatus(await this.statusValue.getAttribute("data-rag"));
	}

	async readLabel(): Promise<string> {
		return (await this.statusValue.innerText()).trim();
	}

	/**
	 * The chip's accessible name, which MUI derives from the RAG tip text.
	 * Read from the chip itself rather than a hovered popper: a bare
	 * `getByRole("tooltip")` also matches the widget's info popover and trips
	 * strict mode intermittently.
	 */
	async readTipText(): Promise<string> {
		return (await this.chip.getAttribute("aria-label")) ?? "";
	}
}
