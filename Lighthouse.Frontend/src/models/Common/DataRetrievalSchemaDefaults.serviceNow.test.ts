import { describe, expect, it } from "vitest";
import type { WorkTrackingSystemType } from "../WorkTracking/WorkTrackingSystemConnection";
import {
	getDefaultPortfolioSchema,
	getDefaultTeamSchema,
} from "./DataRetrievalSchemaDefaults";

const aServiceNowConnectionReading = (table: string) => ({
	workTrackingSystem: "ServiceNow" as WorkTrackingSystemType,
	options: [
		{
			key: "Work Item Table",
			value: table,
			isSecret: false,
			isOptional: true,
		},
	],
});

// Story #5574, US-01 AC2 / ADR-116. The connection and settings surfaces render from these
// schemas, so this file is the ServiceNow settings screen expressed as data.
describe("What Lighthouse asks a ServiceNow shop for", () => {
	describe("when a team is pointed at ServiceNow", () => {
		it("asks for a ServiceNow query in the shop's own words", () => {
			const schema = getDefaultTeamSchema(
				aServiceNowConnectionReading("incident"),
			);

			expect(schema.key).toBe("servicenow.query");
			expect(schema.displayLabel).toBe("ServiceNow Query (Encoded Query)");
			expect(schema.inputKind).toBe("freetext");
			expect(schema.isRequired).toBe(true);
		});

		// Table and field discovery both measured unavailable to a least-privilege account: a
		// wizard would work for an admin and show the customer a silent empty list.
		it("offers no discovery wizard, because discovery needs rights the customer will not have", () => {
			expect(
				getDefaultTeamSchema(aServiceNowConnectionReading("incident"))
					.wizardHint,
			).toBeNull();
		});
	});

	// Story #5611, AC-B4 / AC-B5 / ADR-123 decision 6 as amended 2026-07-31. Every ServiceNow team
	// says which kinds of work are its own, whatever table its connection reads. The conditional this
	// replaces hid a field the read still honoured, and it protected a configuration nothing was ever
	// shipped on. Neither the settings screen nor the create wizard changes — both already gate on
	// this flag; only what the schema says changes.
	describe("when a ServiceNow team is asked what work is its own", () => {
		it.each(["task", "incident", ""])(
			"asks which kinds of work are the team's own, reading '%s'",
			(table) => {
				const schema = getDefaultTeamSchema(
					aServiceNowConnectionReading(table),
				);

				// The fallback schema also requires the field, so the ServiceNow arm has to be named
				// too, or this passes for the wrong reason.
				expect(schema.key).toBe("servicenow.query");
				expect(schema.isWorkItemTypesRequired).toBe(true);
			},
		);
	});

	describe("when someone tries to build a portfolio over ServiceNow", () => {
		// The limitation lives in the configuration surface rather than only in the docs, so
		// there is no half-working portfolio path to stumble into.
		it("declines rather than offering a field that leads nowhere", () => {
			const schema = getDefaultPortfolioSchema(
				aServiceNowConnectionReading("incident"),
			);

			expect(schema.displayLabel).toBe("Not supported for ServiceNow");
			expect(schema.inputKind).toBe("none");
			expect(schema.isRequired).toBe(false);
			expect(schema.wizardHint).toBeNull();
		});
	});
});
