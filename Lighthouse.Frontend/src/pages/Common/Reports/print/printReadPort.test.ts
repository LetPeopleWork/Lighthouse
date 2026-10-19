import { describe, expectTypeOf, it } from "vitest";
import type { PrintReadPort } from "./printReadPort";

/**
 * The print page reads; it never writes. Its port to the server holds the report read and the owner's name and
 * nothing else, so creating, editing or deleting anything is out of its reach by type. The check runs when the
 * code is compiled: a port that gains any other member fails the build.
 */
describe("what the print page may ask the server", () => {
	// @us-01 @slice-01 @error @contract-shape:unbounded-preservation
	it("is the report read and the owner's name, and nothing that changes anything", () => {
		expectTypeOf<keyof PrintReadPort>().toEqualTypeOf<
			"getReport" | "getTeam" | "getPortfolio"
		>();
	});
});
