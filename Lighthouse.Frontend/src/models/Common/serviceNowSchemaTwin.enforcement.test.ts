import { existsSync, readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";
import { describe, expect, it } from "vitest";

// Story #5611 slice 01, AC-B4 / ADR-123 decision 7. Two facts about ServiceNow live on both sides of
// the stack: which tables have descendants, and what the Work Item Table option is called. Bug #5613
// is what a disagreement costs — a team the create wizard accepts and the settings page will not
// save. Collapsing the twins was ruled a design change rather than a fix, so the answer is to make
// the drift loud. Both assertions compare sets, so drift in either direction fails.
//
// Source text on both sides rather than an import on one, following
// formatLikelihood.enforcement.test.ts: the point is that the two declarations agree, and reading
// one of them through the module system would let a rename on that side pass unnoticed.
const here = dirname(fileURLToPath(import.meta.url));

const repositoryRoot = resolve(here, "../../../..");

const twins = {
	backendHierarchy: resolve(
		repositoryRoot,
		"Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/WorkTrackingConnectors/ServiceNow/ServiceNowTableHierarchy.cs",
	),
	backendOptionNames: resolve(
		repositoryRoot,
		"Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/WorkTrackingConnectors/ServiceNow/ServiceNowWorkTrackingOptionNames.cs",
	),
	frontendSchemaDefaults: resolve(here, "DataRetrievalSchemaDefaults.ts"),
} as const;

function sourceOf(path: string): string {
	expect(existsSync(path), `The twin at ${path} does not exist`).toBe(true);

	return readFileSync(path, "utf8");
}

function quotedValuesIn(source: string, declaration: RegExp): string[] {
	const match = declaration.exec(source);

	expect(match, `${declaration.source} not found`).not.toBe(null);

	return [...(match?.[1] ?? "").matchAll(/"([^"]+)"/g)].map(
		(quoted) => quoted[1],
	);
}

// DISTILL scaffold for #5611 slice 01 — un-skip in DELIVER (ADR-025).
describe.skip("what Lighthouse knows about ServiceNow agrees across the stacks", () => {
	it("names the same tables as holding several kinds of work", () => {
		const backend = quotedValuesIn(
			sourceOf(twins.backendHierarchy),
			/RootTables[^=]*=\s*\[([^\]]*)\]/,
		);
		const frontend = quotedValuesIn(
			sourceOf(twins.frontendSchemaDefaults),
			/serviceNowHierarchyRootTables[^=]*=\s*\[([^\]]*)\]/,
		);

		expect(new Set(frontend)).toEqual(new Set(backend));
		expect(backend).toContain("task");
	});

	it("calls the work item table setting the same thing on both sides", () => {
		const backend = quotedValuesIn(
			sourceOf(twins.backendOptionNames),
			/WorkItemTable\s*=\s*("[^"]+")/,
		);
		const frontend = quotedValuesIn(
			sourceOf(twins.frontendSchemaDefaults),
			/serviceNowWorkItemTableOptionKey[^=]*=\s*("[^"]+")/,
		);

		expect(frontend).toEqual(backend);
	});
});
