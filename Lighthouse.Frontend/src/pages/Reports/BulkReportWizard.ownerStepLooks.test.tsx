import { screen, within } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import type { IReportService } from "../../services/Api/ReportService";
import {
	anOwnerToCreateOn,
	createCount,
	creationOptionsFor,
	GRAVITY,
	NOT_ADMIN_OF_ANY,
	ONLY_ADMIN_OWNERS,
	OWNER_STEP,
	OWNERS_COULD_NOT_LOAD,
	OWNERS_LOADING,
	PULSAR,
	permissionsOfAdminOf,
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

const answeringOwners = (
	getCreationOptions: IReportService["getCreationOptions"],
) => aReportService({ getCreationOptions });

describe("Who gets this report", () => {
	it(`a coach whose Admin right went meanwhile is told why there is nothing to pick, instead of an empty list`, async () => {
		const { user } = renderTheReportsPage({
			reportService: answeringOwners(
				vi
					.fn()
					.mockResolvedValue(
						creationOptionsFor(
							anOwnerToCreateOn(GRAVITY),
							anOwnerToCreateOn(PULSAR),
						),
					),
			),
			permissions: { adminOf: [ZENITH] },
		});

		await toTheOwnerStep(user);

		expect(screen.getByText(OWNER_STEP, { exact: true })).toBeInTheDocument();
		expect(
			screen.getByText(NOT_ADMIN_OF_ANY, { exact: true }),
		).toBeInTheDocument();
		expect(screen.queryAllByRole("checkbox")).toEqual([]);
		expect(
			screen.queryByRole("group", { name: TEAMS_GROUP }),
		).not.toBeInTheDocument();
	});

	it(`a coach who only reads Pulsar is offered Gravity and Zenith, alphabetically, and told only Admin owners are listed`, async () => {
		const { user } = renderTheReportsPage({
			reportService: answeringOwners(
				vi
					.fn()
					.mockResolvedValue(
						creationOptionsFor(
							anOwnerToCreateOn(ZENITH),
							anOwnerToCreateOn(GRAVITY),
							anOwnerToCreateOn(PULSAR),
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
		expect(
			screen.queryByText(NOT_ADMIN_OF_ANY, { exact: true }),
		).not.toBeInTheDocument();
	});

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

	it(`when permissions cannot be loaded the owner step shows its could-not-load look, never a permissive list`, async () => {
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
		expect(screen.getByRole("alert")).toHaveTextContent(OWNERS_COULD_NOT_LOAD);
		expect(screen.getByRole("button", { name: RETRY })).toBeInTheDocument();
		expect(screen.queryAllByRole("checkbox")).toEqual([]);
		expect(
			screen.queryByRole("group", { name: TEAMS_GROUP }),
		).not.toBeInTheDocument();
		expect(screen.getByRole("button", { name: createCount(0) })).toBeDisabled();
		expect(
			screen.queryByText(NOT_ADMIN_OF_ANY, { exact: true }),
		).not.toBeInTheDocument();
	});

	it(`when permissions cannot be loaded and no Team is on offer either, it still does not claim she is Admin of none`, async () => {
		const { user } = renderTheReportsPage({
			reportService: answeringOwners(
				vi.fn().mockResolvedValue(creationOptionsFor()),
			),
			permissions: "failing",
		});

		await toTheOwnerStep(user);

		expect(
			await screen.findByText(OWNERS_COULD_NOT_LOAD, { exact: true }),
		).toBeInTheDocument();
		expect(
			screen.queryByText(NOT_ADMIN_OF_ANY, { exact: true }),
		).not.toBeInTheDocument();
	});

	it(`after a failed permission load, Retry shows the loading look and then lists her Admin Teams`, async () => {
		let permissionsArrive: () => void = () => undefined;
		const permissions = {
			answeredBy: vi
				.fn()
				.mockRejectedValueOnce(new Error("the network dropped"))
				.mockImplementation(
					() =>
						new Promise((resolve) => {
							permissionsArrive = () =>
								resolve(permissionsOfAdminOf([ZENITH, GRAVITY]));
						}),
				),
		};
		const { user } = renderTheReportsPage({
			reportService: answeringOwners(
				vi
					.fn()
					.mockResolvedValue(
						creationOptionsFor(
							anOwnerToCreateOn(ZENITH),
							anOwnerToCreateOn(GRAVITY),
							anOwnerToCreateOn(PULSAR),
						),
					),
			),
			permissions,
		});

		await toTheOwnerStep(user);
		expect(
			await screen.findByText(OWNERS_COULD_NOT_LOAD, { exact: true }),
		).toBeInTheDocument();
		await user.click(screen.getByRole("button", { name: RETRY }));

		expect(
			await screen.findByRole("progressbar", { name: OWNERS_LOADING }),
		).toBeInTheDocument();
		expect(screen.getByRole("button", { name: createCount(0) })).toBeDisabled();
		expect(
			screen.queryByText(NOT_ADMIN_OF_ANY, { exact: true }),
		).not.toBeInTheDocument();
		permissionsArrive();

		const teams = await screen.findByRole("group", { name: TEAMS_GROUP });
		expect(
			within(teams)
				.getAllByRole("checkbox")
				.map(
					(box) =>
						box.getAttribute("aria-label") ?? box.closest("label")?.textContent,
				),
		).toEqual([GRAVITY.name, ZENITH.name]);
		expect(screen.queryByRole("progressbar")).not.toBeInTheDocument();
	});
});
