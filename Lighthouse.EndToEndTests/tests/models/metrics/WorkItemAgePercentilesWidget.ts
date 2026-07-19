import type { Page } from "@playwright/test";
import { WorkItemAgePercentilesCard } from "./MetricsPage";
import { RagChip } from "./RagChip";

const WIDGET_KEY = "workItemAgePercentiles";

/**
 * WHY-NEW-FILE: tests/models/metrics/WorkItemAgePercentilesWidget.ts
 *   CLOSEST-EXISTING: tests/models/metrics/MetricsPage.ts (WorkItemAgePercentilesCard)
 *   EXTENSION-COST: the RAG readers belong on WorkItemAgePercentilesCard itself,
 *     but MetricsPage.ts is covered by a read/edit deny rule in the environment this
 *     step ran in and could not be modified at all.
 *   PARALLEL-RATIONALE: tooling constraint, not a design boundary — this subclass
 *     inherits the card verbatim and only adds the chip, so folding it back into
 *     WorkItemAgePercentilesCard when that file is writable is a pure move.
 *
 * Adds the slice-04 status chip to the existing percentiles card. The trend
 * chrome is deliberately NOT re-modelled here: MetricsWidget already reads it
 * (including the glyph-to-direction map MUI forces on us), so specs take the
 * trend from the MetricsWidget instance for this same widget key.
 */
export class WorkItemAgePercentilesWidget extends WorkItemAgePercentilesCard {
	readonly rag: RagChip;

	constructor(page: Page) {
		super(page);
		this.rag = RagChip.forWidget(page, WIDGET_KEY);
	}
}
