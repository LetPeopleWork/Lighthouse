import { createTheme, ThemeProvider } from "@mui/material";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import {
	fireEvent,
	render,
	screen,
	waitFor,
	within,
} from "@testing-library/react";
import { MemoryRouter } from "react-router";
import { beforeEach, describe, expect, it, vi } from "vitest";
import {
	type IConnectionDefaults,
	type IEstimationDefault,
	type IWorkTrackingSystemConnection,
	WorkTrackingSystemConnection,
	type WorkTrackingSystemType,
} from "../../../models/WorkTracking/WorkTrackingSystemConnection";
import { ApiError } from "../../../services/Api/ApiError";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import { WorkTrackingSystemService } from "../../../services/Api/WorkTrackingSystemService";
import { TerminologyProvider } from "../../../services/TerminologyContext";
import {
	createMockApiServiceContext,
	createMockTerminologyService,
	createMockWorkTrackingSystemService,
} from "../../../tests/MockApiServiceProvider";
import EditConnectionPage from "./EditConnection";

/**
 * Priya, System Admin of the Payments department, opens "Jira DC – Payments" and sets, once, the field
 * every Team and Portfolio on it reads unless it names its own. The connection page gains a section
 * "Defaults for Teams and Portfolios" below the Additional Fields, with one row per setting the tracker
 * honours. Before a save that changes a default somebody follows, the page says how many follow it, never
 * who; the names go to the log. Nobody following means no question at all.
 *
 * The page is real down to its editors; only the server, the router and the usage data reporter are
 * stand-ins.
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

const JiraDcPayments = "Jira DC – Payments";
const JiraDcTreasury = "Jira DC – Treasury";
const TheCouldNotLoadLook = "Could not load this connection.";
const CausedBy = "Caused by";
const ParentLink = "Parent Link";
const StoryPoints = "Story Points";

const TheSection = "Defaults for Teams and Portfolios";
const TheIntro =
	"Teams and Portfolios on this connection use these unless they set their own.";

const TheUpdateQuestion = "Update 3 Teams and 0 Portfolios?";
const TheUpdateQuestionSays =
	"3 Teams follow the Parent sources default. Their parents change only once they have been updated. Save only: they change at their next scheduled update.";
const TheFieldsQuestion = "Update 4 Teams and 2 Portfolios?";
const TheFieldsQuestionSays =
	"You changed the Additional Fields. Their values only fill in once each Team and Portfolio on this connection has been updated. Save only: they fill in with the next scheduled update instead.";
const TheEstimationQuestion = "Change the Estimation default?";
const TheEstimationQuestionSays =
	"4 Teams follow it; they show the new estimation the next time their pages load.";

type Words = {
	team: string;
	teams: string;
	portfolio: string;
	portfolios: string;
	feature: string;
	features: string;
};

const TheDefaultWords: Words = {
	team: "Team",
	teams: "Teams",
	portfolio: "Portfolio",
	portfolios: "Portfolios",
	feature: "Feature",
	features: "Features",
};

const SquadsProgramsAndEpics: Words = {
	team: "Squad",
	teams: "Squads",
	portfolio: "Program",
	portfolios: "Programs",
	feature: "Epic",
	features: "Epics",
};

type Followers = Partial<Record<DefaultMember, number>>;

type DefaultMember =
	| "parentOverrideForTeams"
	| "estimationForTeams"
	| "parentOverrideForPortfolios"
	| "dependencyFieldForPortfolios"
	| "sizeEstimateFieldForPortfolios"
	| "featureOwnerFieldForPortfolios"
	| "estimationForPortfolios";

const EveryDefault: DefaultMember[] = [
	"parentOverrideForTeams",
	"estimationForTeams",
	"parentOverrideForPortfolios",
	"dependencyFieldForPortfolios",
	"sizeEstimateFieldForPortfolios",
	"featureOwnerFieldForPortfolios",
	"estimationForPortfolios",
];

const WhatEachTrackerOffers: Record<WorkTrackingSystemType, DefaultMember[]> = {
	Jira: EveryDefault,
	AzureDevOps: EveryDefault,
	Csv: [
		"estimationForTeams",
		"featureOwnerFieldForPortfolios",
		"estimationForPortfolios",
	],
	Linear: [],
	ServiceNow: [],
};

const TheFields = [
	{ id: 11, displayName: CausedBy, reference: "customfield_10500" },
	{ id: 12, displayName: ParentLink, reference: "customfield_10014" },
	{ id: 13, displayName: StoryPoints, reference: "customfield_10016" },
];

const sourcesOf = (...fieldIds: number[]) =>
	fieldIds.map((additionalFieldDefinitionId) => ({
		kind: "AdditionalField" as const,
		additionalFieldDefinitionId,
	}));

const StoryPointsInPoints: IEstimationDefault = {
	additionalFieldDefinitionId: 13,
	unit: "Points",
	useNonNumericEstimation: false,
	estimationCategoryValues: [],
};

const NoAnimations = createTheme({
	transitions: { duration: { enteringScreen: 0, leavingScreen: 0 } },
});

const jiraDcPayments = ({
	id = 7,
	name = JiraDcPayments,
	tracker = "Jira" as WorkTrackingSystemType,
	followers = { parentOverrideForTeams: 3 } as Followers,
	followerGroups = undefined as
		| { follows: DefaultMember[]; count: number }[]
		| undefined,
	defaults = {} as IConnectionDefaults,
} = {}) =>
	Object.assign(
		new WorkTrackingSystemConnection({
			id,
			name,
			workTrackingSystem: tracker,
			authenticationMethodKey: "jira.datacenter",
			availableAuthenticationMethods: [
				{
					key: "jira.datacenter",
					displayName: "Jira Data Center",
					options: [],
				},
			],
			options: [
				{
					key: "Url",
					value: "https://jira.payments.example",
					isSecret: false,
					isOptional: false,
				},
			],
			additionalFieldDefinitions: TheFields,
		}),
		{
			usedBy: { teams: 4, portfolios: 2, followers, followerGroups },
			defaults,
			supportedDefaults: WhatEachTrackerOffers[tracker],
		},
	);

// Each read of the connections answers with the next of these; the last one answers every later read.
type Answers = Promise<IWorkTrackingSystemConnection[]>[];

let reopenThePage: (() => void) | undefined;

const givenPriyaOpensJiraDcPayments = async ({
	connection = jiraDcPayments(),
	words = TheDefaultWords,
	loading = undefined as Promise<IWorkTrackingSystemConnection[]> | undefined,
	answers = undefined as Answers | undefined,
}: {
	connection?: IWorkTrackingSystemConnection;
	words?: Words;
	loading?: Promise<IWorkTrackingSystemConnection[]>;
	answers?: Answers;
} = {}) => {
	const server = createMockWorkTrackingSystemService();
	const queued = [...(answers ?? [loading ?? Promise.resolve([connection])])];
	vi.mocked(server.getWorkTrackingSystems).mockResolvedValue([connection]);
	vi.mocked(server.getConfiguredWorkTrackingSystems).mockImplementation(() =>
		queued.length > 1 ? (queued.shift() as Answers[number]) : queued[0],
	);
	vi.mocked(server.validateWorkTrackingSystemConnection).mockResolvedValue({
		isValid: true,
	});
	vi.mocked(server.updateWorkTrackingSystemConnection).mockImplementation(
		async (saved) => saved,
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

	const thePage = (
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
			<ThemeProvider theme={NoAnimations}>
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
			</ThemeProvider>
		</QueryClientProvider>
	);
	const { rerender } = render(thePage);
	reopenThePage = () => rerender(thePage);

	if (loading === undefined && answers === undefined) {
		await screen.findByDisplayValue(JiraDcPayments);
	}

	if (words.team !== TheDefaultWords.team) {
		await waitFor(() =>
			expect(terminology.getAllTerminology).toHaveBeenCalled(),
		);
	}

	return server;
};

// --- Where things are on the page ---

const theSection = (name = TheSection) => screen.getByRole("region", { name });

const theGroup = (name: string, section = theSection()) =>
	within(section).getByRole("group", { name });

const expectTheRows = (group: HTMLElement, settings: string[]) => {
	expect(within(group).getAllByRole("combobox")).toHaveLength(settings.length);
	for (const setting of settings) {
		expect(
			within(group).getByRole("combobox", { name: setting }),
		).toBeInTheDocument();
	}
};

const theQuestion = (title: string) =>
	screen.findByRole("dialog", { name: title });

// --- What Priya does ---

const whenPriyaPicks = async (
	group: string,
	setting: string,
	field: string,
) => {
	fireEvent.mouseDown(
		within(theGroup(group)).getByRole("combobox", { name: setting }),
	);
	fireEvent.click(
		within(await screen.findByRole("listbox")).getByRole("option", {
			name: field,
		}),
	);
};

// Adding a field to the Teams' list changes the default whatever it held before.
const whenPriyaSetsTheTeamsParentOverrideDefault = (field: string) =>
	whenPriyaPicks("Teams", "Add a source", field);

/** Priya follows a link from one connection's page to another's, as the Overview's connection list offers. */
const whenPriyaOpensTheConnection = (id: number) => {
	route.id = `${id}`;
	reopenThePage?.();
};

const whenPriyaPressesSave = () =>
	fireEvent.click(screen.getByRole("button", { name: /^Save$/ }));

const whenSheAnswers = async (title: string, answer: string) => {
	const question = await theQuestion(title);
	fireEvent.click(within(question).getByRole("button", { name: answer }));
};

// --- What she sees, and what is sent ---

const theSave = (
	server: ReturnType<typeof createMockWorkTrackingSystemService>,
) => {
	const [saved, choice] = vi.mocked(server.updateWorkTrackingSystemConnection)
		.mock.calls[0];
	return {
		defaults: (saved as unknown as { defaults?: Record<string, unknown> })
			.defaults,
		choice,
	};
};

const theSaveIsSent = (
	server: ReturnType<typeof createMockWorkTrackingSystemService>,
) =>
	waitFor(() =>
		expect(server.updateWorkTrackingSystemConnection).toHaveBeenCalledTimes(1),
	);

const theFieldsChangedEventsSent = () =>
	reportUsage.mock.calls
		.map(([use]) => use as { name: string })
		.filter((use) => use.name === "WorkTrackingSystemFieldsChanged");

describe("defaults set once on the connection", () => {
	beforeEach(() => {
		vi.clearAllMocks();
		route.id = "7";
	});

	describe("the section on the connection page", () => {
		it("a Jira connection shows a Defaults for Teams and Portfolios section with the settings Jira honours for each", async () => {
			await givenPriyaOpensJiraDcPayments();

			const section = theSection();
			expect(within(section).getByText(TheIntro)).toBeInTheDocument();
			expectTheRows(theGroup("Teams"), ["Add a source", "Estimation Field"]);
			expect(
				within(theGroup("Teams")).getByText("Parent sources"),
			).toBeInTheDocument();
			expectTheRows(theGroup("Portfolios"), [
				"Add a source",
				"Dependency Field",
				"Size Estimate Field",
				"Feature Owner Field",
				"Estimation Field",
			]);
		});

		it("a CSV connection offers only the Feature Owner and Estimation defaults", async () => {
			await givenPriyaOpensJiraDcPayments({
				connection: jiraDcPayments({ tracker: "Csv" }),
			});

			expectTheRows(theGroup("Teams"), ["Estimation Field"]);
			expectTheRows(theGroup("Portfolios"), [
				"Feature Owner Field",
				"Estimation Field",
			]);
		});

		it.each(["Linear", "ServiceNow"] as const)(
			"a %s connection shows no defaults section",
			async (tracker) => {
				await givenPriyaOpensJiraDcPayments({
					connection: jiraDcPayments({ tracker }),
				});

				expect(
					screen.queryByRole("region", { name: TheSection }),
				).not.toBeInTheDocument();
			},
		);

		it("the section speaks in the words this instance uses", async () => {
			await givenPriyaOpensJiraDcPayments({ words: SquadsProgramsAndEpics });

			const section = await screen.findByRole("region", {
				name: "Defaults for Squads and Programs",
			});
			expect(
				within(section).getByText(
					"Squads and Programs on this connection use these unless they set their own.",
				),
			).toBeInTheDocument();
			expect(
				within(theGroup("Programs", section)).getByRole("combobox", {
					name: "Epic Owner Field",
				}),
			).toBeInTheDocument();
		});

		it("a connection that offers only Teams defaults shows no Portfolios group", async () => {
			await givenPriyaOpensJiraDcPayments({
				connection: Object.assign(jiraDcPayments(), {
					supportedDefaults: ["parentOverrideForTeams", "estimationForTeams"],
				}),
			});

			expectTheRows(theGroup("Teams"), ["Add a source", "Estimation Field"]);
			expect(
				within(theSection()).queryByRole("group", { name: "Portfolios" }),
			).not.toBeInTheDocument();
		});

		it.each([
			["no list", {}, "Parent (Jira's own)"],
			[
				"a field the connection does not list",
				{
					parentSourcesForTeams: [
						{ kind: "AdditionalField", additionalFieldDefinitionId: 99 },
					],
				},
				"Unknown field (id 99)",
			],
		] as const)(
			"a default with %s lists just that source",
			async (_, defaults, source) => {
				await givenPriyaOpensJiraDcPayments({
					connection: jiraDcPayments({
						defaults: defaults as unknown as IConnectionDefaults,
					}),
				});

				expect(within(theGroup("Teams")).getAllByRole("listitem")).toHaveLength(
					1,
				);
				expect(within(theGroup("Teams")).getByText(source)).toBeInTheDocument();
			},
		);

		it("Try again loads the connection that could not be loaded, with its section", async () => {
			await givenPriyaOpensJiraDcPayments({
				answers: [
					Promise.reject(new Error("The server could not be reached")),
					Promise.resolve([jiraDcPayments()]),
				],
			});

			fireEvent.click(
				await screen.findByRole("button", { name: /^Try again$/ }),
			);

			expect(
				await screen.findByRole("region", { name: TheSection }),
			).toBeInTheDocument();
			expect(screen.queryByText(TheCouldNotLoadLook)).not.toBeInTheDocument();
		});

		it("the page never says how many follow a default", async () => {
			await givenPriyaOpensJiraDcPayments();

			expect(
				within(theSection()).queryByText(/follow/i),
			).not.toBeInTheDocument();
			expect(within(theSection()).queryByText(/\b3\b/)).not.toBeInTheDocument();
		});

		it("while the connection is on its way the page shows its loading look, and the section once it arrives", async () => {
			let answer: (connections: IWorkTrackingSystemConnection[]) => void =
				() => {};
			await givenPriyaOpensJiraDcPayments({
				loading: new Promise((resolve) => {
					answer = resolve;
				}),
			});

			expect(await screen.findByRole("progressbar")).toBeInTheDocument();
			expect(
				screen.queryByRole("region", { name: TheSection }),
			).not.toBeInTheDocument();
			expect(screen.queryByText("Parent sources")).not.toBeInTheDocument();

			answer([jiraDcPayments()]);

			expect(
				await screen.findByRole("region", { name: TheSection }),
			).toBeInTheDocument();
			expect(screen.queryByRole("progressbar")).not.toBeInTheDocument();
		});

		it("an answer for the connection Priya has left is dropped, and the one she opened shows its own defaults", async () => {
			let answerForPayments: (
				connections: IWorkTrackingSystemConnection[],
			) => void = () => {};
			const payments = jiraDcPayments({
				defaults: { parentSourcesForTeams: sourcesOf(11) },
			});
			const treasury = jiraDcPayments({
				id: 8,
				name: JiraDcTreasury,
				defaults: { parentSourcesForTeams: sourcesOf(12) },
			});
			await givenPriyaOpensJiraDcPayments({
				answers: [
					new Promise((resolve) => {
						answerForPayments = resolve;
					}),
					Promise.resolve([payments, treasury]),
				],
			});

			whenPriyaOpensTheConnection(8);
			await screen.findByDisplayValue(JiraDcTreasury);
			answerForPayments([payments, treasury]);

			await waitFor(() =>
				expect(
					within(theGroup("Teams")).getByText(ParentLink),
				).toBeInTheDocument(),
			);
			expect(screen.getByDisplayValue(JiraDcTreasury)).toBeInTheDocument();
			expect(
				screen.queryByDisplayValue(JiraDcPayments),
			).not.toBeInTheDocument();
		});

		it("when the connection cannot be loaded the page says so, and no section pretends it has no defaults", async () => {
			await givenPriyaOpensJiraDcPayments({
				loading: Promise.reject(new Error("The server could not be reached")),
			});

			expect(await screen.findByText(TheCouldNotLoadLook)).toBeInTheDocument();
			expect(
				screen.getByRole("button", { name: /^Try again$/ }),
			).toBeInTheDocument();
			expect(
				screen.queryByRole("region", { name: TheSection }),
			).not.toBeInTheDocument();
			expect(screen.queryByText("Parent sources")).not.toBeInTheDocument();
		});
	});

	describe("saving a changed default", () => {
		it("a save that changes neither a default nor a field sends the defaults as they were and asks nothing", async () => {
			const server = await givenPriyaOpensJiraDcPayments({
				connection: jiraDcPayments({
					defaults: {
						parentOverrideForTeams: 11,
						parentSourcesForTeams: sourcesOf(11),
					},
				}),
			});

			whenPriyaPressesSave();

			await theSaveIsSent(server);
			expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
			expect(theSave(server).defaults).toEqual({
				parentSourcesForTeams: sourcesOf(11),
			});
		});

		it("removing the field an Estimation default names clears that default in the save, and leaves the others as they were", async () => {
			const server = await givenPriyaOpensJiraDcPayments({
				connection: jiraDcPayments({
					defaults: {
						parentSourcesForTeams: sourcesOf(11),
						estimationForTeams: StoryPointsInPoints,
					},
				}),
			});

			const storyPoints = screen
				.getByText(StoryPoints, { selector: ".MuiListItemText-primary" })
				.closest("li") as HTMLElement;
			fireEvent.click(
				within(storyPoints).getByRole("button", { name: "delete" }),
			);
			whenPriyaPressesSave();
			const question = await screen.findByRole("dialog");
			fireEvent.click(
				within(question).getByRole("button", { name: "Save only" }),
			);

			await theSaveIsSent(server);
			expect(theSave(server).defaults).toEqual({
				parentSourcesForTeams: sourcesOf(11),
				estimationForTeams: null,
			});
		});

		it("removing a field a Parent sources list names leaves the list as it stands, so the refusal names the default", async () => {
			const server = await givenPriyaOpensJiraDcPayments({
				connection: jiraDcPayments({
					defaults: { parentSourcesForTeams: sourcesOf(11, 12) },
				}),
			});
			const refusal =
				'"Caused by" cannot be removed. It is the Teams\' Parent sources default. Nothing was saved.';
			vi.mocked(server.updateWorkTrackingSystemConnection).mockRejectedValue(
				new ApiError(
					409,
					refusal,
					undefined,
					undefined,
					"additional-field-in-use",
				),
			);

			const causedBy = screen
				.getByText(CausedBy, { selector: ".MuiListItemText-primary" })
				.closest("li") as HTMLElement;
			fireEvent.click(within(causedBy).getByRole("button", { name: "delete" }));

			expect(
				within(theGroup("Teams")).getByText("Unknown field (id 11)"),
			).toBeInTheDocument();
			expect(within(theGroup("Teams")).getByText(ParentLink)).toBeInTheDocument();

			whenPriyaPressesSave();
			await whenSheAnswers(TheFieldsQuestion, "Save only");

			await theSaveIsSent(server);
			expect(theSave(server).defaults).toEqual({
				parentSourcesForTeams: sourcesOf(11, 12),
			});
			expect(await screen.findByText(refusal)).toBeInTheDocument();
		});

		it("renaming the connection sends the Portfolios' default it was loaded with", async () => {
			const asTheServerSendsIt = JSON.parse(
				JSON.stringify(
					jiraDcPayments({
						defaults: { parentSourcesForPortfolios: sourcesOf(12) },
					}),
				),
			);
			const realService = new WorkTrackingSystemService();
			const put = vi.fn(async (_path: string, body: unknown) => ({
				data: body,
			}));
			Object.assign(realService, {
				apiService: {
					get: async () => ({ data: [asTheServerSendsIt] }),
					put,
				},
			});
			const server = await givenPriyaOpensJiraDcPayments({
				answers: [realService.getConfiguredWorkTrackingSystems()],
			});
			vi.mocked(server.updateWorkTrackingSystemConnection).mockImplementation(
				(saved, choice) =>
					realService.updateWorkTrackingSystemConnection(saved, choice),
			);
			const theName = await screen.findByDisplayValue(JiraDcPayments);

			fireEvent.change(theName, { target: { value: JiraDcTreasury } });
			whenPriyaPressesSave();

			await waitFor(() => expect(put).toHaveBeenCalledTimes(1));
			const [, body] = put.mock.calls[0];
			expect((body as { defaults?: unknown }).defaults).toEqual({
				parentSourcesForPortfolios: sourcesOf(12),
			});
		});
	});

	describe("the save dialog for a changed default that is read when updating", () => {
		it("asks whether to update the 3 Teams that follow it, and Save and update says so", async () => {
			const server = await givenPriyaOpensJiraDcPayments({
				connection: jiraDcPayments({
					defaults: { parentSourcesForTeams: sourcesOf(11) },
				}),
			});

			await whenPriyaSetsTheTeamsParentOverrideDefault(ParentLink);
			whenPriyaPressesSave();

			const question = await theQuestion(TheUpdateQuestion);
			expect(question).toHaveTextContent(TheUpdateQuestionSays);
			expect(
				within(question)
					.getAllByRole("button")
					.map((button) => button.textContent),
			).toEqual(["Cancel", "Save only", "Save and update"]);

			await whenSheAnswers(TheUpdateQuestion, "Save and update");
			await theSaveIsSent(server);
			expect(theSave(server).defaults).toEqual({
				parentSourcesForTeams: sourcesOf(11, 12),
			});
			expect(theSave(server).choice).toEqual({
				startUpdates: true,
				additionalFieldsChanged: false,
			});
		});

		it("Save only sends the default without starting any update", async () => {
			const server = await givenPriyaOpensJiraDcPayments();

			await whenPriyaSetsTheTeamsParentOverrideDefault(ParentLink);
			whenPriyaPressesSave();
			await whenSheAnswers(TheUpdateQuestion, "Save only");

			await theSaveIsSent(server);
			expect(theSave(server).choice).toEqual({
				startUpdates: false,
				additionalFieldsChanged: false,
			});
		});

		it.each([
			[
				"featureOwnerFieldForPortfolios",
				"Feature Owner Field",
				"Features' owners",
			],
		] as const)(
			"a changed %s default 2 Portfolios follow asks whether to update them",
			async (name, setting, whatChanges) => {
				await givenPriyaOpensJiraDcPayments({
					connection: jiraDcPayments({ followers: { [name]: 2 } }),
				});

				await whenPriyaPicks("Portfolios", setting, ParentLink);
				whenPriyaPressesSave();

				const question = await theQuestion("Update 0 Teams and 2 Portfolios?");
				expect(question).toHaveTextContent(
					`2 Portfolios follow the ${setting} default. Their ${whatChanges} change only once they have been updated.`,
				);
			},
		);

		it("changing a default clears the refusal the last save got", async () => {
			const server = await givenPriyaOpensJiraDcPayments();
			vi.mocked(
				server.validateWorkTrackingSystemConnection,
			).mockResolvedValueOnce({
				isValid: false,
				message: "The Url cannot be reached.",
			});
			whenPriyaPressesSave();
			expect(
				await screen.findByText("The Url cannot be reached."),
			).toBeInTheDocument();

			await whenPriyaSetsTheTeamsParentOverrideDefault(ParentLink);

			await waitFor(() =>
				expect(
					screen.queryByText("The Url cannot be reached."),
				).not.toBeInTheDocument(),
			);
		});

		it("the dialog never names who follows", async () => {
			await givenPriyaOpensJiraDcPayments();

			await whenPriyaSetsTheTeamsParentOverrideDefault(CausedBy);
			whenPriyaPressesSave();

			const question = await theQuestion(TheUpdateQuestion);
			expect(question).not.toHaveTextContent(
				/Fraud Signals|Ops Tooling|Mobile Wallet/,
			);
		});

		it("a default nobody follows saves without asking anything", async () => {
			const server = await givenPriyaOpensJiraDcPayments({
				connection: jiraDcPayments({ followers: {} }),
			});

			await whenPriyaSetsTheTeamsParentOverrideDefault(CausedBy);
			whenPriyaPressesSave();

			await theSaveIsSent(server);
			expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
			expect(theSave(server).choice).toBeUndefined();
		});

		it("a followed Estimation default left as it was asks nothing when another default changes", async () => {
			const server = await givenPriyaOpensJiraDcPayments({
				connection: jiraDcPayments({
					followers: { estimationForTeams: 4 },
					defaults: { estimationForTeams: StoryPointsInPoints },
				}),
			});

			await whenPriyaSetsTheTeamsParentOverrideDefault(CausedBy);
			whenPriyaPressesSave();

			await theSaveIsSent(server);
			expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
		});

		it("Cancel saves nothing and keeps Priya's choice on the page", async () => {
			const server = await givenPriyaOpensJiraDcPayments();

			await whenPriyaSetsTheTeamsParentOverrideDefault(CausedBy);
			whenPriyaPressesSave();
			await whenSheAnswers(TheUpdateQuestion, "Cancel");

			await waitFor(() =>
				expect(screen.queryByRole("dialog")).not.toBeInTheDocument(),
			);
			expect(server.updateWorkTrackingSystemConnection).not.toHaveBeenCalled();
			expect(within(theGroup("Teams")).getByText(CausedBy)).toBeInTheDocument();
		});

		// A changed Additional Field updates everyone on the connection, which already covers whoever
		// follows the changed default, so one save asks one question: the Additional Fields one.
		it("an Additional Field and a followed default changed in one save ask only the Additional Fields question", async () => {
			const server = await givenPriyaOpensJiraDcPayments({
				connection: jiraDcPayments({
					defaults: { parentSourcesForTeams: sourcesOf(11) },
				}),
			});

			await whenPriyaSetsTheTeamsParentOverrideDefault(ParentLink);
			const storyPoints = screen
				.getByText(StoryPoints, { selector: ".MuiListItemText-primary" })
				.closest("li") as HTMLElement;
			fireEvent.click(
				within(storyPoints).getByRole("button", { name: "delete" }),
			);
			whenPriyaPressesSave();

			const question = await theQuestion(TheFieldsQuestion);
			expect(question).toHaveTextContent(TheFieldsQuestionSays);
			expect(screen.getAllByRole("dialog")).toHaveLength(1);

			await whenSheAnswers(TheFieldsQuestion, "Save and update");
			await theSaveIsSent(server);
			expect(theSave(server).choice).toEqual({
				startUpdates: true,
				additionalFieldsChanged: true,
			});
			await waitFor(() => expect(mockNavigate).toHaveBeenCalledWith("/"));
			expect(theFieldsChangedEventsSent()).toHaveLength(1);
		});

		// A default is not an Additional Field: counting it as a field change would report that fields
		// changed when none did.
		it("answering the question about a default does not report that Additional Fields changed", async () => {
			const server = await givenPriyaOpensJiraDcPayments();

			await whenPriyaSetsTheTeamsParentOverrideDefault(ParentLink);
			whenPriyaPressesSave();
			await whenSheAnswers(TheUpdateQuestion, "Save and update");

			await theSaveIsSent(server);
			await waitFor(() => expect(mockNavigate).toHaveBeenCalledWith("/"));
			expect(theFieldsChangedEventsSent()).toEqual([]);
		});
	});

	describe("the usage event for a default of several parent sources", () => {
		const theParentSourcesEventsSent = () =>
			reportUsage.mock.calls
				.map(([use]) => use as { name: string })
				.filter((use) => use.name === "ParentSourcesSavedWithSeveralEntries");

		it("a changed default of two entries sends the event once, after the server accepts", async () => {
			const server = await givenPriyaOpensJiraDcPayments({
				connection: jiraDcPayments({
					defaults: { parentSourcesForTeams: sourcesOf(11) },
				}),
			});

			await whenPriyaSetsTheTeamsParentOverrideDefault(ParentLink);
			whenPriyaPressesSave();
			await whenSheAnswers(TheUpdateQuestion, "Save only");

			await theSaveIsSent(server);
			await waitFor(() => expect(mockNavigate).toHaveBeenCalledWith("/"));
			expect(theParentSourcesEventsSent()).toEqual([
				{ name: "ParentSourcesSavedWithSeveralEntries" },
			]);
		});

		it("a default of two entries that is saved as it was sends nothing", async () => {
			const server = await givenPriyaOpensJiraDcPayments({
				connection: jiraDcPayments({
					defaults: { parentSourcesForTeams: sourcesOf(11, 12) },
				}),
			});

			whenPriyaPressesSave();

			await theSaveIsSent(server);
			await waitFor(() => expect(mockNavigate).toHaveBeenCalledWith("/"));
			expect(theParentSourcesEventsSent()).toEqual([]);
		});
	});

	describe("the Estimation default", () => {
		it("a changed Estimation default 4 Teams follow asks only to save, because nothing needs updating", async () => {
			const server = await givenPriyaOpensJiraDcPayments({
				connection: jiraDcPayments({ followers: { estimationForTeams: 4 } }),
			});

			await whenPriyaPicks("Teams", "Estimation Field", StoryPoints);
			fireEvent.change(
				within(theGroup("Teams")).getByLabelText("Estimation Unit"),
				{ target: { value: "Points" } },
			);
			whenPriyaPressesSave();

			const question = await theQuestion(TheEstimationQuestion);
			expect(question).toHaveTextContent(TheEstimationQuestionSays);
			expect(
				within(question)
					.getAllByRole("button")
					.map((button) => button.textContent),
			).toEqual(["Cancel", "Save"]);

			await whenSheAnswers(TheEstimationQuestion, "Save");
			await theSaveIsSent(server);
			expect(theSave(server).defaults).toMatchObject({
				estimationForTeams: {
					additionalFieldDefinitionId: 13,
					unit: "Points",
				},
			});
			expect(theSave(server).choice).toEqual({
				startUpdates: true,
				additionalFieldsChanged: false,
			});
		});

		it("a save that also changes a default read when updating asks only the update question, and still sends the Estimation default", async () => {
			const server = await givenPriyaOpensJiraDcPayments({
				connection: jiraDcPayments({
					followers: { parentOverrideForTeams: 3, estimationForTeams: 4 },
				}),
			});

			await whenPriyaSetsTheTeamsParentOverrideDefault(ParentLink);
			await whenPriyaPicks("Teams", "Estimation Field", StoryPoints);
			fireEvent.change(
				within(theGroup("Teams")).getByLabelText("Estimation Unit"),
				{ target: { value: "Points" } },
			);
			whenPriyaPressesSave();

			const question = await theQuestion(TheUpdateQuestion);
			expect(question).toHaveTextContent(TheUpdateQuestionSays);
			expect(
				screen.queryByRole("dialog", { name: TheEstimationQuestion }),
			).not.toBeInTheDocument();

			await whenSheAnswers(TheUpdateQuestion, "Save only");
			await theSaveIsSent(server);
			expect(theSave(server).defaults).toMatchObject({
				parentSourcesForTeams: [{ kind: "TrackerParent" }, ...sourcesOf(12)],
				estimationForTeams: {
					additionalFieldDefinitionId: 13,
					unit: "Points",
				},
			});
		});

		it("the Estimation default stays collapsed to its field until a field is chosen", async () => {
			await givenPriyaOpensJiraDcPayments();

			expect(
				within(theGroup("Teams")).queryByLabelText("Estimation Unit"),
			).not.toBeInTheDocument();
		});
	});
});
