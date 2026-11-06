import { act, fireEvent, screen, waitFor } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import type { IBaseSettings } from "../../../models/Common/BaseSettings";
import type { IPortfolioSettings } from "../../../models/Portfolio/PortfolioSettings";
import type { ITeamSettings } from "../../../models/Team/TeamSettings";
import type { IWorkTrackingSystemConnection } from "../../../models/WorkTracking/WorkTrackingSystemConnection";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import type { IPortfolioService } from "../../../services/Api/PortfolioService";
import type { ITeamService } from "../../../services/Api/TeamService";
import { createMockApiServiceContext } from "../../../tests/MockApiServiceProvider";
import { pastTheAutosaveDelay } from "../../../tests/pastTheAutosaveDelay";
import { renderWithQueryClient as render } from "../../../tests/renderWithQueryClient";
import {
	createMockProjectSettings,
	createMockTeamSettings,
} from "../../../tests/TestDataProvider";
import type { ParentSourceEntry } from "../ConnectionDefault/parentSources";
import ModifyProjectSettings from "../ProjectSettings/ModifyProjectSettings";
import ModifyTeamSettings from "./ModifyTeamSettings";

const { reportUsage } = vi.hoisted(() => ({ reportUsage: vi.fn() }));

vi.mock(
	"../../../services/UsageData/usageDataReporter",
	async (importOriginal) => ({
		...(await importOriginal<
			typeof import("../../../services/UsageData/usageDataReporter")
		>()),
		useUsageDataReporter: () => reportUsage,
	}),
);

const Tracker: ParentSourceEntry = { kind: "TrackerParent" };
const Field: ParentSourceEntry = {
	kind: "AdditionalField",
	additionalFieldDefinitionId: 7,
};

vi.mock("../../../pages/Common/AdvancedInputs/AdvancedInputs.tsx", () => ({
	__esModule: true,
	default: ({
		onSettingsChange,
	}: {
		onSettingsChange: (
			key: keyof IBaseSettings,
			value: string | readonly ParentSourceEntry[],
		) => void;
	}) => (
		<div>
			<button
				type="button"
				onClick={() => onSettingsChange("parentSources", [Tracker, Field])}
			>
				List the tracker then the field
			</button>
			<button
				type="button"
				onClick={() => onSettingsChange("parentSources", [Field, Tracker])}
			>
				List the field then the tracker
			</button>
			<button
				type="button"
				onClick={() => onSettingsChange("parentSources", [Field])}
			>
				List only the field
			</button>
			<button
				type="button"
				onClick={() =>
					onSettingsChange("parentOverrideSource", "ConnectionDefault")
				}
			>
				Follow the default
			</button>
			<button
				type="button"
				onClick={() => onSettingsChange("parentOverrideSource", "Own")}
			>
				Set here
			</button>
			<button
				type="button"
				onClick={() => onSettingsChange("name", "Another name")}
			>
				Change another setting
			</button>
		</div>
	),
}));

vi.mock("../../DataRetrievalWizards", () => ({
	getWizardsForSystem: () => [],
}));
vi.mock("../BaseSettings/GeneralSettingsComponent", () => ({
	default: () => null,
}));
vi.mock("../ProjectSettings/PortfolioSizeNote", () => ({
	default: () => null,
}));
vi.mock("../WorkItemTypes/WorkItemTypesComponent", () => ({
	default: () => null,
}));
vi.mock("../StatesList/StatesList", () => ({ default: () => null }));

const JiraConnectionId = 1;

const jira: IWorkTrackingSystemConnection = {
	id: JiraConnectionId,
	name: "Jira",
	options: [],
	workTrackingSystem: "Jira",
	authenticationMethodKey: "jira.cloud",
	additionalFieldDefinitions: [],
	writeBackMappingDefinitions: [],
	workTrackingSystemGetDataRetrievalDisplayName: () => "JQL Query",
};

const parentSettings = (
	parentSources: readonly ParentSourceEntry[],
	parentOverrideSource: "Own" | "ConnectionDefault" = "Own",
) => ({
	workTrackingSystemConnectionId: JiraConnectionId,
	workTrackingSystem: jira.workTrackingSystem,
	parentOverrideSource,
	parentSources,
});

const theService = {
	updateTeamData: vi.fn(),
	getForecastFilterSchema: vi.fn().mockResolvedValue(null),
	refreshFeaturesForPortfolio: vi.fn(),
};

const save = vi.fn();

const renderPage = (page: React.ReactElement) => {
	const context = createMockApiServiceContext({
		teamService: theService as unknown as ITeamService,
		portfolioService: theService as unknown as IPortfolioService,
	});
	return render(
		<ApiServiceContext.Provider value={context}>
			{page}
		</ApiServiceContext.Provider>,
	);
};

const teamPage = (settings: Partial<IBaseSettings>) => (
	<ModifyTeamSettings
		title="Team"
		getWorkTrackingSystems={async () => [jira]}
		getTeamSettings={async () =>
			({ ...createMockTeamSettings(), id: 1, ...settings }) as ITeamSettings
		}
		saveTeamSettings={save}
		validateTeamSettings={async () => true}
	/>
);

const portfolioPage = (settings: Partial<IBaseSettings>) => (
	<ModifyProjectSettings
		title="Portfolio"
		getWorkTrackingSystems={async () => [jira]}
		getProjectSettings={async () =>
			({
				...createMockProjectSettings(),
				id: 1,
				...settings,
			}) as IPortfolioSettings
		}
		getAllTeams={async () => []}
		saveProjectSettings={save}
		validateProjectSettings={async () => true}
		disableSave={false}
	/>
);

const openTeam = async (settings: Partial<IBaseSettings>) => {
	const page = renderPage(teamPage(settings));
	await screen.findByText("Change another setting");
	return page;
};

const editAndSave = async (buttonName: string) => {
	const savesBefore = save.mock.calls.length;
	fireEvent.click(screen.getByText(buttonName));
	await pastTheAutosaveDelay();
	await waitFor(() =>
		expect(save.mock.calls.length).toBeGreaterThan(savesBefore),
	);
	await act(async () => {
		await Promise.resolve();
	});
};

const theEventsSent = () =>
	reportUsage.mock.calls.filter(
		([use]) => use.name === "ParentSourcesSavedWithSeveralEntries",
	);

const oneEventNamedAndNothingElse = [
	[{ name: "ParentSourcesSavedWithSeveralEntries" }],
];

describe("usage data of saving several parent sources", () => {
	beforeEach(() => {
		vi.useFakeTimers({ shouldAdvanceTime: true });
		reportUsage.mockClear();
		save.mockReset();
		save.mockImplementation(async (settings) => settings);
	});

	afterEach(() => {
		vi.useRealTimers();
	});

	describe.each([
		{ owner: "Team", page: teamPage },
		{ owner: "Portfolio", page: portfolioPage },
	])("on the settings of a $owner", ({ page }) => {
		it("sends the event once, by name alone, when an accepted save changes the list to two entries", async () => {
			renderPage(page(parentSettings([Tracker])));
			await screen.findByText("Change another setting");

			await editAndSave("List the tracker then the field");

			expect(theEventsSent()).toEqual(oneEventNamedAndNothingElse);
		});
	});

	it("is not sent before the server has accepted the save", async () => {
		let accept: (settings: ITeamSettings) => void = () => undefined;
		save.mockImplementation(
			() =>
				new Promise((resolve) => {
					accept = resolve;
				}),
		);
		await openTeam(parentSettings([Tracker]));

		await editAndSave("List the tracker then the field");
		expect(theEventsSent()).toEqual([]);

		await act(async () => {
			accept(save.mock.lastCall?.[0]);
			await Promise.resolve();
		});
		await waitFor(() =>
			expect(theEventsSent()).toEqual(oneEventNamedAndNothingElse),
		);
	});

	it("counts the same entries in another order as a changed list, since the order decides what is read first", async () => {
		await openTeam(parentSettings([Tracker, Field]));

		await editAndSave("List the field then the tracker");

		expect(theEventsSent()).toEqual(oneEventNamedAndNothingElse);
	});

	it("counts a follower setting its own list of two", async () => {
		await openTeam(parentSettings([Tracker, Field], "ConnectionDefault"));

		await editAndSave("Set here");

		expect(theEventsSent()).toEqual(oneEventNamedAndNothingElse);
	});

	it("does not count the same list again when a later save changes something else", async () => {
		await openTeam(parentSettings([Tracker]));
		await editAndSave("List the tracker then the field");

		await editAndSave("Change another setting");

		expect(theEventsSent()).toEqual(oneEventNamedAndNothingElse);
	});

	it("counts a save that leaves the page before the autosave delay is over", async () => {
		const opened = await openTeam(parentSettings([Tracker]));

		fireEvent.click(screen.getByText("List the tracker then the field"));
		opened.unmount();

		await waitFor(() =>
			expect(theEventsSent()).toEqual(oneEventNamedAndNothingElse),
		);
	});

	it("does not count a save the server refused", async () => {
		save.mockRejectedValue(new Error("refused"));
		await openTeam(parentSettings([Tracker]));

		await editAndSave("List the tracker then the field");

		expect(theEventsSent()).toEqual([]);
	});

	it.each([
		{
			when: "an unchanged list goes along with another setting",
			loaded: parentSettings([Tracker, Field]),
			edit: "Change another setting",
		},
		{
			when: "the server wrote the tracker's entry with an empty field where the page leaves it out",
			loaded: parentSettings([
				{ kind: "TrackerParent", additionalFieldDefinitionId: null },
				Field,
			]),
			edit: "List the tracker then the field",
		},
		{
			when: "the list holds a single entry",
			loaded: parentSettings([Tracker]),
			edit: "List only the field",
		},
		{
			when: "a follower's save carries the default's list back",
			loaded: parentSettings([Tracker, Field], "ConnectionDefault"),
			edit: "Change another setting",
		},
		{
			when: "somebody switches to follow the default",
			loaded: parentSettings([Tracker, Field]),
			edit: "Follow the default",
		},
	])("sends nothing when $when", async ({ loaded, edit }) => {
		await openTeam(loaded);

		await editAndSave(edit);

		expect(theEventsSent()).toEqual([]);
	});
});
