import { screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { IWorkTrackingSystemConnection } from "../../../models/WorkTracking/WorkTrackingSystemConnection";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import {
	createMockApiServiceContext,
	createMockLicensingService,
	createMockPortfolioService,
	createMockSuggestionService,
	createMockTeamService,
} from "../../../tests/MockApiServiceProvider";
import { renderWithQueryClient as render } from "../../../tests/renderWithQueryClient";
import { createMockProjectSettings } from "../../../tests/TestDataProvider";
import ModifyProjectSettings from "./ModifyProjectSettings";

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

// Each test waits past the 300 ms debounce in real time, on top of a full page render.
vi.setConfig({ testTimeout: 20000 });

const PAST_THE_DEBOUNCE_MS = 500;
const waitPastTheDebounce = () =>
	new Promise((resolve) => setTimeout(resolve, PAST_THE_DEBOUNCE_MS));

describe("ModifyProjectSettings query field", () => {
	const mockSaveProjectSettings = vi.fn();

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

	const aSavedPortfolio = () => ({
		...createMockProjectSettings(),
		name: "Project A",
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

	const openPortfolioSettings = async () => {
		const suggestionService = createMockSuggestionService();
		suggestionService.getStatesForProjects = vi.fn().mockResolvedValue({
			toDoStates: [],
			doingStates: [],
			doneStates: [],
		});
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
					getProjectSettings={() => Promise.resolve(aSavedPortfolio())}
					getAllTeams={() => Promise.resolve([])}
					saveProjectSettings={mockSaveProjectSettings}
					validateProjectSettings={() => Promise.resolve(true)}
				/>
			</ApiServiceContext.Provider>,
		);

		const query = await screen.findByLabelText("JQL Query");
		await waitFor(() => expect(query).toHaveValue("type = Epic"));
		return { query, user: userEvent.setup() };
	};

	beforeEach(() => {
		mockSaveProjectSettings.mockReset();
		mockSaveProjectSettings.mockResolvedValue(undefined);
	});

	it("saves nothing when the query is focused and left without a change", async () => {
		const { query, user } = await openPortfolioSettings();

		await user.click(query);
		await user.tab();
		await waitPastTheDebounce();

		expect(mockSaveProjectSettings).not.toHaveBeenCalled();
	});

	it("saves nothing while the query is being typed and the field is still focused", async () => {
		const { query, user } = await openPortfolioSettings();

		await user.click(query);
		await user.type(query, " AND project = LH");
		await waitPastTheDebounce();

		expect(mockSaveProjectSettings).not.toHaveBeenCalled();
	});

	it("saves the edited query once, after the field is left", async () => {
		const { query, user } = await openPortfolioSettings();

		await user.click(query);
		await user.type(query, " AND project = LH");
		await waitPastTheDebounce();
		expect(mockSaveProjectSettings).not.toHaveBeenCalled();

		await user.tab();
		await waitFor(() =>
			expect(mockSaveProjectSettings).toHaveBeenCalledTimes(1),
		);
		expect(mockSaveProjectSettings).toHaveBeenCalledWith(
			expect.objectContaining({
				dataRetrievalValue: "type = Epic AND project = LH",
			}),
		);

		await waitPastTheDebounce();
		expect(mockSaveProjectSettings).toHaveBeenCalledTimes(1);
	});

	it("saves nothing when the query is edited back to what was saved before the field is left", async () => {
		const { query, user } = await openPortfolioSettings();

		await user.click(query);
		await user.type(query, "x");
		await user.keyboard("{Backspace}");
		await user.tab();
		await waitPastTheDebounce();

		expect(mockSaveProjectSettings).not.toHaveBeenCalled();
	});
});
