import { screen, within } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import {
	anOwnerToCreateOn,
	createCount,
	creationOptionsFor,
	EQUINOX,
	everyReport,
	GRAVITY,
	keptHistoryStarts,
	OCEAN_EXPLORER,
	openSettingsOf,
	pendingEpic6350,
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

const pending = pendingEpic6350("04");

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

const toTheOwnerStepWithThenEnding29Jul = async (
	user: Parameters<typeof toTheOwnerStep>[0],
	thenPreset = "90 days",
) => {
	await toTheOwnerStep(user, { thenPreset });
};

describe("Owners that cannot hold the window", () => {
	it.skip(`Equinox is listed but cannot be picked, and says why and where to change it (${pending})`, async () => {
		const { user } = renderTheReportsPage({
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

	it.skip(`Select all picks every Team that can hold the window and leaves Equinox out (${pending})`, async () => {
		const { user } = renderTheReportsPage({
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

	it.skip(`a shorter window that stays inside Equinox's kept history makes it pickable (${pending})`, async () => {
		const { user } = renderTheReportsPage({
			reportService: theOwnersOffered(),
		});

		await toTheOwnerStepWithThenEnding29Jul(user, "30 days");

		expect(screen.getByRole("checkbox", { name: EQUINOX.name })).toBeEnabled();
		expect(screen.queryByText(/^Kept history starts/)).not.toBeInTheDocument();
	});

	it.skip(`a Then starting on the very day the kept history starts can be held (${pending})`, async () => {
		const { user } = renderTheReportsPage({
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

	it.skip(`when every owner picked cannot hold the window nothing can be created (${pending})`, async () => {
		const { user } = renderTheReportsPage({
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

		expect(screen.getByRole("button", { name: createCount(0) })).toBeDisabled();
	});
});
