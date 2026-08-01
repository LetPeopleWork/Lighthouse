import { describe, expect, it } from "vitest";
import type { WorkTrackingSystemType } from "../WorkTracking/WorkTrackingSystemConnection";
import {
	getDefaultPortfolioSchema,
	getDefaultTeamSchema,
} from "./DataRetrievalSchemaDefaults";

// The example DD-5 names: non-reference fields only, so it cannot select nothing (#5610).
const WORKED_EXAMPLE = "active=true^priority=1";

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

	// Story #5610 slice 01, AC-A1 / AC-A3. The first real user of the connector stopped at
	// an empty four-line box with nothing in the product saying what an encoded query is. The
	// guidance is carried by the schema so one shared field renders it for every connector.
	describe("when a flow coach is staring at the blank query field", () => {
		it("shows a worked example of the query it wants", () => {
			const schema = getDefaultTeamSchema(aServiceNowConnection());

			expect(schema.placeholder).toBe(WORKED_EXAMPLE);
		});

		// One instruction, not a catalogue of failure modes (AC-A4 withdrawn 2026-08-01). Pinned
		// against the literal: the backend twin has to say this byte for byte.
		it("points at where ServiceNow itself will hand you a good query", () => {
			expect(getDefaultTeamSchema(aServiceNowConnection()).helpText).toBe(
				"To get an encoded query, filter a list in ServiceNow, right-click the filter breadcrumb, and choose Copy query",
			);
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
			const schema = getDefaultPortfolioSchema(aServiceNowConnection());

			expect(schema.placeholder ?? null).toBeNull();
			expect(schema.helpText ?? null).toBeNull();
		});
	});
});
