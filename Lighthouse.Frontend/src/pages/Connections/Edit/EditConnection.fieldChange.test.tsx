import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import {
	act,
	fireEvent,
	render,
	screen,
	waitFor,
	within,
} from "@testing-library/react";
import { MemoryRouter } from "react-router";
import { beforeEach, describe, expect, it, vi } from "vitest";
import {
	type IWorkTrackingSystemConnection,
	WorkTrackingSystemConnection,
} from "../../../models/WorkTracking/WorkTrackingSystemConnection";
import { ApiError } from "../../../services/Api/ApiError";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import { TerminologyProvider } from "../../../services/TerminologyContext";
import {
	createMockApiServiceContext,
	createMockTerminologyService,
	createMockWorkTrackingSystemService,
} from "../../../tests/MockApiServiceProvider";
import EditConnectionPage from "./EditConnection";

/**
 * Priya changes the Additional Fields on "Nordwind Jira DC" and presses Save. Before
 * anything is saved the page asks whether to update every Team and Portfolio on the connection now,
 * because the values of a changed field only fill in once each of them has been updated. The page
 * already knows how many there are from the connection it loaded, in the words this instance uses for
 * them. A save that changes no field, or a connection nobody uses, goes straight through as before.
 *
 * The page is real down to the Additional Fields editor; only the server, the router and the usage
 * data reporter are stand-ins.
 */

const { mockNavigate, reportUsage, route } = vi.hoisted(() => ({
	mockNavigate: vi.fn(),
	reportUsage: vi.fn(),
	route: { id: "7" as string | undefined },
}));

vi.mock("react-router", async () => {
	const actual = await vi.importActual("react-router");
	return {
		...actual,
		useNavigate: () => mockNavigate,
		useParams: () => ({ id: route.id }),
	};
});

vi.mock("../../../hooks/useRbacGate", () => ({
	useRbacGate: () => ({ allowed: true, isLoading: false }),
}));

vi.mock("../../../hooks/useLicenseRestrictions", () => ({
	useLicenseRestrictions: () => ({
		licenseStatus: { canUsePremiumFeatures: true },
		canCreatePortfolio: true,
		canCreateTeam: true,
		createPortfolioTooltip: "",
		createTeamTooltip: "",
	}),
}));

vi.mock(
	"../../../services/UsageData/usageDataReporter",
	async (importOriginal) => ({
		...(await importOriginal<
			typeof import("../../../services/UsageData/usageDataReporter")
		>()),
		useUsageDataReporter: () => reportUsage,
	}),
);

const NordwindJiraDc = "Nordwind Jira DC";
const StoryPoints = "Story Points";
const FixVersions = "Fix Versions";

// Nordwind Jira DC is read by 4 Teams and 2 Portfolios unless a test says otherwise.
const TheQuestionOnNordwindJiraDc = "Update 4 Teams and 2 Portfolios?";

const TheFieldsAlert =
	"Additional Fields will be loaded with the next refresh of data.";
const SomeoneElseSavedFirst =
	"This connection was changed by someone else since you opened it. Reload to get the latest values, then re-apply your change.";

type UsedBy = { teams: number; portfolios: number };

type Field = { id: number; displayName: string; reference: string };

const TheStoryPointsField: Field = {
	id: 11,
	displayName: StoryPoints,
	reference: "customfield_10016",
};

type Words = {
	team: string;
	teams: string;
	portfolio: string;
	portfolios: string;
};

const TheDefaultWords: Words = {
	team: "Team",
	teams: "Teams",
	portfolio: "Portfolio",
	portfolios: "Portfolios",
};

const SquadsAndPrograms: Words = {
	team: "Squad",
	teams: "Squads",
	portfolio: "Program",
	portfolios: "Programs",
};

const nordwindJiraDc = (usedBy: UsedBy, fields: Field[]) =>
	Object.assign(
		new WorkTrackingSystemConnection({
			id: 7,
			name: NordwindJiraDc,
			workTrackingSystem: "Jira",
			authenticationMethodKey: "jira.cloud",
			availableAuthenticationMethods: [
				{
					key: "jira.cloud",
					displayName: "Jira Cloud",
					options: [
						{
							key: "ApiToken",
							displayName: "API Token",
							isSecret: true,
							isOptional: false,
						},
					],
				},
			],
			options: [
				{ key: "ApiToken", value: "", isSecret: true, isOptional: false },
				{
					key: "Url",
					value: "https://jira.nordwind.example",
					isSecret: false,
					isOptional: false,
				},
			],
			additionalFieldDefinitions: fields,
		}),
		{ usedBy },
	);

const givenPriyaOpensTheConnectionPage = async ({
	usedBy = { teams: 4, portfolios: 2 },
	words = TheDefaultWords,
	validation = { isValid: true },
	save = undefined as
		| (() => Promise<IWorkTrackingSystemConnection>)
		| undefined,
	fields = [TheStoryPointsField],
}: {
	usedBy?: UsedBy;
	fields?: Field[];
	words?: Words;
	validation?: { isValid: boolean; message?: string };
	save?: () => Promise<IWorkTrackingSystemConnection>;
} = {}) => {
	const server = createMockWorkTrackingSystemService();
	vi.mocked(server.getWorkTrackingSystems).mockResolvedValue([
		nordwindJiraDc(usedBy, fields),
	]);
	vi.mocked(server.addNewWorkTrackingSystemConnection).mockImplementation(
		async (connection) => connection,
	);
	vi.mocked(server.getConfiguredWorkTrackingSystems).mockResolvedValue([
		nordwindJiraDc(usedBy, fields),
	]);
	vi.mocked(server.validateWorkTrackingSystemConnection).mockResolvedValue(
		validation,
	);
	vi.mocked(server.updateWorkTrackingSystemConnection).mockImplementation(
		save ?? (async (connection) => connection),
	);

	const terminology = createMockTerminologyService();
	vi.mocked(terminology.getAllTerminology).mockResolvedValue(
		Object.entries(words).map(([key, value], index) => ({
			id: index + 1,
			key,
			defaultValue: TheDefaultWords[key as keyof Words],
			description: "",
			value,
		})),
	);

	render(
		<QueryClientProvider
			client={
				new QueryClient({
					defaultOptions: {
						queries: { retry: false },
						mutations: { retry: false },
					},
				})
			}
		>
			<MemoryRouter>
				<ApiServiceContext.Provider
					value={createMockApiServiceContext({
						workTrackingSystemService: server,
						terminologyService: terminology,
					})}
				>
					<TerminologyProvider>
						<EditConnectionPage />
					</TerminologyProvider>
				</ApiServiceContext.Provider>
			</MemoryRouter>
		</QueryClientProvider>,
	);

	return { server, terminology };
};

const givenPriyaOpensNordwindJiraDc = async (
	options: Parameters<typeof givenPriyaOpensTheConnectionPage>[0] = {},
) => {
	const { server, terminology } =
		await givenPriyaOpensTheConnectionPage(options);
	await screen.findByDisplayValue(NordwindJiraDc);
	if ((options.words ?? TheDefaultWords).team !== TheDefaultWords.team) {
		await waitFor(() =>
			expect(terminology.getAllTerminology).toHaveBeenCalled(),
		);
	}

	return server;
};

const givenPriyaSetsUpANewJiraConnection = async () => {
	const { server } = await givenPriyaOpensTheConnectionPage();
	fireEvent.click(await screen.findByRole("button", { name: NordwindJiraDc }));
	fireEvent.change(await screen.findByLabelText(/API Token/), {
		target: { value: "a-new-token" },
	});
	fireEvent.change(screen.getByLabelText(/^Url/), {
		target: { value: "https://jira.nordwind.example" },
	});
	fireEvent.click(screen.getByRole("button", { name: "Next" }));
	return server;
};

// --- What Priya does ---

const theFieldDialog = () => screen.findByRole("dialog");

const fillTheFieldDialog = async (displayName: string, reference?: string) => {
	const dialog = await theFieldDialog();
	fireEvent.change(within(dialog).getByLabelText(/Display Name/), {
		target: { value: displayName },
	});
	if (reference !== undefined) {
		fireEvent.change(within(dialog).getByLabelText(/Field Reference/), {
			target: { value: reference },
		});
	}
	return dialog;
};

const theFieldDialogCloses = () =>
	waitFor(() => expect(screen.queryByRole("dialog")).not.toBeInTheDocument());

const whenPriyaAddsTheField = async (
	displayName: string,
	reference: string,
) => {
	fireEvent.click(screen.getByRole("button", { name: "Add Field" }));
	const dialog = await fillTheFieldDialog(displayName, reference);
	fireEvent.click(within(dialog).getByRole("button", { name: "Save" }));
	await theFieldDialogCloses();
};

const whenPriyaAddsFixVersions = () =>
	whenPriyaAddsTheField(FixVersions, "fixVersions");

const whenPriyaRenamesStoryPoints = async () => {
	fireEvent.click(screen.getByRole("button", { name: "edit" }));
	const dialog = await fillTheFieldDialog("Estimate");
	fireEvent.click(within(dialog).getByRole("button", { name: "Save" }));
	await theFieldDialogCloses();
};

const theRowOf = (displayName: string) =>
	screen.getByText(displayName).closest("li") as HTMLElement;

const whenPriyaEditsTheFieldIn = async (
	row: HTMLElement,
	displayName: string,
	reference: string,
) => {
	fireEvent.click(within(row).getByRole("button", { name: "edit" }));
	const dialog = await fillTheFieldDialog(displayName, reference);
	fireEvent.click(within(dialog).getByRole("button", { name: "Save" }));
	await theFieldDialogCloses();
};

const whenPriyaRemovesStoryPoints = () =>
	fireEvent.click(screen.getByRole("button", { name: "delete" }));

const FieldEdits = [
	["adds a field", whenPriyaAddsFixVersions],
	["renames a field", whenPriyaRenamesStoryPoints],
	["removes a field", whenPriyaRemovesStoryPoints],
] as const;

const whenPriyaPressesSave = () =>
	fireEvent.click(screen.getByRole("button", { name: /^Save$/ }));

const theQuestion = (title: string) =>
	screen.findByRole("dialog", { name: title });

const whenSheAnswers = async (title: string, answer: string) => {
	const question = await theQuestion(title);
	fireEvent.click(within(question).getByRole("button", { name: answer }));
};

// --- What she sees ---

const thenTheConnectionIsSavedWith = async (
	server: ReturnType<typeof createMockWorkTrackingSystemService>,
	displayName: string,
) => {
	await waitFor(() =>
		expect(server.updateWorkTrackingSystemConnection).toHaveBeenCalledTimes(1),
	);
	const [saved] = vi.mocked(server.updateWorkTrackingSystemConnection).mock
		.calls[0];
	expect(
		saved.additionalFieldDefinitions.map((field) => field.displayName),
	).toContain(displayName);
};

const theChoiceTheSaveCarried = (
	server: ReturnType<typeof createMockWorkTrackingSystemService>,
) => vi.mocked(server.updateWorkTrackingSystemConnection).mock.calls[0][1];

const thenSheIsBackOnTheOverview = () =>
	waitFor(() => expect(mockNavigate).toHaveBeenCalledWith("/"));

const thenNoQuestionIsAsked = () =>
	expect(
		screen.queryByRole("dialog", { name: /^Update / }),
	).not.toBeInTheDocument();

// The question already said what the save starts, so nothing is announced after it.
const thenNoNoticeAboutTheUpdatesIsShown = () => {
	expect(screen.queryAllByRole("alert")).toEqual([]);
	expect(
		screen.queryByText(
			/being updated|are updating|will be loaded|next refresh/i,
		),
	).not.toBeInTheDocument();
};

// A save that stays on its way until the test lets it succeed or refuses it.
const aSaveStillOnItsWay = () => {
	const settle = {
		succeed: () => {},
		refuse: (_error: unknown) => {},
	};
	const onItsWay = new Promise<IWorkTrackingSystemConnection>(
		(resolve, reject) => {
			settle.succeed = () =>
				resolve(nordwindJiraDc({ teams: 4, portfolios: 2 }, []));
			settle.refuse = reject;
		},
	);
	return { save: () => onItsWay, ...settle };
};

const TheAnswers = ["Cancel", "Save only", "Save and update"] as const;

// A closed question leaves the page only once its closing animation has run.
const longerThanTheQuestionTakesToClose = () =>
	act(() => new Promise((resolve) => setTimeout(resolve, 500)));

const thenTheQuestionWaitsOnTheSave = async (answer: string) => {
	const question = await theQuestion(TheQuestionOnNordwindJiraDc);
	const busy = within(question).getByRole("button", { name: answer });
	await waitFor(() =>
		expect(within(busy).getByRole("progressbar")).toBeInTheDocument(),
	);
	expect(within(question).getAllByRole("progressbar")).toHaveLength(1);
	for (const each of TheAnswers) {
		expect(within(question).getByRole("button", { name: each })).toBeDisabled();
	}
	return question;
};

const theUsageDataSent = () =>
	reportUsage.mock.calls
		.map(([use]) => use as { name: string })
		.filter((use) => use.name === "WorkTrackingSystemFieldsChanged");

describe("changing a connection's Additional Fields asks whether to update its Teams and Portfolios", () => {
	beforeEach(() => {
		vi.clearAllMocks();
		route.id = "7";
	});

	describe("the question before the save", () => {
		// @walking_skeleton @driving_port @us-02 @contract-shape:bounded-change
		it("adding a field and pressing Save asks whether to update the 4 Teams and 2 Portfolios on the connection", async () => {
			const server = await givenPriyaOpensNordwindJiraDc();

			await whenPriyaAddsFixVersions();
			whenPriyaPressesSave();

			const question = await theQuestion(TheQuestionOnNordwindJiraDc);
			expect(question).toHaveTextContent(
				"You changed the Additional Fields. Their values only fill in once each Team and Portfolio on this connection has been updated.",
			);
			expect(
				within(question).getByRole("button", { name: "Cancel" }),
			).toBeInTheDocument();
			expect(
				within(question).getByRole("button", { name: "Save and update" }),
			).toHaveFocus();
			expect(server.updateWorkTrackingSystemConnection).not.toHaveBeenCalled();
		});

		// @driving_port @us-02 @contract-shape:bounded-change
		it("Save and update saves the new field without holding the updates back and returns to the overview", async () => {
			const server = await givenPriyaOpensNordwindJiraDc();
			await whenPriyaAddsFixVersions();
			whenPriyaPressesSave();

			await whenSheAnswers(TheQuestionOnNordwindJiraDc, "Save and update");

			await thenTheConnectionIsSavedWith(server, FixVersions);
			expect([undefined, true]).toContain(
				theChoiceTheSaveCarried(server)?.startUpdates,
			);
			await thenSheIsBackOnTheOverview();
			thenNoNoticeAboutTheUpdatesIsShown();
		});

		// @us-02 @contract-shape:bounded-change
		it.each(FieldEdits)(
			"the question is asked whenever Priya %s",
			async (_, edit) => {
				await givenPriyaOpensNordwindJiraDc();

				await edit();
				whenPriyaPressesSave();

				expect(
					await theQuestion(TheQuestionOnNordwindJiraDc),
				).toBeInTheDocument();
			},
		);

		// @error @us-02 @contract-shape:unbounded-preservation
		it("Cancel saves nothing and leaves Priya on the page with her new field still listed", async () => {
			const server = await givenPriyaOpensNordwindJiraDc();
			await whenPriyaAddsFixVersions();
			whenPriyaPressesSave();

			await whenSheAnswers(TheQuestionOnNordwindJiraDc, "Cancel");

			await waitFor(thenNoQuestionIsAsked);
			expect(server.updateWorkTrackingSystemConnection).not.toHaveBeenCalled();
			expect(mockNavigate).not.toHaveBeenCalled();
			expect(screen.getByText(FixVersions)).toBeInTheDocument();
			expect(screen.getByDisplayValue(NordwindJiraDc)).toBeInTheDocument();
		});

		// @us-02 @contract-shape:pure-function
		it.each([
			[
				"one of each",
				{ teams: 1, portfolios: 1 },
				TheDefaultWords,
				"Update 1 Team and 1 Portfolio?",
			],
			[
				"Squads and Programs",
				{ teams: 4, portfolios: 2 },
				SquadsAndPrograms,
				"Update 4 Squads and 2 Programs?",
			],
			[
				"one Squad and one Program",
				{ teams: 1, portfolios: 1 },
				SquadsAndPrograms,
				"Update 1 Squad and 1 Program?",
			],
			// A count of zero keeps its place, so every question has the same shape and says outright that
			// one kind is left alone.
			[
				"no Portfolio",
				{ teams: 3, portfolios: 0 },
				TheDefaultWords,
				"Update 3 Teams and 0 Portfolios?",
			],
			[
				"no Team",
				{ teams: 0, portfolios: 2 },
				TheDefaultWords,
				"Update 0 Teams and 2 Portfolios?",
			],
		] as const)(
			"with %s the question reads as such",
			async (_, usedBy, words, title) => {
				await givenPriyaOpensNordwindJiraDc({ usedBy, words });
				await whenPriyaAddsFixVersions();

				whenPriyaPressesSave();

				expect(await theQuestion(title)).toBeInTheDocument();
			},
		);

		// @us-02 @contract-shape:pure-function
		it("the explanation names the Teams and Portfolios in the words this instance uses", async () => {
			await givenPriyaOpensNordwindJiraDc({ words: SquadsAndPrograms });
			await whenPriyaAddsFixVersions();

			whenPriyaPressesSave();

			const question = await theQuestion("Update 4 Squads and 2 Programs?");
			expect(question).toHaveTextContent(
				"You changed the Additional Fields. Their values only fill in once each Squad and Program on this connection has been updated.",
			);
		});

		// @us-02 @contract-shape:unbounded-preservation
		it("changing a field no longer promises it will load with the next refresh", async () => {
			await givenPriyaOpensNordwindJiraDc();

			await whenPriyaAddsFixVersions();

			expect(screen.queryByText(TheFieldsAlert)).not.toBeInTheDocument();
		});

		// @error @us-02 @contract-shape:unbounded-preservation
		it("when someone else saved the connection first, Save and update says so and stays on the page", async () => {
			const server = await givenPriyaOpensNordwindJiraDc({
				save: () => Promise.reject(new ApiError(409, "Conflict")),
			});
			await whenPriyaAddsFixVersions();
			whenPriyaPressesSave();

			await whenSheAnswers(TheQuestionOnNordwindJiraDc, "Save and update");

			expect(
				await screen.findByText(SomeoneElseSavedFirst),
			).toBeInTheDocument();
			expect(server.updateWorkTrackingSystemConnection).toHaveBeenCalledTimes(
				1,
			);
			expect(mockNavigate).not.toHaveBeenCalled();
		});

		// These guards predate the question and must keep holding alongside it.

		// A new connection has no Team or Portfolio yet, and setting one up offers no Additional Fields to add.
		// @error @us-02 @contract-shape:unbounded-preservation
		it("setting up a new connection saves it without a question", async () => {
			route.id = undefined;
			const server = await givenPriyaSetsUpANewJiraConnection();

			fireEvent.click(await screen.findByRole("button", { name: "Create" }));

			await waitFor(() =>
				expect(server.addNewWorkTrackingSystemConnection).toHaveBeenCalledTimes(
					1,
				),
			);
			thenNoQuestionIsAsked();
			expect(server.updateWorkTrackingSystemConnection).not.toHaveBeenCalled();
			await thenSheIsBackOnTheOverview();
		});

		// @error @us-02 @contract-shape:unbounded-preservation
		it("a field change on a connection no Team or Portfolio uses is saved straight away without a question", async () => {
			const server = await givenPriyaOpensNordwindJiraDc({
				usedBy: { teams: 0, portfolios: 0 },
			});
			await whenPriyaAddsFixVersions();

			whenPriyaPressesSave();

			await thenTheConnectionIsSavedWith(server, FixVersions);
			thenNoQuestionIsAsked();
			await thenSheIsBackOnTheOverview();
		});

		// @error @us-02 @contract-shape:unbounded-preservation
		it.each([
			[
				"a new name",
				() =>
					fireEvent.change(screen.getByLabelText("Connection Name"), {
						target: { value: "Nordwind Jira" },
					}),
			],
			[
				"a new API token",
				() =>
					fireEvent.change(screen.getByLabelText("API Token"), {
						target: { value: "a-new-token" },
					}),
			],
		])(
			"a save with only %s is saved straight away without a question",
			async (_, edit) => {
				const server = await givenPriyaOpensNordwindJiraDc();
				edit();

				whenPriyaPressesSave();

				await thenTheConnectionIsSavedWith(server, StoryPoints);
				thenNoQuestionIsAsked();
				await thenSheIsBackOnTheOverview();
			},
		);

		// @error @us-02 @contract-shape:unbounded-preservation
		it("a field Priya started to add and then cancelled asks no question", async () => {
			const server = await givenPriyaOpensNordwindJiraDc();
			fireEvent.click(screen.getByRole("button", { name: "Add Field" }));
			const dialog = await fillTheFieldDialog(FixVersions, "fixVersions");
			fireEvent.click(within(dialog).getByRole("button", { name: "Cancel" }));
			await theFieldDialogCloses();

			whenPriyaPressesSave();

			await thenTheConnectionIsSavedWith(server, StoryPoints);
			thenNoQuestionIsAsked();
		});

		// @error @us-02 @contract-shape:unbounded-preservation
		it("a field Priya added and removed again before saving asks no question", async () => {
			const server = await givenPriyaOpensNordwindJiraDc();
			await whenPriyaAddsFixVersions();
			fireEvent.click(
				within(theRowOf(FixVersions)).getByRole("button", { name: "delete" }),
			);

			whenPriyaPressesSave();

			await thenTheConnectionIsSavedWith(server, StoryPoints);
			thenNoQuestionIsAsked();
		});

		// Stored values and rules hang off a field's id, and the server gives a field that comes back
		// under a new id a fresh one, so it loads it again even though its name and reference are as before.
		// @us-02 @contract-shape:bounded-change
		it("a field Priya removed and added back with the same name and reference asks the question", async () => {
			await givenPriyaOpensNordwindJiraDc();
			whenPriyaRemovesStoryPoints();
			await whenPriyaAddsTheField(StoryPoints, "customfield_10016");

			whenPriyaPressesSave();

			expect(
				await theQuestion(TheQuestionOnNordwindJiraDc),
			).toBeInTheDocument();
		});

		// @us-02 @contract-shape:bounded-change
		it("swapping the names and references of two fields asks the question", async () => {
			await givenPriyaOpensNordwindJiraDc({
				fields: [
					TheStoryPointsField,
					{ id: 12, displayName: FixVersions, reference: "fixVersions" },
				],
			});
			const storyPoints = theRowOf(StoryPoints);
			const fixVersions = theRowOf(FixVersions);
			await whenPriyaEditsTheFieldIn(storyPoints, FixVersions, "fixVersions");
			await whenPriyaEditsTheFieldIn(
				fixVersions,
				StoryPoints,
				"customfield_10016",
			);

			whenPriyaPressesSave();

			expect(
				await theQuestion(TheQuestionOnNordwindJiraDc),
			).toBeInTheDocument();
		});

		// @error @us-02 @contract-shape:unbounded-preservation
		it("a field change the tracker refuses asks no question and saves nothing", async () => {
			const server = await givenPriyaOpensNordwindJiraDc({
				validation: {
					isValid: false,
					message: "Jira has no field called fixVersions.",
				},
			});
			await whenPriyaAddsFixVersions();

			whenPriyaPressesSave();

			expect(
				await screen.findByText("Jira has no field called fixVersions."),
			).toBeInTheDocument();
			thenNoQuestionIsAsked();
			expect(server.updateWorkTrackingSystemConnection).not.toHaveBeenCalled();
		});
	});

	describe("save now, update later", () => {
		// @us-03 @contract-shape:pure-function
		it("the question offers to save only and says when the field fills in then", async () => {
			await givenPriyaOpensNordwindJiraDc();
			await whenPriyaAddsFixVersions();

			whenPriyaPressesSave();

			const question = await theQuestion(TheQuestionOnNordwindJiraDc);
			expect(question).toHaveTextContent(
				"Save only: they fill in with the next scheduled update instead.",
			);
			expect(
				within(question).getByRole("button", { name: "Save only" }),
			).toBeInTheDocument();
		});

		// @driving_port @us-03 @contract-shape:bounded-change
		it("Save only saves the new field, holds the updates back and returns to the overview", async () => {
			const server = await givenPriyaOpensNordwindJiraDc();
			await whenPriyaAddsFixVersions();
			whenPriyaPressesSave();

			await whenSheAnswers(TheQuestionOnNordwindJiraDc, "Save only");

			await thenTheConnectionIsSavedWith(server, FixVersions);
			expect(theChoiceTheSaveCarried(server)).toEqual({ startUpdates: false });
			await thenSheIsBackOnTheOverview();
			thenNoNoticeAboutTheUpdatesIsShown();
		});

		// @kpi @us-03 @contract-shape:bounded-change
		it.each([
			["Save and update", "Now"],
			["Save only", "Later"],
		])(
			`answering %s tells usage data the updates run %s and nothing else`,
			async (answer, updates) => {
				await givenPriyaOpensNordwindJiraDc();
				await whenPriyaAddsFixVersions();
				whenPriyaPressesSave();

				await whenSheAnswers(TheQuestionOnNordwindJiraDc, answer);

				await thenSheIsBackOnTheOverview();
				expect(theUsageDataSent()).toEqual([
					{ name: "WorkTrackingSystemFieldsChanged", updates },
				]);
			},
		);

		// @error @us-03 @contract-shape:unbounded-preservation
		it(`Cancel tells usage data nothing`, async () => {
			await givenPriyaOpensNordwindJiraDc();
			await whenPriyaAddsFixVersions();
			whenPriyaPressesSave();

			await whenSheAnswers(TheQuestionOnNordwindJiraDc, "Cancel");

			await waitFor(thenNoQuestionIsAsked);
			expect(theUsageDataSent()).toEqual([]);
		});

		// The answer only counts once the save it chose has gone through; a refused save changed nothing.
		// @error @us-03 @contract-shape:unbounded-preservation
		it(`a save refused because someone else saved first tells usage data nothing`, async () => {
			await givenPriyaOpensNordwindJiraDc({
				save: () => Promise.reject(new ApiError(409, "Conflict")),
			});
			await whenPriyaAddsFixVersions();
			whenPriyaPressesSave();

			await whenSheAnswers(TheQuestionOnNordwindJiraDc, "Save and update");

			expect(
				await screen.findByText(SomeoneElseSavedFirst),
			).toBeInTheDocument();
			expect(theUsageDataSent()).toEqual([]);
		});

		// @error @us-03 @contract-shape:unbounded-preservation
		it("a field change saved without a question tells usage data nothing", async () => {
			await givenPriyaOpensNordwindJiraDc({
				usedBy: { teams: 0, portfolios: 0 },
			});
			await whenPriyaAddsFixVersions();

			whenPriyaPressesSave();

			await thenSheIsBackOnTheOverview();
			expect(theUsageDataSent()).toEqual([]);
		});
	});

	describe("while the answer is being saved", () => {
		// @us-02 @contract-shape:bounded-change
		it.each(["Save and update", "Save only"])(
			"after %s the question stays open, that button is busy and no answer can be pressed until the save is done",
			async (answer) => {
				const saving = aSaveStillOnItsWay();
				const server = await givenPriyaOpensNordwindJiraDc({
					save: saving.save,
				});
				await whenPriyaAddsFixVersions();
				whenPriyaPressesSave();

				await whenSheAnswers(TheQuestionOnNordwindJiraDc, answer);

				await thenTheQuestionWaitsOnTheSave(answer);
				expect(server.updateWorkTrackingSystemConnection).toHaveBeenCalledTimes(
					1,
				);
				expect(mockNavigate).not.toHaveBeenCalled();

				saving.succeed();

				await thenSheIsBackOnTheOverview();
			},
		);

		// @error @us-02 @contract-shape:unbounded-preservation
		it("Esc does not close the question while the save is on its way", async () => {
			const saving = aSaveStillOnItsWay();
			await givenPriyaOpensNordwindJiraDc({ save: saving.save });
			await whenPriyaAddsFixVersions();
			whenPriyaPressesSave();
			await whenSheAnswers(TheQuestionOnNordwindJiraDc, "Save and update");
			const question = await thenTheQuestionWaitsOnTheSave("Save and update");

			fireEvent.keyDown(question, { key: "Escape" });
			await longerThanTheQuestionTakesToClose();

			expect(
				screen.getByRole("dialog", { name: TheQuestionOnNordwindJiraDc }),
			).toBeInTheDocument();
			saving.succeed();
			await thenSheIsBackOnTheOverview();
		});

		// @error @us-02 @contract-shape:unbounded-preservation
		it("when someone else saved first, the question closes and the page says so", async () => {
			const saving = aSaveStillOnItsWay();
			await givenPriyaOpensNordwindJiraDc({ save: saving.save });
			await whenPriyaAddsFixVersions();
			whenPriyaPressesSave();
			await whenSheAnswers(TheQuestionOnNordwindJiraDc, "Save and update");
			await thenTheQuestionWaitsOnTheSave("Save and update");

			saving.refuse(new ApiError(409, "Conflict"));

			expect(
				await screen.findByText(SomeoneElseSavedFirst),
			).toBeInTheDocument();
			await waitFor(thenNoQuestionIsAsked);
			expect(mockNavigate).not.toHaveBeenCalled();
		});
	});
});
