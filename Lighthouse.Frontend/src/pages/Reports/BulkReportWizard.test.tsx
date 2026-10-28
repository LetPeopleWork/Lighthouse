import { screen, waitFor, within } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
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
	NOT_ADMIN_OF_ANY,
	NOT_CONFIRMED_CREATED,
	neverAnswered,
	notCreated,
	notCreatedBecause,
	OPEN,
	OPTIONS_COULD_NOT_LOAD,
	OWNERS_AT_MOST_50,
	type Owner,
	PREMIUM_LICENSE_TOOLTIP,
	PULSAR,
	pendingEpic6350,
	pickOwner,
	QUARTER,
	RETRY,
	renderTheReportsPage,
	rowOf,
	SELECT_ALL,
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

const pending = pendingEpic6350("02a");

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
	// @walking_skeleton @driving_port @us-02a @slice-02a @kpi-OUT-6350-bulk-creation-used @contract-shape:bounded-change
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

	// @driving_port @us-02a @slice-02a @contract-shape:bounded-change
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
	// @us-02a @slice-02a @contract-shape:pure-function
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

	// @us-02a @slice-02a @edge @contract-shape:unbounded-preservation
	it.skip(`while the run is under way the result says so and cannot be closed (${pending})`, async () => {
		const { user } = renderTheReportsPage({
			reportService: aReportServiceCreating(vi.fn(() => neverAnswered())),
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

	// @us-02a @slice-02a @error @kpi-OUT-6350-G-per-owner-outcome-and-rbac @contract-shape:bounded-change
	it.skip(`a Team refused is named with its reason while the others are created and can be opened (${pending})`, async () => {
		const createReportsInBulk = vi
			.fn()
			.mockResolvedValue([
				created(ZENITH, 601),
				created(GRAVITY, 602),
				notCreated(PULSAR, "not-admin"),
			]);
		const { user } = renderTheReportsPage({
			reportService: aReportServiceCreating(createReportsInBulk),
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

	// @us-02a @slice-02a @error @contract-shape:bounded-change
	it.skip(`when every Team is refused the headline says none was created and each line says why (${pending})`, async () => {
		const createReportsInBulk = vi
			.fn()
			.mockResolvedValue([
				notCreated(ZENITH, "not-admin"),
				notCreated(GRAVITY, "owner-not-found"),
				notCreated(PULSAR, "not-saved"),
			]);
		const { user } = renderTheReportsPage({
			reportService: aReportServiceCreating(createReportsInBulk),
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

	// @us-02a @slice-02a @error @contract-shape:pure-function
	it.skip.each([
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
	])(
		`a Team refused for %s gets its own line (${pending})`,
		async (_why, outcome, line) => {
			const { user } = renderTheReportsPage({
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
			await pickOwner(user, outcome.ownerId === PULSAR.id ? PULSAR : EQUINOX);
			await user.click(screen.getByRole("button", { name: createCount(2) }));

			expect(await theLineSaying(line)).toBeInTheDocument();
		},
	);

	// @us-02a @slice-02a @error @contract-shape:unbounded-preservation
	it.skip(`when no answer arrives it says it cannot confirm what was created and leads back to Reports (${pending})`, async () => {
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

	// @us-02a @slice-02a @edge @contract-shape:bounded-change
	it.skip(`creating the same report twice creates it twice, with no warning (${pending})`, async () => {
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

	// @us-02a @slice-02a @edge @contract-shape:pure-function
	it.skip(`with no report anywhere yet, the empty page also offers Create reports, on Premium and Community alike (${pending})`, async () => {
		renderTheReportsPage({ licence: "community" });

		expect(await screen.findByText(EMPTY, { exact: true })).toBeInTheDocument();
	});
});

describe("At most 50 Teams and Portfolios in one run", () => {
	// @us-02a @slice-02a @error @edge @contract-shape:pure-function
	it.skip(`Select all picks all 60 Teams, the button counts them, and Create says at most 50 (${pending})`, async () => {
		const { user } = renderTheReportsPage({
			reportService: offering(...sixtyTeams),
			permissions: { adminOf: sixtyTeams },
		});

		await toTheOwnerStep(user);
		await user.click(
			within(screen.getByRole("group", { name: TEAMS_GROUP })).getByRole(
				"button",
				{ name: SELECT_ALL },
			),
		);

		expect(screen.getAllByRole("checkbox", { checked: true })).toHaveLength(60);
		expect(
			screen.getByRole("button", { name: createCount(60) }),
		).toBeDisabled();
		expect(
			screen.getByText(OWNERS_AT_MOST_50, { exact: true }),
		).toBeInTheDocument();
	});

	// @us-02a @slice-02a @edge @contract-shape:pure-function
	it.skip(`exactly 50 picked may still be created, with no ceiling message (${pending})`, async () => {
		const fifty = sixtyTeams.slice(0, 50);
		const { user } = renderTheReportsPage({
			reportService: offering(...fifty),
			permissions: { adminOf: fifty },
		});

		await toTheOwnerStep(user);
		await user.click(screen.getByRole("button", { name: SELECT_ALL }));

		expect(screen.getByRole("button", { name: createCount(50) })).toBeEnabled();
		expect(screen.queryByText(OWNERS_AT_MOST_50)).not.toBeInTheDocument();
	});
});

describe("The wizard waits for what a report needs", () => {
	// @us-02b @slice-02a @edge @contract-shape:unbounded-preservation
	it.skip(`while it loads, Create reports spins and is disabled, and no wizard opens (${pending})`, async () => {
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

	// @driving_port @us-02b @slice-02a @contract-shape:pure-function
	it.skip(`once it has arrived the wizard opens at its first step, filled (${pending})`, async () => {
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

	// @us-02b @slice-02a @error @contract-shape:unbounded-preservation
	it.skip(`when it cannot be loaded the wizard says so, shows no step, and offers Cancel and Retry (${pending})`, async () => {
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

	// @us-02b @slice-02a @error @contract-shape:bounded-change
	it.skip(`Retry asks again and opens the first step once it arrives (${pending})`, async () => {
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

	// @us-02b @slice-02a @error @contract-shape:unbounded-preservation
	it.skip(`Cancel closes it and creates nothing (${pending})`, async () => {
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
	// @us-02a @slice-02a @error @contract-shape:pure-function
	it.skip(`on Community it is shown but disabled, with the Premium licence tooltip (${pending})`, async () => {
		const { user } = renderTheReportsPage({ licence: "community" });

		const button = await screen.findByRole("button", { name: CREATE_REPORTS });
		await waitFor(() => expect(button).toBeDisabled());
		await user.hover(button.parentElement ?? button);
		expect(await screen.findByRole("tooltip")).toHaveTextContent(
			PREMIUM_LICENSE_TOOLTIP,
		);
	});

	// @us-02b @slice-02a @error @contract-shape:pure-function
	it.skip(`for someone who is Admin of no Team or Portfolio it is disabled and says why (${pending})`, async () => {
		const { user } = renderTheReportsPage({ permissions: { adminOf: [] } });

		const button = await screen.findByRole("button", { name: CREATE_REPORTS });
		await waitFor(() => expect(button).toBeDisabled());
		await user.hover(button.parentElement ?? button);
		expect(await screen.findByRole("tooltip")).toHaveTextContent(
			new RegExp(`^${NOT_ADMIN_OF_ANY.replaceAll(".", "\\.")}$`),
		);
	});

	// @us-02b @slice-02a @edge @contract-shape:pure-function
	it.skip(`while permissions are on their way it is disabled (${pending})`, async () => {
		renderTheReportsPage({ permissions: "loading" });

		expect(
			await screen.findByRole("button", { name: CREATE_REPORTS }),
		).toBeDisabled();
	});

	// @us-02b @slice-02a @error @contract-shape:pure-function
	it.skip(`when permissions cannot be loaded it stays enabled, so the owner step can say what went wrong (${pending})`, async () => {
		renderTheReportsPage({ permissions: "failing" });

		await waitFor(async () =>
			expect(
				await screen.findByRole("button", { name: CREATE_REPORTS }),
			).toBeEnabled(),
		);
	});

	// @us-02a @slice-02a @contract-shape:pure-function
	it.skip(`where roles are not enforced anyone may press it on Premium (${pending})`, async () => {
		renderTheReportsPage({ permissions: "rolesNotEnforced" });

		await waitFor(async () =>
			expect(
				await screen.findByRole("button", { name: CREATE_REPORTS }),
			).toBeEnabled(),
		);
	});

	// @us-02a @slice-02a @contract-shape:pure-function
	it.skip(`an Admin of one Team on Premium may press it (${pending})`, async () => {
		renderTheReportsPage({ permissions: { adminOf: [LIGHTSPEED_TEAM] } });

		await waitFor(async () =>
			expect(
				await screen.findByRole("button", { name: CREATE_REPORTS }),
			).toBeEnabled(),
		);
	});
});
