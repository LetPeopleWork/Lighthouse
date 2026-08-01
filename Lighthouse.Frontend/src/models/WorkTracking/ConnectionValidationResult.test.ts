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

	it("reads a refusal out of the full result object", () => {
		expect(readConnectionValidation({ isValid: false }).isValid).toBe(false);
	});

	// US 5612 removed the advisory channel: the only advisory any connector ever returned was
	// withdrawn as unactionable at connection scope, so a field nothing writes was deleted rather
	// than kept for a caller that might one day appear. A payload that still carries the old keys
	// must be read as a plain verdict rather than tripping over them.
	it("ignores keys the backend no longer sends", () => {
		// A variable rather than an inline literal, so excess-property checking does not reject the
		// very shape this test exists to feed in.
		const payloadFromAnOlderBackend = {
			isValid: true,
			advisory: "Something a previous version would have said.",
			advisoryCode: "history_requires_itil",
		};

		expect(readConnectionValidation(payloadFromAnOlderBackend)).toEqual({
			isValid: true,
		});
	});

	it("treats a missing answer as not valid rather than as valid", () => {
		expect(readConnectionValidation(null).isValid).toBe(false);
		expect(readConnectionValidation(undefined).isValid).toBe(false);
	});
});
