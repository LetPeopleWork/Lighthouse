import { renderHook } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { ITeamSettings } from "../models/Team/TeamSettings";
import { UsageDataEventName } from "../services/Api/UsageDataService";
import { createMockTeamSettings } from "../tests/TestDataProvider";
import { useRefinementSetUpReporter } from "./useRefinementSetUpReporter";

const { reporter } = vi.hoisted(() => ({
	reporter: { current: vi.fn() },
}));

vi.mock("../services/UsageData/usageDataReporter", () => ({
	useUsageDataReporter: () => reporter.current,
}));

const withoutRefinement: ITeamSettings = {
	...createMockTeamSettings(),
	refinement: null,
};

const withRefinement: ITeamSettings = {
	...createMockTeamSettings(),
	refinement: { states: [{ state: "Backlog" }] },
};

describe("useRefinementSetUpReporter", () => {
	beforeEach(() => {
		reporter.current = vi.fn();
	});

	// The reporter is handed out anew once the browser's usage-data answer arrives, which can be after the
	// settings page opened. The report must go through the reporter that holds that answer.
	it("reports through the reporter the page holds when the save is answered, not the one it opened with", () => {
		const reporterBeforeConsentWasKnown = reporter.current;
		const { result, rerender } = renderHook(() => useRefinementSetUpReporter());
		result.current.settingsLoaded(withoutRefinement);

		const reporterOnceConsentIsKnown = vi.fn();
		reporter.current = reporterOnceConsentIsKnown;
		rerender();
		result.current.settingsSaved(withRefinement);

		expect(reporterOnceConsentIsKnown).toHaveBeenCalledWith({
			name: UsageDataEventName.TeamRefinementConfigured,
		});
		expect(reporterBeforeConsentWasKnown).not.toHaveBeenCalled();
	});
});
