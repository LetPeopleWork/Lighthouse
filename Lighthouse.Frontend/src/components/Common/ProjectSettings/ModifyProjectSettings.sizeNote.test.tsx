import { act, screen, waitFor } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import type { IPortfolioSize } from "../../../models/Portfolio/PortfolioSize";
import { RenamedTerms, renderWithTerms } from "../../../tests/FieldTipsTestKit";
import { deferred } from "../../../tests/HeldMetricsService";
import {
	createMockPortfolioService,
	createMockSuggestionService,
} from "../../../tests/MockApiServiceProvider";
import { createMockProjectSettings } from "../../../tests/TestDataProvider";
import ModifyProjectSettings from "./ModifyProjectSettings";

vi.mock("../BaseSettings/GeneralSettingsComponent", () => ({
	default: () => <div>GeneralSettingsComponent</div>,
}));
vi.mock("../WorkItemTypes/WorkItemTypesComponent", () => ({
	default: () => <div>WorkItemTypesComponent</div>,
}));
vi.mock("../StatesList/StatesList", () => ({
	default: () => <div>StatesList</div>,
}));
vi.mock("../Tags/TagsComponent", () => ({
	default: () => <div>TagsComponent</div>,
}));
vi.mock("../StateMappings/StateMappingsEditor", () => ({
	default: () => <div>StateMappingsEditor</div>,
}));
vi.mock("../../../hooks/useLicenseRestrictions", () => ({
	useLicenseRestrictions: () => ({
		status: "ready",
		canCreateTeam: true,
		canUpdateTeamData: true,
		canCreatePortfolio: true,
		canUpdatePortfolioData: true,
		licenseStatus: { canUsePremiumFeatures: true },
		maxTeamsWithoutPremium: 3,
		maxPortfoliosWithoutPremium: 1,
	}),
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
	}),
}));

const NORTHWIND_PLATFORM = 2;
const A_NEW_PORTFOLIO = 0;

const theNoteFor = (
	count: number,
	terms = { features: "Features", portfolio: "Portfolio" },
) =>
	`This ${terms.portfolio} holds ${count} ${terms.features} (as of its last update). Above 100, its pages and forecasts get slower. If you don't plan with all of them, narrow the query.`;

const COULD_NOT_CHECK = "Couldn't check this Portfolio's size.";

const aSize = (featureCount: number): IPortfolioSize => ({
	featureCount,
	threshold: 100,
	isOverThreshold: featureCount > 100,
});

/** The edit page of a Portfolio, whose size the server answers with what the scenario chooses. */
const thePortfolioSettingsOpen = async (
	portfolioId: number,
	size: Promise<IPortfolioSize>,
	terms: Record<string, string> = {},
) => {
	size.catch(() => undefined);
	const portfolioService = {
		...createMockPortfolioService(),
		getSize: vi.fn(() => size),
		getPortfolios: vi.fn().mockResolvedValue([]),
	};
	const suggestionService = {
		...createMockSuggestionService(),
		getStatesForProjects: vi.fn().mockResolvedValue({
			toDoStates: ["New"],
			doingStates: ["Active"],
			doneStates: ["Done"],
		}),
	};

	await renderWithTerms(
		<ModifyProjectSettings
			title="Edit Portfolio"
			getWorkTrackingSystems={vi.fn().mockResolvedValue([])}
			getProjectSettings={vi
				.fn()
				.mockResolvedValue({ ...createMockProjectSettings(), id: portfolioId })}
			getAllTeams={vi.fn().mockResolvedValue([])}
			saveProjectSettings={vi.fn()}
			validateProjectSettings={vi.fn().mockResolvedValue(true)}
		/>,
		terms,
		{ portfolioService, suggestionService },
	);
	await screen.findByText("Edit Portfolio");

	return portfolioService;
};

describe("Story 6410 (slice 06): the Portfolio's settings say when it holds more than 100 Features", () => {
	it("a Portfolio holding 101 Features shows the note with its count, as information", async () => {
		await thePortfolioSettingsOpen(
			NORTHWIND_PLATFORM,
			Promise.resolve(aSize(101)),
		);

		const note = (await screen.findByText(theNoteFor(101))).closest(
			'[role="alert"]',
		);
		expect(note).not.toBeNull();
		expect(note).toHaveTextContent(theNoteFor(101));
		expect(note).toHaveClass("MuiAlert-colorInfo");
	});

	it("the note asks for the size of the Portfolio being edited", async () => {
		const portfolioService = await thePortfolioSettingsOpen(
			NORTHWIND_PLATFORM,
			Promise.resolve(aSize(101)),
		);

		await screen.findByText(theNoteFor(101));
		expect(portfolioService.getSize).toHaveBeenCalledWith(NORTHWIND_PLATFORM);
	});

	it("when the size cannot be checked, the page says so and stays usable", async () => {
		await thePortfolioSettingsOpen(
			NORTHWIND_PLATFORM,
			Promise.reject(new Error("the network dropped")),
		);

		expect(await screen.findByText(COULD_NOT_CHECK)).toBeInTheDocument();
		expect(screen.getByText("GeneralSettingsComponent")).toBeInTheDocument();
	});

	it("the note uses the instance's own words for Features and Portfolios", async () => {
		await thePortfolioSettingsOpen(
			NORTHWIND_PLATFORM,
			Promise.resolve(aSize(240)),
			RenamedTerms,
		);

		expect(
			await screen.findByText(
				theNoteFor(240, { features: "Bets", portfolio: "Program" }),
			),
		).toBeInTheDocument();
	});

	it("the could-not-check message uses the instance's own word for Portfolios", async () => {
		await thePortfolioSettingsOpen(
			NORTHWIND_PLATFORM,
			Promise.reject(new Error("the network dropped")),
			RenamedTerms,
		);

		expect(
			await screen.findByText("Couldn't check this Program's size."),
		).toBeInTheDocument();
	});

	it("a Portfolio holding exactly 100 Features shows no note", async () => {
		const size = Promise.resolve(aSize(100));
		const portfolioService = await thePortfolioSettingsOpen(
			NORTHWIND_PLATFORM,
			size,
		);

		await waitFor(() =>
			expect(portfolioService.getSize).toHaveBeenCalledWith(NORTHWIND_PLATFORM),
		);
		await act(async () => {
			await size;
		});

		expect(
			screen.queryByText(/\(as of its last update\)/),
		).not.toBeInTheDocument();
		expect(screen.queryByText(COULD_NOT_CHECK)).not.toBeInTheDocument();
	});

	it("while the size is on its way the page shows no note and no warning", async () => {
		const portfolioService = await thePortfolioSettingsOpen(
			NORTHWIND_PLATFORM,
			deferred<IPortfolioSize>().promise,
		);

		await waitFor(() =>
			expect(portfolioService.getSize).toHaveBeenCalledWith(NORTHWIND_PLATFORM),
		);
		expect(
			screen.queryByText(/\(as of its last update\)/),
		).not.toBeInTheDocument();
		expect(screen.queryByText(COULD_NOT_CHECK)).not.toBeInTheDocument();
	});

	it("a Portfolio being created is not asked for its size", async () => {
		const portfolioService = await thePortfolioSettingsOpen(
			A_NEW_PORTFOLIO,
			Promise.resolve(aSize(0)),
		);

		expect(portfolioService.getSize).not.toHaveBeenCalled();
	});
});
