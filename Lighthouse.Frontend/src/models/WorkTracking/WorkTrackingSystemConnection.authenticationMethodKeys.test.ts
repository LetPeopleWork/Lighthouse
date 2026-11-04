import { describe, expect, it } from "vitest";
import { AuthenticationMethodKeys } from "./WorkTrackingSystemConnection";

describe("the sign-in methods the frontend knows by name", () => {
	it("includes every Jira sign-in, so the picker rule can name them", () => {
		expect(AuthenticationMethodKeys).toEqual({
			AzureDevOpsPat: "ado.pat",
			JiraCloud: "jira.cloud",
			JiraDataCenter: "jira.datacenter",
			JiraScopedToken: "jira.scopedtoken",
			JiraOAuth: "jira.oauth",
			LinearApiKey: "linear.apikey",
			ServiceNowBasic: "servicenow.basic",
			None: "none",
		});
	});
});
