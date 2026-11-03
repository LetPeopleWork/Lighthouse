import { act, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { DEBOUNCE_MS } from "../../../hooks/useModifySettings";
import type { IWorkTrackingSystemConnection } from "../../../models/WorkTracking/WorkTrackingSystemConnection";
import {
	ApiServiceContext,
	type IApiServiceContext,
} from "../../../services/Api/ApiServiceContext";
import type { ITeamService } from "../../../services/Api/TeamService";
import { createMockApiServiceContext } from "../../../tests/MockApiServiceProvider";
import { renderWithQueryClient as render } from "../../../tests/renderWithQueryClient";
import { createMockTeamSettings } from "../../../tests/TestDataProvider";
import ModifyTeamSettings from "./ModifyTeamSettings";

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

// A full page render plus a typed query can take several seconds on a loaded machine.
vi.setConfig({ testTimeout: 20000 });

// The clock is moved on rather than waited out, so a pending autosave has surely fired by the
// time nothing is checked as saved.
const pastTheAutosaveDelay = () =>
	act(async () => {
		await vi.advanceTimersByTimeAsync(DEBOUNCE_MS + 200);
	});

describe("ModifyTeamSettings query field", () => {
	const mockSaveTeamSettings = vi.fn();

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

	const aSavedTeam = () => ({
		...createMockTeamSettings(),
		name: "Team A",
		dataRetrievalValue: "project = LH",
		dataRetrievalSchema: {
			key: "jira.jql",
			displayLabel: "JQL Query",
			inputKind: "freetext" as const,
			isRequired: true,
			isWorkItemTypesRequired: true,
			wizardHint: null,
		},
	});

	const openTeamSettings = async () => {
		const context: IApiServiceContext = createMockApiServiceContext({
			teamService: {
				updateTeamData: vi.fn().mockResolvedValue(undefined),
				getForecastFilterSchema: vi.fn().mockResolvedValue(null),
			} as unknown as ITeamService,
		});

		const view = render(
			<ApiServiceContext.Provider value={context}>
				<ModifyTeamSettings
					title="Modify Team Settings"
					getWorkTrackingSystems={() => Promise.resolve([jira])}
					getTeamSettings={() => Promise.resolve(aSavedTeam())}
					saveTeamSettings={mockSaveTeamSettings}
					validateTeamSettings={() => Promise.resolve(true)}
				/>
			</ApiServiceContext.Provider>,
		);

		const query = await screen.findByLabelText("JQL Query");
		await waitFor(() => expect(query).toHaveValue("project = LH"));
		return { query, user: userEvent.setup(), leaveThePage: view.unmount };
	};

	afterEach(() => {
		vi.useRealTimers();
	});

	beforeEach(() => {
		vi.useFakeTimers({ shouldAdvanceTime: true });
		mockSaveTeamSettings.mockReset();
		mockSaveTeamSettings.mockResolvedValue(undefined);
	});

	it("saves a changed name after the debounce without anyone leaving a field", async () => {
		const { user } = await openTeamSettings();

		await user.type(screen.getByLabelText("Name"), " Two");

		await waitFor(() =>
			expect(mockSaveTeamSettings).toHaveBeenCalledWith(
				expect.objectContaining({ name: "Team A Two" }),
			),
		);
	});

	it("saves nothing when the query is focused and left without a change", async () => {
		const { query, user } = await openTeamSettings();

		await user.click(query);
		await user.tab();
		await pastTheAutosaveDelay();

		expect(mockSaveTeamSettings).not.toHaveBeenCalled();
	});

	it("saves nothing while the query is being typed and the field is still focused", async () => {
		const { query, user } = await openTeamSettings();

		await user.click(query);
		await user.type(query, " AND team = Red");
		await pastTheAutosaveDelay();

		expect(mockSaveTeamSettings).not.toHaveBeenCalled();
	});

	it("saves the edited query once, after the field is left", async () => {
		const { query, user } = await openTeamSettings();

		await user.click(query);
		await user.type(query, " AND team = Red");
		await pastTheAutosaveDelay();
		expect(mockSaveTeamSettings).not.toHaveBeenCalled();

		await user.tab();
		await waitFor(() => expect(mockSaveTeamSettings).toHaveBeenCalledTimes(1));
		expect(mockSaveTeamSettings).toHaveBeenCalledWith(
			expect.objectContaining({
				dataRetrievalValue: "project = LH AND team = Red",
			}),
		);

		await pastTheAutosaveDelay();
		expect(mockSaveTeamSettings).toHaveBeenCalledTimes(1);
	});

	it("saves nothing when the query is edited back to what was saved before the field is left", async () => {
		const { query, user } = await openTeamSettings();

		await user.click(query);
		await user.type(query, "x");
		await user.keyboard("{Backspace}");
		await user.tab();
		await pastTheAutosaveDelay();

		expect(mockSaveTeamSettings).not.toHaveBeenCalled();
	});

	it("saves the edited query when the page is left straight after the field", async () => {
		const { query, user, leaveThePage } = await openTeamSettings();

		await user.click(query);
		await user.type(query, " AND team = Red");
		await user.tab();
		leaveThePage();
		await pastTheAutosaveDelay();

		expect(mockSaveTeamSettings).toHaveBeenCalledTimes(1);
		expect(mockSaveTeamSettings).toHaveBeenCalledWith(
			expect.objectContaining({
				dataRetrievalValue: "project = LH AND team = Red",
			}),
		);
	});

	it("saves a changed name when the page is left before the debounce runs out", async () => {
		const { user, leaveThePage } = await openTeamSettings();

		await user.type(screen.getByLabelText("Name"), " Two");
		leaveThePage();
		await pastTheAutosaveDelay();

		expect(mockSaveTeamSettings).toHaveBeenCalledTimes(1);
		expect(mockSaveTeamSettings).toHaveBeenCalledWith(
			expect.objectContaining({ name: "Team A Two" }),
		);
	});

	it("saves the latest edit after the save already on its way, when the page is left meanwhile", async () => {
		let finishFirstSave: () => void = () => {};
		mockSaveTeamSettings.mockImplementationOnce(
			() =>
				new Promise<undefined>((resolve) => {
					finishFirstSave = () => resolve(undefined);
				}),
		);
		const { user, leaveThePage } = await openTeamSettings();

		await user.type(screen.getByLabelText("Name"), " Two");
		await waitFor(() => expect(mockSaveTeamSettings).toHaveBeenCalledTimes(1));
		await user.type(screen.getByLabelText("Name"), " Three");
		leaveThePage();
		expect(mockSaveTeamSettings).toHaveBeenCalledTimes(1);

		finishFirstSave();
		await pastTheAutosaveDelay();

		expect(mockSaveTeamSettings).toHaveBeenCalledTimes(2);
		expect(mockSaveTeamSettings).toHaveBeenLastCalledWith(
			expect.objectContaining({ name: "Team A Two Three" }),
		);
	});

	it("saves nothing when the page is left while the form is invalid", async () => {
		const { user, leaveThePage } = await openTeamSettings();

		await user.clear(screen.getByLabelText("Name"));
		expect(screen.getByLabelText("Name")).toHaveValue("");
		leaveThePage();
		await pastTheAutosaveDelay();

		expect(mockSaveTeamSettings).not.toHaveBeenCalled();
	});

	it("saves nothing when the page is left without an edit", async () => {
		const { query, user, leaveThePage } = await openTeamSettings();

		await user.click(query);
		await user.tab();
		leaveThePage();
		await pastTheAutosaveDelay();

		expect(mockSaveTeamSettings).not.toHaveBeenCalled();
	});
});
