import { describe, expect, it } from "vitest";
import { readConnectionValidation } from "./ConnectionValidationResult";

// Story #5577, ADR-118 decision 5.
//
// The validation response has carried only a verdict. Slice 04 needs it to carry a working
// connection that is nonetheless missing a capability — and to keep working unchanged for every
// connector that has nothing extra to say.
describe("readConnectionValidation", () => {
	it("reads a bare boolean, which is what the older connectors answer", () => {
		expect(readConnectionValidation(true).isValid).toBe(true);
		expect(readConnectionValidation(false).isValid).toBe(false);
	});

	it("reads the verdict out of the full result object", () => {
		expect(readConnectionValidation({ isValid: true }).isValid).toBe(true);
	});

	it("says nothing when there is nothing to say", () => {
		const result = readConnectionValidation({ isValid: true });

		// A connector with no capability gap must not put an empty banner in front of an
		// administrator who has nothing to act on.
		expect(result.advisory).toBeUndefined();
		expect(result.advisoryCode).toBeUndefined();
	});

	it("carries an advisory that arrived alongside a valid connection", () => {
		const result = readConnectionValidation({
			isValid: true,
			advisory:
				"Cycle time measures request-to-resolution. Grant the integration account the itil role for time in progress.",
			advisoryCode: "history_requires_itil",
		});

		expect(result.isValid).toBe(true);
		expect(result.advisoryCode).toBe("history_requires_itil");
		expect(result.advisory).toContain("itil");
	});

	// The advisory is not an error. Treating it as one would block a setup that works perfectly
	// well for throughput and forecasting, which is most of what the connector is for.
	it("an advisory does not make the connection invalid", () => {
		const result = readConnectionValidation({
			isValid: true,
			advisory: "Something worth knowing.",
			advisoryCode: "history_requires_state_metric",
		});

		expect(result.isValid).toBe(true);
	});

	it("treats a missing answer as not valid rather than as valid", () => {
		expect(readConnectionValidation(null).isValid).toBe(false);
		expect(readConnectionValidation(undefined).isValid).toBe(false);
	});
});
