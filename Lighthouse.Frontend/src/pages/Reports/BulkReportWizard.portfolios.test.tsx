import { screen, waitFor, within } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import {
	APOLLO_PORTFOLIO,
	anOwnerToCreateOn,
	createCount,
	created,
	creationOptionsFor,
	everyReport,
	GRAVITY,
	NEUROLINK_CITY,
	nextUntil,
	OCEAN_EXPLORER,
	PORTFOLIOS_GROUP,
	pickOwner,
	pressCreateReports,
	renderTheReportsPage,
	SAME_AS_THEN,
	selectAllOf,
	TEAMS_GROUP,
	THEN_PRESETS,
	toTheOwnerStep,
	ZENITH,
} from "../../tests/BulkReportsTestKit";
import {
	aReportService,
	nowNeedsAtLeast,
	thenNeedsAtLeast,
} from "../../tests/ReportsTestKit";

/**
 * Portfolios join the selection: one run may create the same comparison for Teams and Portfolios together. The
 * window is chosen the same way whatever mix is picked: Then of 30, 90 or 180 days or a custom number of days of
 * at least 14, and Now as long as Then unless the reader says otherwise.
 */

const teamsAndPortfolios = creationOptionsFor(
	anOwnerToCreateOn(ZENITH),
	anOwnerToCreateOn(GRAVITY),
	anOwnerToCreateOn(OCEAN_EXPLORER),
	anOwnerToCreateOn(NEUROLINK_CITY),
	anOwnerToCreateOn(APOLLO_PORTFOLIO),
);

const creatingWith = (createReportsInBulk = vi.fn().mockResolvedValue([])) =>
	aReportService({
		getAllReports: vi.fn().mockResolvedValue(everyReport()),
		getCreationOptions: vi.fn().mockResolvedValue(teamsAndPortfolios),
		createReportsInBulk,
	});

describe("Teams and Portfolios in one run", () => {
	it(`a coach creates one comparison for two Teams and Ocean Explorer in one go`, async () => {
		const createReportsInBulk = vi
			.fn()
			.mockResolvedValue([
				created(GRAVITY, 1),
				created(ZENITH, 2),
				created(OCEAN_EXPLORER, 3),
			]);
		const { user } = renderTheReportsPage({
			reportService: creatingWith(createReportsInBulk),
		});

		await toTheOwnerStep(user);
		await pickOwner(user, ZENITH);
		await pickOwner(user, GRAVITY);
		await pickOwner(user, OCEAN_EXPLORER);
		await user.click(screen.getByRole("button", { name: createCount(3) }));

		await waitFor(() =>
			expect(createReportsInBulk).toHaveBeenCalledWith(
				expect.objectContaining({
					owners: [
						{ ownerKind: "team", ownerId: GRAVITY.id },
						{ ownerKind: "team", ownerId: ZENITH.id },
						{ ownerKind: "portfolio", ownerId: OCEAN_EXPLORER.id },
					],
				}),
			),
		);
	});

	it(`Portfolios are listed after Teams, alphabetically, with their own Select all`, async () => {
		const { user } = renderTheReportsPage({ reportService: creatingWith() });

		await toTheOwnerStep(user);

		const groups = screen
			.getAllByRole("group")
			.map((group) => group.getAttribute("aria-label"));
		expect(groups).toEqual([TEAMS_GROUP, PORTFOLIOS_GROUP]);
		const portfolios = screen.getByRole("group", { name: PORTFOLIOS_GROUP });
		await user.click(
			within(portfolios).getByRole("button", {
				name: selectAllOf(PORTFOLIOS_GROUP),
			}),
		);
		expect(
			within(portfolios)
				.getAllByRole("checkbox", { checked: true })
				.map((box) => box.getAttribute("aria-label")),
		).toEqual([
			APOLLO_PORTFOLIO.name,
			NEUROLINK_CITY.name,
			OCEAN_EXPLORER.name,
		]);
		expect(screen.getByRole("button", { name: createCount(3) })).toBeEnabled();
	});

	it(`only Portfolios may be picked, with no Team at all`, async () => {
		const createReportsInBulk = vi.fn().mockResolvedValue([]);
		const { user } = renderTheReportsPage({
			reportService: creatingWith(createReportsInBulk),
		});

		await toTheOwnerStep(user);
		await pickOwner(user, OCEAN_EXPLORER);
		const teams = screen.getByRole("group", { name: TEAMS_GROUP });
		expect(within(teams).queryAllByRole("checkbox", { checked: true })).toEqual(
			[],
		);
		await user.click(screen.getByRole("button", { name: createCount(1) }));

		await waitFor(() =>
			expect(createReportsInBulk).toHaveBeenCalledWith(
				expect.objectContaining({
					owners: [{ ownerKind: "portfolio", ownerId: OCEAN_EXPLORER.id }],
				}),
			),
		);
	});

	it(`a Portfolio the reader is not Admin of is not listed and Select all leaves it out`, async () => {
		const createReportsInBulk = vi.fn().mockResolvedValue([]);
		const { user } = renderTheReportsPage({
			reportService: creatingWith(createReportsInBulk),
			permissions: {
				adminOf: [ZENITH, GRAVITY, OCEAN_EXPLORER, NEUROLINK_CITY],
			},
		});

		await toTheOwnerStep(user);
		expect(
			screen.queryByRole("checkbox", { name: APOLLO_PORTFOLIO.name }),
		).not.toBeInTheDocument();
		const portfolios = screen.getByRole("group", { name: PORTFOLIOS_GROUP });
		await user.click(
			within(portfolios).getByRole("button", {
				name: selectAllOf(PORTFOLIOS_GROUP),
			}),
		);
		await user.click(screen.getByRole("button", { name: createCount(2) }));

		await waitFor(() =>
			expect(createReportsInBulk).toHaveBeenCalledWith(
				expect.objectContaining({
					owners: [
						{ ownerKind: "portfolio", ownerId: NEUROLINK_CITY.id },
						{ ownerKind: "portfolio", ownerId: OCEAN_EXPLORER.id },
					],
				}),
			),
		);
	});
});

describe("Choosing the window for several owners", () => {
	const toTheThenStep = async (
		user: ReturnType<typeof renderTheReportsPage>["user"],
	) => {
		await pressCreateReports(user);
		await user.click(await screen.findByRole("button", { name: "Next" }));
	};

	it(`Then offers 30, 90 and 180 days or a custom length, and Now is as long as Then unless changed`, async () => {
		const { user } = renderTheReportsPage({ reportService: creatingWith() });

		await toTheThenStep(user);

		await screen.findByRole("button", { name: THEN_PRESETS[0] });
		expect(
			THEN_PRESETS.filter(
				(preset) => screen.queryByRole("button", { name: preset }) === null,
			),
		).toEqual([]);
		await user.click(screen.getByRole("button", { name: "90 days" }));
		await user.click(screen.getByRole("button", { name: "Next" }));
		expect(
			await screen.findByRole("radio", { name: SAME_AS_THEN }),
		).toBeChecked();
	});

	it(`a custom Then shorter than 14 days is refused before any owner is asked`, async () => {
		const { user } = renderTheReportsPage({ reportService: creatingWith() });

		await toTheThenStep(user);
		await user.click(await screen.findByRole("button", { name: "Custom" }));
		await user.type(screen.getByRole("spinbutton", { name: /^Then/ }), "13");

		expect(
			await screen.findByText(thenNeedsAtLeast(14), { exact: true }),
		).toBeInTheDocument();
		expect(screen.getByRole("button", { name: "Next" })).toBeDisabled();
	});

	const toTheNowStep = async (
		user: ReturnType<typeof renderTheReportsPage>["user"],
	) => {
		await toTheThenStep(user);
		await user.click(await screen.findByRole("button", { name: "30 days" }));
		await user.click(screen.getByRole("button", { name: "Next" }));
		await user.click(await screen.findByRole("radio", { name: "Custom" }));
	};

	it(`a custom Now shorter than 14 days is refused`, async () => {
		const { user } = renderTheReportsPage({ reportService: creatingWith() });

		await toTheNowStep(user);
		await user.type(screen.getByRole("spinbutton", { name: /^Now/ }), "13");

		expect(
			await screen.findByText(nowNeedsAtLeast(14), { exact: true }),
		).toBeInTheDocument();
		expect(screen.getByRole("button", { name: "Next" })).toBeDisabled();
	});

	it(`a custom Now left empty cannot be moved past`, async () => {
		const { user } = renderTheReportsPage({ reportService: creatingWith() });

		await toTheNowStep(user);

		expect(screen.getByRole("spinbutton", { name: /^Now/ })).toHaveValue(null);
		expect(screen.getByRole("button", { name: "Next" })).toBeDisabled();
	});

	it(`going back to Same as Then drops a typed custom Now from the request`, async () => {
		const createReportsInBulk = vi.fn().mockResolvedValue([]);
		const { user } = renderTheReportsPage({
			reportService: creatingWith(createReportsInBulk),
		});

		await toTheNowStep(user);
		await user.type(screen.getByRole("spinbutton", { name: /^Now/ }), "21");
		await user.click(screen.getByRole("radio", { name: SAME_AS_THEN }));

		expect(screen.getByRole("radio", { name: SAME_AS_THEN })).toBeChecked();
		await createdForZenith(user);
		await waitFor(() =>
			expect(createReportsInBulk).toHaveBeenCalledWith(
				expect.objectContaining({
					thenAndNow: expect.objectContaining({
						thenLengthDays: 30,
						nowLengthDays: null,
					}),
				}),
			),
		);
	});

	it(`a custom Then of exactly 14 days is accepted`, async () => {
		const { user } = renderTheReportsPage({ reportService: creatingWith() });

		await toTheThenStep(user);
		await user.click(await screen.findByRole("button", { name: "Custom" }));
		await user.type(screen.getByRole("spinbutton", { name: /^Then/ }), "14");

		expect(screen.queryByText(thenNeedsAtLeast(14))).not.toBeInTheDocument();
		expect(screen.getByRole("button", { name: "Next" })).toBeEnabled();
	});

	const createdForZenith = async (
		user: ReturnType<typeof renderTheReportsPage>["user"],
	) => {
		await nextUntil(
			user,
			() => screen.queryByRole("checkbox", { name: ZENITH.name }) !== null,
		);
		await pickOwner(user, ZENITH);
		await user.click(screen.getByRole("button", { name: createCount(1) }));
	};

	it(`a custom Now length travels with the request`, async () => {
		const createReportsInBulk = vi.fn().mockResolvedValue([]);
		const { user } = renderTheReportsPage({
			reportService: creatingWith(createReportsInBulk),
		});

		await toTheThenStep(user);
		await user.click(await screen.findByRole("button", { name: "30 days" }));
		await user.click(screen.getByRole("button", { name: "Next" }));
		await user.click(await screen.findByRole("radio", { name: "Custom" }));
		await user.type(screen.getByRole("spinbutton", { name: /^Now/ }), "21");
		await createdForZenith(user);

		await waitFor(() =>
			expect(createReportsInBulk).toHaveBeenCalledWith(
				expect.objectContaining({
					thenAndNow: expect.objectContaining({
						thenLengthDays: 30,
						nowLengthDays: 21,
					}),
				}),
			),
		);
	});

	it(`Same as Then travels as no Now length`, async () => {
		const createReportsInBulk = vi.fn().mockResolvedValue([]);
		const { user } = renderTheReportsPage({
			reportService: creatingWith(createReportsInBulk),
		});

		await toTheThenStep(user);
		await user.click(await screen.findByRole("button", { name: "30 days" }));
		await user.click(screen.getByRole("button", { name: "Next" }));
		expect(
			await screen.findByRole("radio", { name: SAME_AS_THEN }),
		).toBeChecked();
		await createdForZenith(user);

		await waitFor(() =>
			expect(createReportsInBulk).toHaveBeenCalledWith(
				expect.objectContaining({
					thenAndNow: expect.objectContaining({
						thenLengthDays: 30,
						nowLengthDays: null,
					}),
				}),
			),
		);
	});
});
