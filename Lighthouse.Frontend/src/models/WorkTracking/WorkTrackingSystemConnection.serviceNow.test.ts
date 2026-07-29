import { describe, expect, it } from "vitest";
import {
	AuthenticationMethodKeys,
	WorkTrackingSystemConnection,
} from "./WorkTrackingSystemConnection";

// Story #5574. This switch carries a default arm, so TypeScript will NOT force the ServiceNow
// case the way the exhaustive schema Records do — it would silently fall through to the generic
// "Query" label. That silence is why this test exists.
describe("How a ServiceNow connection describes itself", () => {
	it("calls a team's data retrieval field a ServiceNow query, not just a query", () => {
		const connection = new WorkTrackingSystemConnection({
			name: "Acme ServiceNow",
			workTrackingSystem: "ServiceNow",
			options: [],
		});

		expect(connection.workTrackingSystemGetDataRetrievalDisplayName()).toBe(
			"ServiceNow Query",
		);
	});

	it("authenticates with a username and password", () => {
		expect(AuthenticationMethodKeys.ServiceNowBasic).toBe("servicenow.basic");
	});
});
