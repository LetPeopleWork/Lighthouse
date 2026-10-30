import { screen, waitFor, within } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import type { IReportService } from "../../services/Api/ReportService";
import {
	anOwnerToCreateOn,
	createCount,
	creationOptionsFor,
	GRAVITY,
	NOT_ADMIN_OF_ANY,
	neverAnswered,
	ONLY_ADMIN_OWNERS,
	OWNER_STEP,
	OWNERS_COULD_NOT_LOAD,
	PULSAR,
	pendingEpic6350,
	pickOwner,
	RETRY,
	renderTheReportsPage,
	SELECT_ALL,
	TEAMS_GROUP,
	toTheOwnerStep,
	ZENITH,
} from "../../tests/BulkReportsTestKit";
import { aReportService } from "../../tests/ReportsTestKit";

/**
 * The step of the wizard that asks who gets the report. It lists only the Teams and Portfolios the reader is
 * Admin of, says so, and tells a reader who is Admin of none why there is nothing to pick. While the list is on
 * its way, or when it cannot be loaded, nothing can be created.
 */

const pending = pendingEpic6350("02b");

const answeringOwners = (
	getCreationOptions: IReportService["getCreationOptions"],
) => aReportService({ getCreationOptions });

describe("Who gets this report", () => {
	// @us-02b @slice-02b @error @kpi-OUT-6350-G-per-owner-outcome-and-rbac @contract-shape:pure-function
	it.skip(`a coach whose Admin right went meanwhile is told why there is nothing to pick, instead of an empty list (${pending})`, async () => {
		const { user } = renderTheReportsPage({
			reportService: answeringOwners(
				vi.fn().mockResolvedValue(creationOptionsFor()),
			),
			permissions: { adminOf: [ZENITH] },
		});

		await toTheOwnerStep(user);

		expect(screen.getByText(OWNER_STEP, { exact: true })).toBeInTheDocument();
		expect(
			screen.getByText(NOT_ADMIN_OF_ANY, { exact: true }),
		).toBeInTheDocument();
		expect(screen.queryAllByRole("checkbox")).toEqual([]);
	});

	// @walking_skeleton @driving_port @us-02b @slice-02b @kpi-OUT-6350-G-per-owner-outcome-and-rbac @contract-shape:pure-function
	it(`a coach who only reads Pulsar is offered Gravity and Zenith, alphabetically, and told only Admin owners are listed`, async () => {
		const { user } = renderTheReportsPage({
			reportService: answeringOwners(
				vi
					.fn()
					.mockResolvedValue(
						creationOptionsFor(
							anOwnerToCreateOn(ZENITH),
							anOwnerToCreateOn(GRAVITY),
						),
					),
			),
		});

		await toTheOwnerStep(user);

		const teams = screen.getByRole("group", { name: TEAMS_GROUP });
		expect(
			within(teams)
				.getAllByRole("checkbox")
				.map(
					(box) =>
						box.getAttribute("aria-label") ?? box.closest("label")?.textContent,
				),
		).toEqual([GRAVITY.name, ZENITH.name]);
		expect(
			within(teams).queryByRole("checkbox", { name: PULSAR.name }),
		).not.toBeInTheDocument();
		expect(
			screen.getByText(ONLY_ADMIN_OWNERS, { exact: true }),
		).toBeInTheDocument();
	});

	// @us-02b @slice-02b @contract-shape:bounded-change
	it(`Select all picks every Team in its group and the button counts them`, async () => {
		const { user } = renderTheReportsPage({
			reportService: answeringOwners(
				vi
					.fn()
					.mockResolvedValue(
						creationOptionsFor(
							anOwnerToCreateOn(ZENITH),
							anOwnerToCreateOn(GRAVITY),
						),
					),
			),
		});

		await toTheOwnerStep(user);
		await user.click(
			within(screen.getByRole("group", { name: TEAMS_GROUP })).getByRole(
				"button",
				{ name: SELECT_ALL },
			),
		);

		expect(screen.getByRole("button", { name: createCount(2) })).toBeEnabled();
	});

	// @us-02b @slice-02b @edge @contract-shape:pure-function
	it(`with nobody picked yet nothing can be created`, async () => {
		const { user } = renderTheReportsPage({
			reportService: answeringOwners(
				vi
					.fn()
					.mockResolvedValue(creationOptionsFor(anOwnerToCreateOn(ZENITH))),
			),
		});

		await toTheOwnerStep(user);

		expect(screen.getByRole("button", { name: createCount(0) })).toBeDisabled();
		await pickOwner(user, ZENITH);
		expect(screen.getByRole("button", { name: createCount(1) })).toBeEnabled();
	});

	// @us-02b @slice-02b @edge @contract-shape:unbounded-preservation
	it.skip(`while the owners are on their way it shows a loading look and nothing can be created (${pending})`, async () => {
		const { user } = renderTheReportsPage({
			reportService: answeringOwners(vi.fn(() => neverAnswered())),
		});

		await toTheOwnerStep(user);

		expect(screen.getByRole("progressbar")).toBeInTheDocument();
		expect(
			screen.getByRole("button", { name: /^Create \d+ reports?$/ }),
		).toBeDisabled();
	});

	// @us-02b @slice-02b @error @contract-shape:unbounded-preservation
	it.skip(`when the owners cannot be loaded it says so, nothing can be created, and Retry loads them (${pending})`, async () => {
		const getCreationOptions = vi
			.fn()
			.mockRejectedValueOnce(new Error("the network dropped"))
			.mockResolvedValue(creationOptionsFor(anOwnerToCreateOn(ZENITH)));
		const { user } = renderTheReportsPage({
			reportService: answeringOwners(getCreationOptions),
		});

		await toTheOwnerStep(user);

		expect(
			await screen.findByText(OWNERS_COULD_NOT_LOAD, { exact: true }),
		).toBeInTheDocument();
		expect(
			screen.getByRole("button", { name: /^Create \d+ reports?$/ }),
		).toBeDisabled();
		await user.click(screen.getByRole("button", { name: RETRY }));
		await waitFor(() =>
			expect(
				screen.getByRole("checkbox", { name: ZENITH.name }),
			).toBeInTheDocument(),
		);
	});

	// @us-02b @slice-02b @error @contract-shape:unbounded-preservation
	it.skip(`when permissions cannot be loaded the owner step shows its could-not-load look, never a permissive list (${pending})`, async () => {
		const { user } = renderTheReportsPage({
			reportService: answeringOwners(
				vi
					.fn()
					.mockResolvedValue(creationOptionsFor(anOwnerToCreateOn(ZENITH))),
			),
			permissions: "failing",
		});

		await toTheOwnerStep(user);

		expect(
			await screen.findByText(OWNERS_COULD_NOT_LOAD, { exact: true }),
		).toBeInTheDocument();
		expect(screen.getByRole("button", { name: RETRY })).toBeInTheDocument();
	});
});
