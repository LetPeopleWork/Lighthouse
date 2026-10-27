import { screen, waitFor, within } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import {
	APOLLO_PORTFOLIO,
	anOwnerToCreateOn,
	CREATE_REPORTS,
	createCount,
	created,
	creationOptionsFor,
	everyReport,
	GRAVITY,
	NEUROLINK_CITY,
	nextUntil,
	OCEAN_EXPLORER,
	PORTFOLIOS_GROUP,
	pendingEpic6350,
	pickOwner,
	renderTheReportsPage,
	SAME_AS_THEN,
	SELECT_ALL,
	TEAMS_GROUP,
	THEN_PRESETS,
	toTheOwnerStep,
	ZENITH,
} from "../../tests/BulkReportsTestKit";
import { aReportService, thenNeedsAtLeast } from "../../tests/ReportsTestKit";

/**
 * Portfolios join the selection: one run may create the same comparison for Teams and Portfolios together. The
 * window is chosen the same way whatever mix is picked: Then of 30, 90 or 180 days or a custom number of days of
 * at least 14, and Now as long as Then unless the reader says otherwise.
 */

const pending = pendingEpic6350("03");

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
	// @walking_skeleton @driving_port @us-03 @slice-03 @kpi-OUT-6350-bulk-creation-used @contract-shape:bounded-change
	it.skip(`a coach creates one comparison for two Teams and Ocean Explorer in one go (${pending})`, async () => {
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

	// @us-03 @slice-03 @contract-shape:pure-function
	it.skip(`Portfolios are listed after Teams, alphabetically, with their own Select all (${pending})`, async () => {
		const { user } = renderTheReportsPage({ reportService: creatingWith() });

		await toTheOwnerStep(user);

		const groups = screen
			.getAllByRole("group")
			.map((group) => group.getAttribute("aria-label"));
		expect(groups).toEqual([TEAMS_GROUP, PORTFOLIOS_GROUP]);
		const portfolios = screen.getByRole("group", { name: PORTFOLIOS_GROUP });
		await user.click(
			within(portfolios).getByRole("button", { name: SELECT_ALL }),
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

	// @us-03 @slice-03 @edge @contract-shape:pure-function
	it.skip(`only Portfolios may be picked, with no Team at all (${pending})`, async () => {
		const createReportsInBulk = vi.fn().mockResolvedValue([]);
		const { user } = renderTheReportsPage({
			reportService: creatingWith(createReportsInBulk),
		});

		await toTheOwnerStep(user);
		await pickOwner(user, OCEAN_EXPLORER);
		await user.click(screen.getByRole("button", { name: createCount(1) }));

		await waitFor(() =>
			expect(createReportsInBulk).toHaveBeenCalledWith(
				expect.objectContaining({
					owners: [{ ownerKind: "portfolio", ownerId: OCEAN_EXPLORER.id }],
				}),
			),
		);
	});
});

describe("Choosing the window for several owners", () => {
	// @us-03 @slice-03 @contract-shape:pure-function
	it.skip(`Then offers 30, 90 and 180 days or a custom length, and Now is as long as Then unless changed (${pending})`, async () => {
		const { user } = renderTheReportsPage({ reportService: creatingWith() });

		await user.click(
			await screen.findByRole("button", { name: CREATE_REPORTS }),
		);

		await screen.findByRole("button", { name: THEN_PRESETS[0] });
		expect(
			THEN_PRESETS.filter(
				(preset) => screen.queryByRole("button", { name: preset }) === null,
			),
		).toEqual([]);
		expect(screen.getByRole("radio", { name: SAME_AS_THEN })).toBeChecked();
	});

	// @us-03 @slice-03 @error @contract-shape:unbounded-preservation
	it.skip(`a custom Then shorter than 14 days is refused before any owner is asked (${pending})`, async () => {
		const createReportsInBulk = vi.fn();
		const { user } = renderTheReportsPage({
			reportService: creatingWith(createReportsInBulk),
		});

		await user.click(
			await screen.findByRole("button", { name: CREATE_REPORTS }),
		);
		await user.click(await screen.findByRole("button", { name: "Custom" }));
		await user.type(screen.getByRole("spinbutton", { name: /^Then/ }), "13");

		expect(
			await screen.findByText(thenNeedsAtLeast(14), { exact: true }),
		).toBeInTheDocument();
		expect(screen.getByRole("button", { name: "Next" })).toBeDisabled();
		expect(createReportsInBulk).not.toHaveBeenCalled();
	});

	// @us-03 @slice-03 @edge @contract-shape:pure-function
	it.skip(`a custom Then of exactly 14 days is accepted (${pending})`, async () => {
		const { user } = renderTheReportsPage({ reportService: creatingWith() });

		await user.click(
			await screen.findByRole("button", { name: CREATE_REPORTS }),
		);
		await user.click(await screen.findByRole("button", { name: "Custom" }));
		await user.type(screen.getByRole("spinbutton", { name: /^Then/ }), "14");

		expect(screen.queryByText(thenNeedsAtLeast(14))).not.toBeInTheDocument();
		expect(screen.getByRole("button", { name: "Next" })).toBeEnabled();
	});

	// @us-03 @slice-03 @contract-shape:bounded-change
	it.skip(`a custom Now length travels with the request; Same as Then travels as no Now length (${pending})`, async () => {
		const createReportsInBulk = vi.fn().mockResolvedValue([]);
		const { user } = renderTheReportsPage({
			reportService: creatingWith(createReportsInBulk),
		});

		await user.click(
			await screen.findByRole("button", { name: CREATE_REPORTS }),
		);
		await user.click(await screen.findByRole("button", { name: "30 days" }));
		await user.click(screen.getByRole("radio", { name: "Custom" }));
		await user.type(screen.getByRole("spinbutton", { name: /^Now/ }), "21");
		await nextUntil(
			user,
			() => screen.queryByRole("checkbox", { name: ZENITH.name }) !== null,
		);
		await pickOwner(user, ZENITH);
		await user.click(screen.getByRole("button", { name: createCount(1) }));

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
});
