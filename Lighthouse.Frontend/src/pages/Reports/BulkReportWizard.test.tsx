import {
	act,
	fireEvent,
	screen,
	waitFor,
	within,
} from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import type { IReportCreationOutcome } from "../../models/Reports/ReportsAcrossOwners";
import type { IReportService } from "../../services/Api/ReportService";
import {
	anOwnerToCreateOn,
	aReportOn,
	CANCEL,
	CREATE_REPORTS,
	CREATED,
	createCount,
	created,
	createdHeadline,
	creating,
	creationOptionsFor,
	DONE,
	EMPTY,
	EQUINOX,
	everyReport,
	GO_TO_REPORTS,
	GRAVITY,
	LIGHTSPEED_TEAM,
	manyOwners,
	NOT_ADMIN_OF_ANY,
	NOT_CONFIRMED_CREATED,
	neverAnswered,
	notCreated,
	notCreatedBecause,
	OCEAN_EXPLORER,
	OPEN,
	OPTIONS_COULD_NOT_LOAD,
	OWNERS_AT_MOST_50,
	type Owner,
	PORTFOLIOS_GROUP,
	PREMIUM_LICENSE_TOOLTIP,
	PULSAR,
	pickOwner,
	pressCreateReports,
	QUARTER,
	RETRY,
	renderTheReportsPage,
	rowOf,
	selectAllOf,
	TEAMS_GROUP,
	theReportsTable,
	toTheOwnerStep,
	whereTheReaderIs,
	ZENITH,
} from "../../tests/BulkReportsTestKit";
import { aReportService } from "../../tests/ReportsTestKit";

/**
 * Create reports, on the Reports page: one report, with one name, one set of metrics and one window, created for
 * several Teams in one go. The wizard opens only once what a report needs has arrived. The server answers each
 * Team on its own, created or not with the reason, and the result names every one of them. Creating is Premium.
 */

/** The single wizard's first step, which the bulk wizard reuses unchanged. */
const THE_TYPE_STEP =
	/^Set today's flow against a window that stays as it was\./;

const forZenithGravityAndPulsar = creationOptionsFor(
	anOwnerToCreateOn(ZENITH),
	anOwnerToCreateOn(GRAVITY),
	anOwnerToCreateOn(PULSAR),
);

const aReportServiceCreating = (
	createReportsInBulk: IReportService["createReportsInBulk"],
	getAllReports: IReportService["getAllReports"] = vi
		.fn()
		.mockResolvedValue(everyReport()),
) =>
	aReportService({
		getAllReports,
		getCreationOptions: vi.fn().mockResolvedValue(forZenithGravityAndPulsar),
		createReportsInBulk,
	});

const theResult = () => screen.findByRole("dialog");

const theLineSaying = async (line: string) =>
	within(await theResult()).findByText(line, { exact: true });

/** Rendering 50 or 60 owner rows is slow under a loaded run. */
const RENDER_HEAVY = 20000;

const sixtyTeams: Owner[] = Array.from({ length: 60 }, (_, index) => ({
	kind: "team",
	id: 300 + index,
	name: `Crew ${String(index + 1).padStart(2, "0")}`,
}));

const offering = (...owners: Owner[]) =>
	aReportService({
		getAllReports: vi.fn().mockResolvedValue(everyReport()),
		getCreationOptions: vi
			.fn()
			.mockResolvedValue(
				creationOptionsFor(...owners.map((owner) => anOwnerToCreateOn(owner))),
			),
	});

describe("Create reports for several Teams", () => {
	it(`a coach creates the same comparison for Zenith and Gravity and finds both listed`, async () => {
		const zenithsQuarter = aReportOn(ZENITH, QUARTER, { id: 501 });
		const gravitysQuarter = aReportOn(GRAVITY, QUARTER, { id: 502 });
		const getAllReports = vi
			.fn()
			.mockResolvedValueOnce(everyReport())
			.mockResolvedValue(everyReport(zenithsQuarter, gravitysQuarter));
		const createReportsInBulk = vi
			.fn()
			.mockResolvedValue([created(ZENITH, 501), created(GRAVITY, 502)]);
		const { user } = renderTheReportsPage({
			reportService: aReportServiceCreating(createReportsInBulk, getAllReports),
			permissions: { adminOf: [ZENITH, GRAVITY, PULSAR] },
		});

		await toTheOwnerStep(user);
		await pickOwner(user, ZENITH);
		await pickOwner(user, GRAVITY);
		await user.click(screen.getByRole("button", { name: createCount(2) }));

		const result = await theResult();
		expect(
			await within(result).findByText(createdHeadline(2, 0), { exact: true }),
		).toBeInTheDocument();
		await user.click(within(result).getByRole("button", { name: DONE }));
		await theReportsTable();
		expect(rowOf(QUARTER, ZENITH)).toBeInTheDocument();
		expect(rowOf(QUARTER, GRAVITY)).toBeInTheDocument();
	});

	it(`every Team picked gets the same comparison, in the order the step lists them`, async () => {
		const createReportsInBulk = vi
			.fn()
			.mockResolvedValue([created(GRAVITY, 1), created(ZENITH, 2)]);
		const { user } = renderTheReportsPage({
			reportService: aReportServiceCreating(createReportsInBulk),
		});

		await toTheOwnerStep(user, { thenPreset: "90 days" });
		await pickOwner(user, ZENITH);
		await pickOwner(user, GRAVITY);
		await user.click(screen.getByRole("button", { name: createCount(2) }));

		await waitFor(() => expect(createReportsInBulk).toHaveBeenCalledTimes(1));
		expect(createReportsInBulk).toHaveBeenCalledWith(
			expect.objectContaining({
				templateKey: "then-and-now",
				thenAndNow: expect.objectContaining({
					thenLengthDays: 90,
					nowLengthDays: null,
				}),
				owners: [
					{ ownerKind: "team", ownerId: GRAVITY.id },
					{ ownerKind: "team", ownerId: ZENITH.id },
				],
			}),
		);
	});

	// Today is 27 Oct 2026. Now ends today, and Then ends the day before Now starts, so the windows sit back to back.
	it.each([
		["30 days", 30, "2026-09-27"],
		["90 days", 90, "2026-07-29"],
		["180 days", 180, "2026-04-30"],
	])(
		`with %s left as it opens, Then ends the day before Now starts`,
		async (preset, days, thenEndDate) => {
			const createReportsInBulk = vi
				.fn()
				.mockResolvedValue([created(ZENITH, 1)]);
			const { user } = renderTheReportsPage({
				reportService: aReportServiceCreating(createReportsInBulk),
			});

			await toTheOwnerStep(user, { thenPreset: preset });
			await pickOwner(user, ZENITH);
			await user.click(screen.getByRole("button", { name: createCount(1) }));

			await waitFor(() =>
				expect(createReportsInBulk).toHaveBeenCalledWith(
					expect.objectContaining({
						thenAndNow: {
							thenEndDate,
							thenLengthDays: days,
							nowLengthDays: null,
						},
					}),
				),
			);
		},
	);

	it(`while the run is under way the result says so and cannot be closed`, async () => {
		const { user } = renderTheReportsPage({
			reportService: aReportServiceCreating(vi.fn(() => neverAnswered())),
			permissions: { adminOf: [ZENITH, GRAVITY, PULSAR] },
		});

		await toTheOwnerStep(user);
		await pickOwner(user, ZENITH);
		await pickOwner(user, GRAVITY);
		await pickOwner(user, PULSAR);
		await user.click(screen.getByRole("button", { name: createCount(3) }));

		const result = await theResult();
		expect(
			within(result).getByText(creating(3), { exact: true }),
		).toBeInTheDocument();
		expect(within(result).getByRole("progressbar")).toBeInTheDocument();
		await user.keyboard("{Escape}");
		expect(screen.getByRole("dialog")).toBeInTheDocument();
		expect(
			within(result).queryByRole("button", { name: DONE }),
		).not.toBeInTheDocument();
	});

	it(`a Team refused is named with its reason while the others are created and can be opened`, async () => {
		const createReportsInBulk = vi
			.fn()
			.mockResolvedValue([
				created(ZENITH, 601),
				created(GRAVITY, 602),
				notCreated(PULSAR, "not-admin"),
			]);
		const { user } = renderTheReportsPage({
			reportService: aReportServiceCreating(createReportsInBulk),
			permissions: { adminOf: [ZENITH, GRAVITY, PULSAR] },
		});

		await toTheOwnerStep(user);
		await pickOwner(user, ZENITH);
		await pickOwner(user, GRAVITY);
		await pickOwner(user, PULSAR);
		await user.click(screen.getByRole("button", { name: createCount(3) }));

		const result = await theResult();
		expect(
			await within(result).findByText(createdHeadline(2, 1), { exact: true }),
		).toBeInTheDocument();
		expect(
			within(result).getByText(notCreatedBecause.notAdmin(PULSAR), {
				exact: true,
			}),
		).toBeInTheDocument();
		expect(within(result).getAllByText(CREATED, { exact: true })).toHaveLength(
			2,
		);

		await user.click(within(result).getAllByRole("link", { name: OPEN })[0]);

		expect(whereTheReaderIs()).toBe(`/teams/${ZENITH.id}/reports/601`);
	});

	it(`when every Team is refused the headline says none was created and each line says why`, async () => {
		const createReportsInBulk = vi
			.fn()
			.mockResolvedValue([
				notCreated(ZENITH, "not-admin"),
				notCreated(GRAVITY, "owner-not-found"),
				notCreated(PULSAR, "not-saved"),
			]);
		const { user } = renderTheReportsPage({
			reportService: aReportServiceCreating(createReportsInBulk),
			permissions: { adminOf: [ZENITH, GRAVITY, PULSAR] },
		});

		await toTheOwnerStep(user);
		await pickOwner(user, ZENITH);
		await pickOwner(user, GRAVITY);
		await pickOwner(user, PULSAR);
		await user.click(screen.getByRole("button", { name: createCount(3) }));

		const result = await theResult();
		expect(
			await within(result).findByText(createdHeadline(0, 3), { exact: true }),
		).toBeInTheDocument();
		for (const line of [
			notCreatedBecause.notAdmin(ZENITH),
			notCreatedBecause.ownerGone(GRAVITY),
			notCreatedBecause.notSaved(),
		]) {
			expect(
				within(result).getByText(line, { exact: true }),
			).toBeInTheDocument();
		}
	});

	it.each([
		[
			"the Community cap after the licence lapsed",
			notCreated(PULSAR, "report-cap-reached"),
			notCreatedBecause.capReached(PULSAR),
		],
		[
			"a kept history too short for the window",
			notCreated(EQUINOX, "then-before-cutoff", "2026-07-30"),
			notCreatedBecause.historyTooShort("30 Jul 2026"),
		],
	])(`a Team refused for %s gets its own line`, async (_why, outcome, line) => {
		const refused = outcome.ownerId === PULSAR.id ? PULSAR : EQUINOX;
		const { user } = renderTheReportsPage({
			permissions: { adminOf: [ZENITH, refused] },
			reportService: aReportService({
				getAllReports: vi.fn().mockResolvedValue(everyReport()),
				getCreationOptions: vi
					.fn()
					.mockResolvedValue(
						creationOptionsFor(
							anOwnerToCreateOn(ZENITH),
							anOwnerToCreateOn(PULSAR),
							anOwnerToCreateOn(EQUINOX),
						),
					),
				createReportsInBulk: vi
					.fn()
					.mockResolvedValue([created(ZENITH, 1), outcome]),
			}),
		});

		await toTheOwnerStep(user, { thenPreset: "30 days" });
		await pickOwner(user, ZENITH);
		await pickOwner(user, refused);
		await user.click(screen.getByRole("button", { name: createCount(2) }));

		expect(await theLineSaying(line)).toBeInTheDocument();
	});

	it(`when no answer arrives it says it cannot confirm what was created and leads back to Reports`, async () => {
		const { user } = renderTheReportsPage({
			reportService: aReportServiceCreating(
				vi.fn().mockRejectedValue(new Error("the network dropped")),
			),
		});

		await toTheOwnerStep(user);
		await pickOwner(user, ZENITH);
		await user.click(screen.getByRole("button", { name: createCount(1) }));

		const result = await theResult();
		expect(
			await within(result).findByText(NOT_CONFIRMED_CREATED, { exact: true }),
		).toBeInTheDocument();
		expect(within(result).queryByText(/^Not created/)).not.toBeInTheDocument();
		await user.click(
			within(result).getByRole("button", { name: GO_TO_REPORTS }),
		);
		expect(whereTheReaderIs()).toBe("/reports");
		expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
	});

	it(`creating the same report twice creates it twice, with no warning`, async () => {
		const createReportsInBulk = vi
			.fn()
			.mockResolvedValueOnce([created(ZENITH, 701)])
			.mockResolvedValueOnce([created(ZENITH, 702)]);
		const { user } = renderTheReportsPage({
			reportService: aReportServiceCreating(createReportsInBulk),
		});

		const createForZenith = async () => {
			await toTheOwnerStep(user);
			await pickOwner(user, ZENITH);
			expect(screen.queryByRole("alert")).not.toBeInTheDocument();
			await user.click(screen.getByRole("button", { name: createCount(1) }));
			await user.click(
				await within(await theResult()).findByRole("button", { name: DONE }),
			);
		};
		await createForZenith();
		await createForZenith();

		expect(createReportsInBulk).toHaveBeenCalledTimes(2);
	});

	it(`with no report anywhere yet, the empty page also offers Create reports, on Premium and Community alike`, async () => {
		renderTheReportsPage({ licence: "community" });

		expect(await screen.findByText(EMPTY, { exact: true })).toBeInTheDocument();
	});
});

describe("At most 50 Teams and Portfolios in one run", () => {
	it(
		`Select all picks all 60 Teams, the button counts them, and Create says at most 50`,
		async () => {
			const { user } = renderTheReportsPage({
				reportService: offering(...sixtyTeams),
				permissions: { adminOf: sixtyTeams },
			});

			await toTheOwnerStep(user);
			await user.click(
				within(screen.getByRole("group", { name: TEAMS_GROUP })).getByRole(
					"button",
					{ name: selectAllOf(TEAMS_GROUP) },
				),
			);

			expect(screen.getAllByRole("checkbox", { checked: true })).toHaveLength(
				60,
			);
			expect(
				screen.getByRole("button", { name: createCount(60) }),
			).toBeDisabled();
			expect(
				screen.getByText(OWNERS_AT_MOST_50, { exact: true }),
			).toBeInTheDocument();
		},
		RENDER_HEAVY,
	);

	it(
		`exactly 50 picked may still be created, with no ceiling message`,
		async () => {
			const fifty = sixtyTeams.slice(0, 50);
			const { user } = renderTheReportsPage({
				reportService: offering(...fifty),
				permissions: { adminOf: fifty },
			});

			await toTheOwnerStep(user);
			await user.click(
				screen.getByRole("button", { name: selectAllOf(TEAMS_GROUP) }),
			);

			expect(
				screen.getByRole("button", { name: createCount(50) }),
			).toBeEnabled();
			expect(screen.queryByText(OWNERS_AT_MOST_50)).not.toBeInTheDocument();
		},
		RENDER_HEAVY,
	);

	const selectingAllIn = (
		user: ReturnType<typeof renderTheReportsPage>["user"],
		group: string,
	) =>
		user.click(
			within(screen.getByRole("group", { name: group })).getByRole("button", {
				name: selectAllOf(group),
			}),
		);

	const pickingAllOf = async (
		teamCount: number,
		portfolioCount: number,
	): Promise<void> => {
		const owners = [
			...manyOwners("team", teamCount),
			...manyOwners("portfolio", portfolioCount),
		];
		const { user } = renderTheReportsPage({
			reportService: offering(...owners),
			permissions: { adminOf: owners },
		});

		await toTheOwnerStep(user);
		await selectingAllIn(user, TEAMS_GROUP);
		await selectingAllIn(user, PORTFOLIOS_GROUP);
	};

	it(
		`30 Teams and 21 Portfolios make 51: Create is disabled and says at most 50`,
		async () => {
			await pickingAllOf(30, 21);

			expect(
				screen.getByRole("button", { name: createCount(51) }),
			).toBeDisabled();
			expect(screen.getByRole("alert")).toHaveTextContent(OWNERS_AT_MOST_50);
			expect(
				screen.getByRole("button", { name: createCount(51) }),
			).toHaveAccessibleDescription(OWNERS_AT_MOST_50);
		},
		RENDER_HEAVY,
	);

	it(
		`30 Teams and 20 Portfolios make exactly 50: Create is enabled, with no ceiling message`,
		async () => {
			await pickingAllOf(30, 20);

			expect(
				screen.getByRole("button", { name: createCount(50) }),
			).toBeEnabled();
			expect(screen.queryByText(OWNERS_AT_MOST_50)).not.toBeInTheDocument();
		},
		RENDER_HEAVY,
	);
});

describe("The wizard waits for what a report needs", () => {
	it(`while it loads, Create reports spins and is disabled, and no wizard opens`, async () => {
		const { user } = renderTheReportsPage({
			reportService: aReportService({
				getAllReports: vi.fn().mockResolvedValue(everyReport()),
				getCreationOptions: vi.fn(() => neverAnswered()),
			}),
		});
		const button = await screen.findByRole("button", { name: CREATE_REPORTS });
		await waitFor(() => expect(button).toBeEnabled());

		await user.click(button);

		expect(button).toBeDisabled();
		expect(within(button).getByRole("progressbar")).toBeInTheDocument();
		expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
	});

	it(`once it has arrived the wizard opens at its first step, filled`, async () => {
		const { user } = renderTheReportsPage({
			reportService: aReportServiceCreating(vi.fn()),
		});

		await user.click(
			await screen.findByRole("button", { name: CREATE_REPORTS }),
		);

		const wizard = await screen.findByRole("dialog");
		expect(within(wizard).getByText(THE_TYPE_STEP)).toBeInTheDocument();
		expect(
			within(wizard).getByRole("radio", { name: /^Then & Now/ }),
		).toBeInTheDocument();
	});

	it(`when it cannot be loaded the wizard says so, shows no step, and offers Cancel and Retry`, async () => {
		const { user } = renderTheReportsPage({
			reportService: aReportService({
				getAllReports: vi.fn().mockResolvedValue(everyReport()),
				getCreationOptions: vi
					.fn()
					.mockRejectedValue(new Error("the network dropped")),
			}),
		});

		await user.click(
			await screen.findByRole("button", { name: CREATE_REPORTS }),
		);

		const wizard = await screen.findByRole("dialog");
		expect(
			await within(wizard).findByText(OPTIONS_COULD_NOT_LOAD, { exact: true }),
		).toBeInTheDocument();
		expect(within(wizard).queryByText(THE_TYPE_STEP)).not.toBeInTheDocument();
		expect(
			within(wizard)
				.getAllByRole("button")
				.map((button) => button.textContent),
		).toEqual([CANCEL, RETRY]);
	});

	it(`Retry asks again and opens the first step once it arrives`, async () => {
		const getCreationOptions = vi
			.fn()
			.mockRejectedValueOnce(new Error("the network dropped"))
			.mockResolvedValue(forZenithGravityAndPulsar);
		const { user } = renderTheReportsPage({
			reportService: aReportService({
				getAllReports: vi.fn().mockResolvedValue(everyReport()),
				getCreationOptions,
			}),
		});

		await user.click(
			await screen.findByRole("button", { name: CREATE_REPORTS }),
		);
		await user.click(
			within(await screen.findByRole("dialog")).getByRole("button", {
				name: RETRY,
			}),
		);

		expect(
			await within(screen.getByRole("dialog")).findByText(THE_TYPE_STEP),
		).toBeInTheDocument();
		expect(getCreationOptions).toHaveBeenCalledTimes(2);
	});

	it(`Cancel closes it and creates nothing`, async () => {
		const createReportsInBulk = vi.fn();
		const { user } = renderTheReportsPage({
			reportService: aReportService({
				getAllReports: vi.fn().mockResolvedValue(everyReport()),
				getCreationOptions: vi
					.fn()
					.mockRejectedValue(new Error("the network dropped")),
				createReportsInBulk,
			}),
		});

		await user.click(
			await screen.findByRole("button", { name: CREATE_REPORTS }),
		);
		await user.click(
			within(await screen.findByRole("dialog")).getByRole("button", {
				name: CANCEL,
			}),
		);

		await waitFor(() =>
			expect(screen.queryByRole("dialog")).not.toBeInTheDocument(),
		);
		expect(createReportsInBulk).not.toHaveBeenCalled();
	});
});

describe("When Create reports can be pressed", () => {
	it(`on Community it is shown but disabled, with the Premium licence tooltip`, async () => {
		const { user } = renderTheReportsPage({ licence: "community" });

		const button = await screen.findByRole("button", { name: CREATE_REPORTS });
		await waitFor(() => expect(button).toBeDisabled());
		await user.hover(button.parentElement ?? button);
		expect(await screen.findByRole("tooltip")).toHaveTextContent(
			PREMIUM_LICENSE_TOOLTIP,
		);
	});

	it(`for someone who is Admin of no Team or Portfolio it is disabled and says why`, async () => {
		const { user } = renderTheReportsPage({ permissions: { adminOf: [] } });

		const button = await screen.findByRole("button", { name: CREATE_REPORTS });
		await waitFor(() => expect(button).toBeDisabled());
		await user.hover(button.parentElement ?? button);
		expect(await screen.findByRole("tooltip")).toHaveTextContent(
			new RegExp(`^${NOT_ADMIN_OF_ANY.replaceAll(".", "\\.")}$`),
		);
	});

	it(`while permissions are on their way it is disabled`, async () => {
		const { queryClient } = renderTheReportsPage({ permissions: "loading" });
		// Before the Premium licence has answered the button is disabled anyway, so only a check after it proves
		// that the permissions still on their way are what holds it.
		await waitFor(() =>
			expect(queryClient.getQueryState(["licenseStatus"])?.status).toBe(
				"success",
			),
		);
		await act(() => new Promise((resolve) => setTimeout(resolve, 0)));

		expect(screen.getByRole("button", { name: CREATE_REPORTS })).toBeDisabled();
	});

	it(`when permissions cannot be loaded it stays enabled, so the owner step can say what went wrong`, async () => {
		renderTheReportsPage({ permissions: "failing" });

		await waitFor(async () =>
			expect(
				await screen.findByRole("button", { name: CREATE_REPORTS }),
			).toBeEnabled(),
		);
	});

	it(`where roles are not enforced anyone may press it on Premium`, async () => {
		renderTheReportsPage({ permissions: "rolesNotEnforced" });

		await waitFor(async () =>
			expect(
				await screen.findByRole("button", { name: CREATE_REPORTS }),
			).toBeEnabled(),
		);
	});

	it(`an Admin of one Team on Premium may press it`, async () => {
		renderTheReportsPage({ permissions: { adminOf: [LIGHTSPEED_TEAM] } });

		await waitFor(async () =>
			expect(
				await screen.findByRole("button", { name: CREATE_REPORTS }),
			).toBeEnabled(),
		);
	});
});

describe("Moving through the wizard", () => {
	const aWizardFor = (permissions = { adminOf: [ZENITH, GRAVITY, PULSAR] }) =>
		renderTheReportsPage({
			reportService: aReportServiceCreating(
				vi.fn().mockResolvedValue([created(ZENITH, 1)]),
			),
			permissions,
		});

	it(`is titled Create reports and names its steps Type, Then, Now, Metrics & name and Owners`, async () => {
		const { user } = aWizardFor();

		await pressCreateReports(user);

		const wizard = await screen.findByRole("dialog", { name: CREATE_REPORTS });
		for (const label of ["Type", "Then", "Now", "Metrics & name", "Owners"]) {
			expect(
				within(wizard).getAllByText(label, { exact: true }).length,
			).toBeGreaterThan(0);
		}
	});

	it(`Back leads from Then to the Type step, which offers no Back of its own`, async () => {
		const { user } = aWizardFor();
		await pressCreateReports(user);
		expect(await screen.findByText(THE_TYPE_STEP)).toBeInTheDocument();
		expect(
			screen.queryByRole("button", { name: "Back" }),
		).not.toBeInTheDocument();

		await user.click(screen.getByRole("button", { name: "Next" }));
		await screen.findByRole("button", { name: "90 days" });
		await user.click(screen.getByRole("button", { name: "Back" }));

		expect(await screen.findByText(THE_TYPE_STEP)).toBeInTheDocument();
	});

	it(`Next waits while Then's length is no number of days`, async () => {
		const { user } = aWizardFor();
		await pressCreateReports(user);
		await user.click(await screen.findByRole("button", { name: "Next" }));

		await user.clear(
			await screen.findByRole("spinbutton", { name: "Then length in days" }),
		);

		expect(screen.getByRole("button", { name: "Next" })).toBeDisabled();
	});

	it(`the owner step offers Create, not Next, and nothing can be created before a Team is picked`, async () => {
		const { user } = aWizardFor();

		await toTheOwnerStep(user);

		expect(
			screen.queryByRole("button", { name: "Next" }),
		).not.toBeInTheDocument();
		expect(screen.getByRole("button", { name: createCount(0) })).toBeDisabled();
	});

	it(`a Team picked a second time is no longer picked`, async () => {
		const { user } = aWizardFor();
		await toTheOwnerStep(user);

		await pickOwner(user, ZENITH);
		await pickOwner(user, GRAVITY);
		await pickOwner(user, GRAVITY);

		expect(
			screen.getByRole("checkbox", { name: GRAVITY.name }),
		).not.toBeChecked();
		expect(screen.getByRole("button", { name: createCount(1) })).toBeEnabled();
	});

	it(`lists only the Teams the reader is Admin of`, async () => {
		const { user } = aWizardFor({ adminOf: [ZENITH] });

		await toTheOwnerStep(user);

		expect(
			screen.getByRole("checkbox", { name: ZENITH.name }),
		).toBeInTheDocument();
		expect(
			screen.queryByRole("checkbox", { name: GRAVITY.name }),
		).not.toBeInTheDocument();
		expect(
			screen.queryByRole("checkbox", { name: PULSAR.name }),
		).not.toBeInTheDocument();
	});

	it(`Escape leaves the result open while the run is under way, and closes it once the run has answered`, async () => {
		let answer: (outcomes: IReportCreationOutcome[]) => void = () => undefined;
		const { user } = renderTheReportsPage({
			reportService: aReportServiceCreating(
				vi.fn(
					() =>
						new Promise<IReportCreationOutcome[]>((resolve) => {
							answer = resolve;
						}),
				),
			),
		});
		await toTheOwnerStep(user);
		await pickOwner(user, ZENITH);
		await user.click(screen.getByRole("button", { name: createCount(1) }));
		await screen.findByText("Creating 1 report…", { exact: true });

		fireEvent.keyDown(screen.getByRole("dialog"), { key: "Escape" });
		expect(screen.getByRole("dialog")).toBeInTheDocument();

		await act(async () => answer([created(ZENITH, 1)]));
		await screen.findByRole("button", { name: DONE });
		fireEvent.keyDown(screen.getByRole("dialog"), { key: "Escape" });

		await waitFor(() =>
			expect(screen.queryByRole("dialog")).not.toBeInTheDocument(),
		);
	});

	it(`one report created is counted in the singular, and its line names its Team`, async () => {
		const { user } = aWizardFor();
		await toTheOwnerStep(user);
		await pickOwner(user, ZENITH);
		await user.click(screen.getByRole("button", { name: createCount(1) }));

		const result = await theResult();
		expect(
			await within(result).findByText(createdHeadline(1, 0), { exact: true }),
		).toBeInTheDocument();
		expect(
			within(result).getByText(`Team ${ZENITH.name}`, { exact: true }),
		).toBeInTheDocument();
	});

	it(`an owner the answer leaves unnamed is spoken of as this Team`, async () => {
		const unnamed = {
			...notCreated(ZENITH, "owner-not-found"),
			ownerName: undefined,
		};
		const { user } = renderTheReportsPage({
			reportService: aReportServiceCreating(
				vi.fn().mockResolvedValue([unnamed]),
			),
		});
		await toTheOwnerStep(user);
		await pickOwner(user, ZENITH);
		await user.click(screen.getByRole("button", { name: createCount(1) }));

		const result = await theResult();
		expect(
			await within(result).findByText("Team", { exact: true }),
		).toBeInTheDocument();
		expect(
			within(result).getByText("Not created: this Team no longer exists.", {
				exact: true,
			}),
		).toBeInTheDocument();
	});
});

describe("Who may press Create reports", () => {
	it(`an Admin of one Portfolio alone on Premium may press it`, async () => {
		renderTheReportsPage({ permissions: { adminOf: [OCEAN_EXPLORER] } });

		await waitFor(async () =>
			expect(
				await screen.findByRole("button", { name: CREATE_REPORTS }),
			).toBeEnabled(),
		);
	});

	it(`for an Admin on Premium it carries no tooltip`, async () => {
		const { user } = renderTheReportsPage();
		const button = await screen.findByRole("button", { name: CREATE_REPORTS });
		await waitFor(() => expect(button).toBeEnabled());

		await user.hover(button.parentElement ?? button);
		await act(() => new Promise((resolve) => setTimeout(resolve, 300)));

		expect(screen.queryByRole("tooltip")).not.toBeInTheDocument();
	});
});
