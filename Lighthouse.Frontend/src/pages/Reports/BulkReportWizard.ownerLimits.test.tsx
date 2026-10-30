import { screen, waitFor, within } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import {
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
	pickOwner,
	renderTheReportsPage,
	selectAllOf,
	TEAMS_GROUP,
	toTheOwnerStep,
	whereTheReaderIs,
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
		permissions: { adminOf: [ZENITH, GRAVITY, EQUINOX, OCEAN_EXPLORER] },
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

		await user.click(
			screen.getByRole("link", { name: openSettingsOf(EQUINOX.name) }),
		);

		expect(whereTheReaderIs()).toBe(`/teams/${EQUINOX.id}/settings`);
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
});
