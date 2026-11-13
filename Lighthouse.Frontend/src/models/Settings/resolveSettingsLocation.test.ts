import { describe, expect, it } from "vitest";
import { resolveSettingsLocation } from "./resolveSettingsLocation";
import type { SettingsAccess } from "./settingsAreas";

const systemAdmin: SettingsAccess = {
	isLoading: false,
	isSystemAdmin: true,
	isRbacEnabled: true,
};
const member: SettingsAccess = {
	isLoading: false,
	isSystemAdmin: false,
	isRbacEnabled: true,
};

describe("resolveSettingsLocation", () => {
	it("waits while access is still loading", () => {
		expect(
			resolveSettingsLocation(
				{ area: "organization", tab: "calendar" },
				{ ...systemAdmin, isLoading: true },
			),
		).toEqual({ kind: "wait" });
	});

	it("shows a tab the person may open", () => {
		expect(
			resolveSettingsLocation(
				{ area: "organization", tab: "calendar" },
				systemAdmin,
			),
		).toEqual({ kind: "show", area: "organization", tab: "calendar" });
	});

	it("moves an earlier slug on to the tab's current address", () => {
		expect(
			resolveSettingsLocation(
				{ area: "organization", tab: "configuration" },
				systemAdmin,
			),
		).toEqual({ kind: "redirect", path: "/settings/organization/terminology" });
	});

	it("moves an old ?tab= key on to its tab, whatever its case", () => {
		expect(
			resolveSettingsLocation({ legacyKey: "RBAC" }, systemAdmin),
		).toEqual({ kind: "redirect", path: "/settings/access/system-wide" });
	});

	it("moves an old ?tab= key the person may not open to the nearest tab of the same area", () => {
		expect(resolveSettingsLocation({ legacyKey: "rbac" }, member)).toEqual({
			kind: "redirect",
			path: "/settings/access/api-keys",
		});
	});

	it("lands in another area when the area asked for has no tab the person may open", () => {
		expect(
			resolveSettingsLocation(
				{ area: "organization", tab: "calendar" },
				member,
			),
		).toEqual({ kind: "redirect", path: "/settings/access/api-keys" });
	});

	it("lands on the first tab the person may open when the area is unknown", () => {
		expect(resolveSettingsLocation({ area: "nowhere" }, member)).toEqual({
			kind: "redirect",
			path: "/settings/access/api-keys",
		});
	});
});
