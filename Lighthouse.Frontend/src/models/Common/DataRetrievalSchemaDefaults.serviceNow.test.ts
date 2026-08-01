import { describe, expect, it } from "vitest";
import type { WorkTrackingSystemType } from "../WorkTracking/WorkTrackingSystemConnection";
import type { IDataRetrievalSchema } from "./DataRetrievalSchema";
import {
	getDefaultPortfolioSchema,
	getDefaultTeamSchema,
} from "./DataRetrievalSchemaDefaults";

// The example DD-5 names: a real encoded query in column form, narrow enough to be one team's.
const WORKED_EXAMPLE = "active=true^assignment_group=Service Desk";

// The two fields the schema gains in slice 01. Read through a cast because the interface does not
// declare them yet; DELIVER deletes the cast in the commit that adds them.
type QueryGuidance = { placeholder: string | null; helpText: string | null };

const guidanceOn = (schema: IDataRetrievalSchema) =>
	schema as unknown as QueryGuidance;

// No options: the schema factories are a lookup by system type and read nothing else off the
// connection (ADR-123 decision 6 as amended 2026-07-31). A ServiceNow connection now carries no
// table option at all — every read is rooted at `task` (ADR-116 decision 1, withdrawn 2026-07-31).
const aServiceNowConnection = () => ({
	workTrackingSystem: "ServiceNow" as WorkTrackingSystemType,
});

// Story #5574, US-01 AC2 / ADR-116. The connection and settings surfaces render from these
// schemas, so this file is the ServiceNow settings screen expressed as data.
describe("What Lighthouse asks a ServiceNow shop for", () => {
	describe("when a team is pointed at ServiceNow", () => {
		it("asks for a ServiceNow query in the shop's own words", () => {
			const schema = getDefaultTeamSchema(aServiceNowConnection());

			expect(schema.key).toBe("servicenow.query");
			expect(schema.displayLabel).toBe("ServiceNow Query (Encoded Query)");
			expect(schema.inputKind).toBe("freetext");
			expect(schema.isRequired).toBe(true);
		});

		// Table and field discovery both measured unavailable to a least-privilege account: a
		// wizard would work for an admin and show the customer a silent empty list.
		it("offers no discovery wizard, because discovery needs rights the customer will not have", () => {
			expect(
				getDefaultTeamSchema(aServiceNowConnection()).wizardHint,
			).toBeNull();
		});
	});

	// Story #5611, AC-B4 / AC-B5 / ADR-123 decision 6 as amended 2026-07-31. Every ServiceNow team
	// says which kinds of work are its own, whatever table its connection reads. The conditional this
	// replaces hid a field the read still honoured, and it protected a configuration nothing was ever
	// shipped on. Neither the settings screen nor the create wizard changes — both already gate on
	// this flag; only what the schema says changes.
	// The table is not parametrised over: it is not an input to the factory at all, so a case per
	// table would run one case three times while reading as a table-independence claim.
	describe("when a ServiceNow team is asked what work is its own", () => {
		it("asks which kinds of work are the team's own", () => {
			const schema = getDefaultTeamSchema(aServiceNowConnection());

			// The fallback schema also requires the field, so the ServiceNow arm has to be named
			// too, or this passes for the wrong reason.
			expect(schema.key).toBe("servicenow.query");
			expect(schema.isWorkItemTypesRequired).toBe(true);
		});
	});

	// Story #5610 slice 01, AC-A1 / AC-A3 / AC-A4. The first real user of the connector stopped at
	// an empty four-line box with nothing in the product saying what an encoded query is. The
	// guidance is carried by the schema so one shared field renders it for every connector.
	// DISTILL scaffold for #5610 slice 01 - un-skip in DELIVER (ADR-025).
	describe.skip("when a flow coach is staring at the blank query field", () => {
		it("shows a worked example of the query it wants", () => {
			const guidance = guidanceOn(
				getDefaultTeamSchema(aServiceNowConnection()),
			);

			expect(guidance.placeholder).toBe(WORKED_EXAMPLE);
		});

		// An unknown field name is dropped and the query widens to the whole table; a bad value on a
		// real field matches nothing. Both were measured, and this is the last surface before either
		// one costs someone their afternoon.
		it("names both ways a query fails quietly, and where ServiceNow will hand you a good one", () => {
			const help = guidanceOn(
				getDefaultTeamSchema(aServiceNowConnection()),
			).helpText;

			expect(help).toBeTruthy();
			expect(help?.toLowerCase()).toContain("whole table");
			expect(help?.toLowerCase()).toContain("nothing");
			expect(help).toContain("Copy query");
		});
	});

	describe("when someone tries to build a portfolio over ServiceNow", () => {
		// The limitation lives in the configuration surface rather than only in the docs, so
		// there is no half-working portfolio path to stumble into.
		it("declines rather than offering a field that leads nowhere", () => {
			const schema = getDefaultPortfolioSchema(aServiceNowConnection());

			expect(schema.displayLabel).toBe("Not supported for ServiceNow");
			expect(schema.inputKind).toBe("none");
			expect(schema.isRequired).toBe(false);
			expect(schema.wizardHint).toBeNull();
		});

		// AC-A6. Guidance for a field that is never rendered would be help nobody can reach.
		it("offers no query guidance for a field it never renders", () => {
			const guidance = guidanceOn(
				getDefaultPortfolioSchema(aServiceNowConnection()),
			);

			expect(guidance.placeholder ?? null).toBeNull();
			expect(guidance.helpText ?? null).toBeNull();
		});
	});
});
