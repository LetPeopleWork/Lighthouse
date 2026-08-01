import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";
import { describe, expect, it } from "vitest";

const here = dirname(fileURLToPath(import.meta.url));

// Story #5610 slice 01, AC-A3 / D4. The data-retrieval schema is duplicated knowledge that has
// already drifted once and shipped unsaveable ServiceNow teams (Bug #5613). The guidance is one
// more thing both sides have to agree on, and the twins disagreeing is invisible to either stack's
// own tests — so this reads both as source text rather than importing either.
const twins = {
	backend: resolve(
		here,
		"../../../../Lighthouse.Backend/Lighthouse.Backend/API/DTO/DataRetrievalSchemaDto.cs",
	),
	frontend: resolve(here, "./DataRetrievalSchemaDefaults.ts"),
} as const;

const WORKED_EXAMPLE = "active=true^assignment_group=Service Desk";

describe("the ServiceNow query guidance says the same thing on both stacks", () => {
	it.each(Object.entries(twins))(
		"%s shows the same worked example",
		(_stack, path) => {
			expect(readFileSync(path, "utf8")).toContain(WORKED_EXAMPLE);
		},
	);

	// The example alone is not the guidance: the help text is what names the two silent failures and
	// the path ServiceNow itself offers. A stack that carries one and not the other renders half a
	// sentence.
	it.each(Object.entries(twins))(
		"%s carries the help text beside it",
		(_stack, path) => {
			const source = readFileSync(path, "utf8");

			expect(source.toLowerCase()).toContain("whole table");
			expect(source).toContain("Copy query");
		},
	);
});
