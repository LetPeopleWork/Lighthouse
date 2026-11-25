import { fireEvent, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import type { IWorkTrackingSystemConnection } from "../../../models/WorkTracking/WorkTrackingSystemConnection";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import {
	createMockApiServiceContext,
	createMockLicensingService,
	createMockPortfolioService,
	createMockSuggestionService,
	createMockTeamService,
} from "../../../tests/MockApiServiceProvider";
import { pastTheAutosaveDelay } from "../../../tests/pastTheAutosaveDelay";
import { renderWithQueryClient as render } from "../../../tests/renderWithQueryClient";
import { createMockProjectSettings } from "../../../tests/TestDataProvider";
import ModifyProjectSettings from "./ModifyProjectSettings";

const { reportUsage } = vi.hoisted(() => ({ reportUsage: vi.fn() }));

vi.mock("../../../services/UsageData/usageDataReporter", async (original) => ({
	...(await original<
		typeof import("../../../services/UsageData/usageDataReporter")
	>()),
	useUsageDataReporter: () => reportUsage,
}));

vi.mock("../../../hooks/useRbac", () => ({
	useRbac: () => ({
		isLoading: false,
		isRbacEnabled: false,
		isSystemAdmin: true,
		canCreateTeam: true,
		canCreatePortfolio: true,
		isTeamAdmin: () => true,
		isPortfolioAdmin: () => true,
		summary: {
			isRbacEnabled: false,
			isSystemAdmin: true,
			canCreateTeam: true,
			canCreatePortfolio: true,
			adminTeamIds: [],
			adminPortfolioIds: [],
		},
	}),
}));

const SWITCHED_OFF_EVENT = { name: "DefaultFeatureSizingSwitchedOff" };

describe("Switching default Feature sizing off is counted once, when the save is accepted", () => {
	const saveProjectSettings = vi.fn();

	const jira: IWorkTrackingSystemConnection = {
		id: 1,
		name: "System 1",
		options: [],
		workTrackingSystem: "Jira",
		authenticationMethodKey: "jira.cloud",
		additionalFieldDefinitions: [],
		writeBackMappingDefinitions: [],
		workTrackingSystemGetDataRetrievalDisplayName: () => "JQL Query",
	};

	const aSavedPortfolio = (infersDefaultFeatureSize: boolean) => ({
		...createMockProjectSettings(),
		...{ infersDefaultFeatureSize },
		name: "Ocean",
		dataRetrievalValue: "type = Epic",
		dataRetrievalSchema: {
			key: "jira.jql",
			displayLabel: "JQL Query",
			inputKind: "freetext" as const,
			isRequired: true,
			isWorkItemTypesRequired: true,
			wizardHint: null,
		},
	});

	const openPortfolioSettings = async (infersDefaultFeatureSize: boolean) => {
		const suggestionService = createMockSuggestionService();
		suggestionService.getStatesForProjects = vi
			.fn()
			.mockResolvedValue({ toDoStates: [], doingStates: [], doneStates: [] });
		const licensingService = createMockLicensingService();
		licensingService.getLicenseStatus = vi.fn().mockResolvedValue({
			hasLicense: true,
			isValid: true,
			canUsePremiumFeatures: true,
		});
		const teamService = createMockTeamService();
		teamService.getTeams = vi.fn().mockResolvedValue([]);
		const portfolioService = createMockPortfolioService();
		portfolioService.getPortfolios = vi.fn().mockResolvedValue([]);

		render(
			<ApiServiceContext.Provider
				value={createMockApiServiceContext({
					licensingService,
					suggestionService,
					teamService,
					portfolioService,
				})}
			>
				<ModifyProjectSettings
					title="Modify Project Settings"
					getWorkTrackingSystems={() => Promise.resolve([jira])}
					getProjectSettings={() =>
						Promise.resolve(aSavedPortfolio(infersDefaultFeatureSize))
					}
					getAllTeams={() => Promise.resolve([])}
					saveProjectSettings={saveProjectSettings}
					validateProjectSettings={() => Promise.resolve(true)}
				/>
			</ApiServiceContext.Provider>,
		);

		const name = await screen.findByLabelText("Name");
		await waitFor(() => expect(name).toHaveValue("Ocean"));
		const group = screen
			.getByText("Default Feature Size")
			.closest(".MuiCard-root") as HTMLElement;
		fireEvent.click(within(group).getByLabelText("toggle"));
		return { name, user: userEvent.setup(), group };
	};

	const theSwitch = (group: HTMLElement) =>
		within(group).getByLabelText(/Infer a size for Features/i);

	beforeEach(() => {
		vi.useFakeTimers({ shouldAdvanceTime: true });
		saveProjectSettings.mockReset();
		saveProjectSettings.mockResolvedValue(undefined);
		reportUsage.mockClear();
	});

	afterEach(() => {
		vi.useRealTimers();
	});

	it("reports once when a save turns it off, and not again for a later save that leaves it off", async () => {
		const { name, user, group } = await openPortfolioSettings(true);

		await user.click(theSwitch(group));
		await pastTheAutosaveDelay();

		expect(saveProjectSettings).toHaveBeenCalledTimes(1);
		expect(saveProjectSettings.mock.calls[0][0]).toMatchObject({
			infersDefaultFeatureSize: false,
		});
		expect(reportUsage).toHaveBeenCalledTimes(1);
		expect(reportUsage).toHaveBeenCalledWith(SWITCHED_OFF_EVENT);

		await user.type(name, " Two");
		await pastTheAutosaveDelay();

		expect(saveProjectSettings).toHaveBeenCalledTimes(2);
		expect(reportUsage).toHaveBeenCalledTimes(1);
	});

	it("reports nothing for a save that leaves it off, nor for one that turns it on", async () => {
		const { name, user, group } = await openPortfolioSettings(false);

		await user.type(name, " Two");
		await pastTheAutosaveDelay();
		expect(saveProjectSettings).toHaveBeenCalledTimes(1);

		await user.click(theSwitch(group));
		await pastTheAutosaveDelay();

		expect(saveProjectSettings).toHaveBeenCalledTimes(2);
		expect(saveProjectSettings.mock.calls[1][0]).toMatchObject({
			infersDefaultFeatureSize: true,
		});
		expect(reportUsage).not.toHaveBeenCalledWith(SWITCHED_OFF_EVENT);
	});

	it("reports nothing for a save the server rejects, and once for the accepted save that follows", async () => {
		const { name, user, group } = await openPortfolioSettings(true);
		saveProjectSettings.mockRejectedValueOnce(new Error("the server said no"));

		await user.click(theSwitch(group));
		await pastTheAutosaveDelay();

		expect(saveProjectSettings).toHaveBeenCalledTimes(1);
		expect(reportUsage).not.toHaveBeenCalled();

		await user.type(name, " Two");
		await pastTheAutosaveDelay();

		expect(saveProjectSettings).toHaveBeenCalledTimes(2);
		expect(saveProjectSettings.mock.calls[1][0]).toMatchObject({
			infersDefaultFeatureSize: false,
		});
		expect(reportUsage).toHaveBeenCalledTimes(1);
		expect(reportUsage).toHaveBeenCalledWith(SWITCHED_OFF_EVENT);
	});

	it("reports nothing for a save that leaves it on", async () => {
		const { name, user } = await openPortfolioSettings(true);

		await user.type(name, " Two");
		await pastTheAutosaveDelay();

		expect(saveProjectSettings).toHaveBeenCalledTimes(1);
		expect(reportUsage).not.toHaveBeenCalledWith(SWITCHED_OFF_EVENT);
	});
});
