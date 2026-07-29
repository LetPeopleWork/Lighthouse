import { describe, expect, it } from "vitest";
import {
	getDefaultPortfolioSchema,
	getDefaultTeamSchema,
} from "./DataRetrievalSchemaDefaults";

// Story #5574, US-01 AC2 / ADR-116. The connection and settings surfaces render from these
// schemas, so this file is the ServiceNow settings screen expressed as data.
describe("What Lighthouse asks a ServiceNow shop for", () => {
	describe("when a team is pointed at ServiceNow", () => {
		it("asks for a ServiceNow query in the shop's own words", () => {
			const schema = getDefaultTeamSchema("ServiceNow");

			expect(schema.key).toBe("servicenow.query");
			expect(schema.displayLabel).toBe("ServiceNow Query (Encoded Query)");
			expect(schema.inputKind).toBe("freetext");
			expect(schema.isRequired).toBe(true);
		});

		// The configured table is the type for an ITSM-first read, so there is no separate work
		// item type list to fill in. Linear sets the precedent. Revisit in slice 02, where a
		// task-rooted read scoped by sys_class_name actually exercises the question.
		it("does not ask for a separate list of work item types", () => {
			expect(getDefaultTeamSchema("ServiceNow").isWorkItemTypesRequired).toBe(
				false,
			);
		});

		// Table and field discovery both measured unavailable to a least-privilege account: a
		// wizard would work for an admin and show the customer a silent empty list.
		it("offers no discovery wizard, because discovery needs rights the customer will not have", () => {
			expect(getDefaultTeamSchema("ServiceNow").wizardHint).toBeNull();
		});
	});

	describe("when someone tries to build a portfolio over ServiceNow", () => {
		// The limitation lives in the configuration surface rather than only in the docs, so
		// there is no half-working portfolio path to stumble into.
		it("declines rather than offering a field that leads nowhere", () => {
			const schema = getDefaultPortfolioSchema("ServiceNow");

			expect(schema.displayLabel).toBe("Not supported for ServiceNow");
			expect(schema.inputKind).toBe("none");
			expect(schema.isRequired).toBe(false);
			expect(schema.wizardHint).toBeNull();
		});
	});
});
