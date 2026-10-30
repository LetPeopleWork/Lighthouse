import { fireEvent, screen, waitFor, within } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import {
	APOLLO_PORTFOLIO,
	anOwnerToCreateOn,
	createCount,
	creationOptionsFor,
	EQUINOX,
	everyReport,
	GRAVITY,
	keptHistoryStarts,
	nextUntil,
	OCEAN_EXPLORER,
	OWNER_STEP,
	openSettingsOf,
	PORTFOLIOS_GROUP,
	pickOwner,
	pressCreateReports,
	renderTheReportsPage,
	selectAllOf,
	TEAMS_GROUP,
	toTheOwnerStep,
	ZENITH,
} from "../../tests/BulkReportsTestKit";
import { aReportService } from "../../tests/ReportsTestKit";

/**
 * An owner that no longer keeps the finished work a window reaches back to cannot hold that report. The owner
 * step still lists it, greyed and unpickable, says when its kept history starts and when Then would start, and
 * leads to its settings. The server answers each owner's kept-history start, so the browser holds no copy of it.
 */

// Today is 27 Oct 2026; a 90-day Then ending 29 Jul 2026 starts 1 May 2026, before Equinox's kept history.
const equinoxKeepsSince30Jul = creationOptionsFor(
	anOwnerToCreateOn(ZENITH),
	anOwnerToCreateOn(GRAVITY),
	anOwnerToCreateOn(EQUINOX, "2026-07-30"),
	anOwnerToCreateOn(OCEAN_EXPLORER),
);

const theOwnersOffered = () =>
	aReportService({
		getAllReports: vi.fn().mockResolvedValue(everyReport()),
		getCreationOptions: vi.fn().mockResolvedValue(equinoxKeepsSince30Jul),
	});

// The kit's default reader is not Admin of Equinox, and the owner step lists only owners its reader is Admin of.
const renderAsAdminOfEquinox = (
	scene: Pick<
		NonNullable<Parameters<typeof renderTheReportsPage>[0]>,
		"reportService"
	>,
) =>
	renderTheReportsPage({
		...scene,
		permissions: {
			adminOf: [ZENITH, GRAVITY, EQUINOX, OCEAN_EXPLORER, APOLLO_PORTFOLIO],
		},
	});

const toTheOwnerStepWithThenEnding29Jul = async (
	user: Parameters<typeof toTheOwnerStep>[0],
	thenPreset = "90 days",
) => {
	await toTheOwnerStep(user, { thenPreset });
};

const backUntil = async (
	user: Parameters<typeof toTheOwnerStep>[0],
	reached: () => boolean,
): Promise<void> => {
	if (reached()) return;
	await user.click(screen.getByRole("button", { name: "Back" }));
	return backUntil(user, reached);
};

describe("Owners that cannot hold the window", () => {
	it("Equinox is listed but cannot be picked, and says why and where to change it", async () => {
		const { user } = renderAsAdminOfEquinox({
			reportService: theOwnersOffered(),
		});

		await toTheOwnerStepWithThenEnding29Jul(user);

		const equinox = screen.getByRole("checkbox", { name: EQUINOX.name });
		expect(equinox).toBeDisabled();
		expect(
			screen.getByText(keptHistoryStarts("30 Jul 2026", "1 May 2026"), {
				exact: true,
			}),
		).toBeInTheDocument();

		const settings = screen.getByRole("link", {
			name: openSettingsOf(EQUINOX.name),
		});
		expect(settings).toHaveAttribute("href", `/teams/${EQUINOX.id}/settings`);
		expect(settings).toHaveAttribute("target", "_blank");
		expect(settings).toHaveAttribute("rel", "noopener noreferrer");
		expect(screen.getByText(OWNER_STEP)).toBeInTheDocument();
	});

	it("Select all picks every Team that can hold the window and leaves Equinox out", async () => {
		const { user } = renderAsAdminOfEquinox({
			reportService: theOwnersOffered(),
		});

		await toTheOwnerStepWithThenEnding29Jul(user);
		await user.click(
			within(screen.getByRole("group", { name: TEAMS_GROUP })).getByRole(
				"button",
				{ name: selectAllOf(TEAMS_GROUP) },
			),
		);

		expect(
			screen.getByRole("checkbox", { name: EQUINOX.name }),
		).not.toBeChecked();
		expect(screen.getByRole("button", { name: createCount(2) })).toBeEnabled();
	});

	it("a shorter window that stays inside Equinox's kept history makes it pickable", async () => {
		const { user } = renderAsAdminOfEquinox({
			reportService: theOwnersOffered(),
		});

		await toTheOwnerStepWithThenEnding29Jul(user, "30 days");

		expect(screen.getByRole("checkbox", { name: EQUINOX.name })).toBeEnabled();
		expect(screen.queryByText(/^Kept history starts/)).not.toBeInTheDocument();
	});

	it("a Then starting on the very day the kept history starts can be held", async () => {
		const { user } = renderAsAdminOfEquinox({
			reportService: aReportService({
				getAllReports: vi.fn().mockResolvedValue(everyReport()),
				getCreationOptions: vi
					.fn()
					.mockResolvedValue(
						creationOptionsFor(anOwnerToCreateOn(EQUINOX, "2026-05-01")),
					),
			}),
		});

		await toTheOwnerStepWithThenEnding29Jul(user);

		expect(screen.getByRole("checkbox", { name: EQUINOX.name })).toBeEnabled();
	});

	it("a pick made under a short window is dropped when going back makes the window too long for that owner", async () => {
		const createReportsInBulk = vi.fn().mockResolvedValue([]);
		const { user } = renderAsAdminOfEquinox({
			reportService: aReportService({
				getAllReports: vi.fn().mockResolvedValue(everyReport()),
				getCreationOptions: vi.fn().mockResolvedValue(equinoxKeepsSince30Jul),
				createReportsInBulk,
			}),
		});
		await toTheOwnerStepWithThenEnding29Jul(user, "30 days");
		await pickOwner(user, EQUINOX);
		await pickOwner(user, ZENITH);
		expect(screen.getByRole("button", { name: createCount(2) })).toBeEnabled();

		await backUntil(
			user,
			() => screen.queryByRole("button", { name: "90 days" }) !== null,
		);
		await user.click(screen.getByRole("button", { name: "90 days" }));
		await nextUntil(user, () => screen.queryByText(OWNER_STEP) !== null);

		const equinox = screen.getByRole("checkbox", { name: EQUINOX.name });
		expect(equinox).toBeDisabled();
		expect(equinox).not.toBeChecked();
		expect(
			screen.getByText(keptHistoryStarts("30 Jul 2026", "1 May 2026"), {
				exact: true,
			}),
		).toBeInTheDocument();
		await user.click(screen.getByRole("button", { name: createCount(1) }));

		await waitFor(() =>
			expect(createReportsInBulk).toHaveBeenCalledWith(
				expect.objectContaining({
					owners: [{ ownerKind: "team", ownerId: ZENITH.id }],
				}),
			),
		);
	});

	it("when every owner picked cannot hold the window nothing can be created", async () => {
		const { user } = renderAsAdminOfEquinox({
			reportService: aReportService({
				getAllReports: vi.fn().mockResolvedValue(everyReport()),
				getCreationOptions: vi
					.fn()
					.mockResolvedValue(
						creationOptionsFor(anOwnerToCreateOn(EQUINOX, "2026-07-30")),
					),
			}),
		});

		await toTheOwnerStepWithThenEnding29Jul(user);

		expect(screen.getByRole("checkbox", { name: EQUINOX.name })).toBeDisabled();
		expect(screen.getByRole("button", { name: createCount(0) })).toBeDisabled();
	});

	it("only Now reaching past the kept history names Now, not Then", async () => {
		const { user } = renderAsAdminOfEquinox({
			reportService: theOwnersOffered(),
		});

		await pressCreateReports(user);
		await user.click(await screen.findByRole("button", { name: "Next" }));
		await user.click(await screen.findByRole("button", { name: "30 days" }));
		fireEvent.change(screen.getByLabelText("Then ends on"), {
			target: { value: "2026-10-20" },
		});
		await user.click(screen.getByRole("button", { name: "Next" }));
		await user.click(screen.getByRole("radio", { name: "Custom" }));
		await user.type(screen.getByLabelText("Now length in days"), "120");
		await nextUntil(user, () => screen.queryByText(OWNER_STEP) !== null);

		expect(screen.getByRole("checkbox", { name: EQUINOX.name })).toBeDisabled();
		expect(
			screen.getByText(keptHistoryStarts("30 Jul 2026", "30 Jun 2026", "Now"), {
				exact: true,
			}),
		).toBeInTheDocument();
	});

	it("a Then starting one day before the kept history starts cannot be held", async () => {
		const { user } = renderAsAdminOfEquinox({
			reportService: aReportService({
				getAllReports: vi.fn().mockResolvedValue(everyReport()),
				getCreationOptions: vi
					.fn()
					.mockResolvedValue(
						creationOptionsFor(anOwnerToCreateOn(EQUINOX, "2026-05-02")),
					),
			}),
		});

		await toTheOwnerStepWithThenEnding29Jul(user);

		expect(screen.getByRole("checkbox", { name: EQUINOX.name })).toBeDisabled();
		expect(
			screen.getByText(keptHistoryStarts("2 May 2026", "1 May 2026"), {
				exact: true,
			}),
		).toBeInTheDocument();
	});

	it("a Portfolio that cannot hold the window is greyed, says why, and Select all leaves it out", async () => {
		const { user } = renderAsAdminOfEquinox({
			reportService: aReportService({
				getAllReports: vi.fn().mockResolvedValue(everyReport()),
				getCreationOptions: vi
					.fn()
					.mockResolvedValue(
						creationOptionsFor(
							anOwnerToCreateOn(OCEAN_EXPLORER, "2026-07-30"),
							anOwnerToCreateOn(APOLLO_PORTFOLIO),
						),
					),
			}),
		});

		await toTheOwnerStepWithThenEnding29Jul(user);

		expect(
			screen.getByRole("checkbox", { name: OCEAN_EXPLORER.name }),
		).toBeDisabled();
		expect(
			screen.getByText(keptHistoryStarts("30 Jul 2026", "1 May 2026"), {
				exact: true,
			}),
		).toBeInTheDocument();
		expect(
			screen.getByRole("link", { name: openSettingsOf(OCEAN_EXPLORER.name) }),
		).toHaveAttribute("href", `/portfolios/${OCEAN_EXPLORER.id}/settings`);

		await user.click(
			within(screen.getByRole("group", { name: PORTFOLIOS_GROUP })).getByRole(
				"button",
				{ name: selectAllOf(PORTFOLIOS_GROUP) },
			),
		);

		expect(
			screen.getByRole("checkbox", { name: OCEAN_EXPLORER.name }),
		).not.toBeChecked();
		expect(
			screen.getByRole("checkbox", { name: APOLLO_PORTFOLIO.name }),
		).toBeChecked();
		expect(screen.getByRole("button", { name: createCount(1) })).toBeEnabled();
	});

	it("a dropped pick stays dropped when the window shortens again", async () => {
		const { user } = renderAsAdminOfEquinox({
			reportService: theOwnersOffered(),
		});
		await toTheOwnerStepWithThenEnding29Jul(user, "30 days");
		await pickOwner(user, EQUINOX);
		await pickOwner(user, ZENITH);

		await backUntil(
			user,
			() => screen.queryByRole("button", { name: "90 days" }) !== null,
		);
		await user.click(screen.getByRole("button", { name: "90 days" }));
		await nextUntil(user, () => screen.queryByText(OWNER_STEP) !== null);
		await backUntil(
			user,
			() => screen.queryByRole("button", { name: "30 days" }) !== null,
		);
		await user.click(screen.getByRole("button", { name: "30 days" }));
		await nextUntil(user, () => screen.queryByText(OWNER_STEP) !== null);

		const equinox = screen.getByRole("checkbox", { name: EQUINOX.name });
		expect(equinox).toBeEnabled();
		expect(equinox).not.toBeChecked();
		expect(screen.getByRole("checkbox", { name: ZENITH.name })).toBeChecked();
		expect(screen.getByRole("button", { name: createCount(1) })).toBeEnabled();
	});
});
