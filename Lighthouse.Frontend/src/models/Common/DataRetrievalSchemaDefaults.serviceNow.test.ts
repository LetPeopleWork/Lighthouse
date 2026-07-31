import { describe, expect, it } from "vitest";
import type { WorkTrackingSystemType } from "../WorkTracking/WorkTrackingSystemConnection";
import type { IDataRetrievalSchema } from "./DataRetrievalSchema";
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

		// Table and field discovery both measured unavailable to a least-privilege account: a
		// wizard would work for an admin and show the customer a silent empty list.
		it("offers no discovery wizard, because discovery needs rights the customer will not have", () => {
			expect(getDefaultTeamSchema("ServiceNow").wizardHint).toBeNull();
		});
	});

	// Story #5611 slice 01, AC-B4 / AC-B5 / ADR-123 decision 6. Whether a ServiceNow team is asked
	// which kinds of work are its own stops being a constant per system and becomes a question about
	// the table its connection reads. Neither the settings screen nor the create wizard changes —
	// both already gate on this flag; only what the schema says changes. Replaces the flat
	// "does not ask for a separate list of work item types" case, whose answer is now conditional.
	//
	// The schema is looked up through the connection rather than the system type, so the Work Item
	// Table option key is read in exactly one place on this side of the stack, mirroring where the
	// backend keeps it. The cast is what lets the scenario be written before that signature exists;
	// DELIVER removes it and updates the two system-type calls above along with it.
	// DISTILL scaffold for #5611 slice 01 — un-skip in DELIVER (ADR-025).
	describe.skip("when a ServiceNow team reads a table holding several kinds of work", () => {
		const schemaForConnection = getDefaultTeamSchema as unknown as (
			connection: unknown,
		) => IDataRetrievalSchema;

		const aConnectionReading = (table: string) => ({
			id: 1,
			name: "Acme ServiceNow",
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

		it("asks which kinds of work are the team's own", () => {
			const schema = schemaForConnection(aConnectionReading("task"));

			// The fallback schema also requires the field, so the ServiceNow arm has to be named
			// too, or this passes for the wrong reason.
			expect(schema.key).toBe("servicenow.query");
			expect(schema.isWorkItemTypesRequired).toBe(true);
		});

		// AC-B5. A team on a single kind of work keeps hiding the field and keeps saving without it.
		it("leaves a team reading only incidents exactly as it was", () => {
			const schema = schemaForConnection(aConnectionReading("incident"));

			expect(schema.key).toBe("servicenow.query");
			expect(schema.isWorkItemTypesRequired).toBe(false);
		});

		// A connection that never named a table reads the shipped default.
		it("treats a connection that named no table as reading one kind of work", () => {
			const schema = schemaForConnection(aConnectionReading(""));

			expect(schema.key).toBe("servicenow.query");
			expect(schema.isWorkItemTypesRequired).toBe(false);
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
