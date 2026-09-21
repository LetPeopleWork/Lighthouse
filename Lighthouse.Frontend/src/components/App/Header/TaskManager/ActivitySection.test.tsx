import { render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import type {
	IUpdateTask,
	UpdateTaskType,
} from "../../../../services/UpdateSubscriptionService";
import ActivitySection from "./ActivitySection";

/**
 * DISTILL specifications (User Story #6055 — the Activity list names entities, not work).
 *
 * Slice 01 in full — what a row reads — plus the frontend half of slice 02: the clause that explains a
 * wait, and the promise that the browser never recomputes what the instance already decided.
 *
 * Everything here is pending (`it.skip`) until DELIVER, so the suite is green at hand-off and each is
 * un-skipped as its step lands. A skipped test is still type-checked, which is why
 * `UpdateSubscriptionService` carries the widened `waitingBehind` scaffold: these specifications
 * express the target shape today and DELIVER narrows the type as it removes the old arm.
 *
 * The backend half of slice 02 — which piece of work holds the lane, and whether it is this row's own
 * entity — lives in `Story6055ActivityNamesTheWork{Scenarios,Specifications}.cs`.
 */

const mockGetTerm = vi.fn((key: string) => key);

vi.mock("../../../../services/TerminologyContext", async () => {
	const actual = await vi.importActual<
		typeof import("../../../../services/TerminologyContext")
	>("../../../../services/TerminologyContext");
	return {
		...actual,
		useTerminology: () => ({ getTerm: mockGetTerm }),
	};
});

const aTask = (overrides: Partial<IUpdateTask> = {}): IUpdateTask => ({
	updateType: "Features",
	id: 3,
	name: "Ocean Explorer",
	status: "InProgress",
	...overrides,
});

const renderRows = (tasks: IUpdateTask[]) =>
	render(
		<ActivitySection tasks={tasks} stopAsked={new Set()} onCancel={vi.fn()} />,
	);

const rowText = (updateType: UpdateTaskType, id: number) =>
	screen.getByTestId(`task-manager-row-${updateType}-${id}`).textContent ?? "";

const EVERY_UPDATE_TYPE: UpdateTaskType[] = [
	"Team",
	"Features",
	"Forecasts",
	"TeamDelete",
	"PortfolioDelete",
];

describe("Activity rows say what the work is", () => {
	// @AC-01.1 — the reported defect. Asserted as a DIFFERENCE rather than against two expected
	// literals: an assertion that hard-codes both strings keeps passing while a future update type
	// quietly joins the pile, which is exactly how this shipped.
	//
	// Both rows are given the SAME status deliberately. With one running and one queued they differ in
	// their last word whatever the lookup does, so the assertion passes against the very bug it is meant
	// to catch — which is what it did when this was first written.
	it.skip("tells a portfolio refresh apart from the forecast it triggers", () => {
		renderRows([
			aTask({ updateType: "Features", status: "Queued" }),
			aTask({ updateType: "Forecasts", status: "Queued" }),
		]);

		expect(rowText("Features", 3)).not.toEqual(rowText("Forecasts", 3));
	});

	// @AC-01.2 — five members, five answers. The lookup that caused #6055 had a default arm absorbing
	// three of them, so the promise is about the whole enum rather than about the pair that was reported.
	it.skip("gives every kind of work its own phrase", () => {
		renderRows(
			EVERY_UPDATE_TYPE.map((updateType) =>
				aTask({ updateType, status: "InProgress" }),
			),
		);

		const phrases = EVERY_UPDATE_TYPE.map((updateType) =>
			rowText(updateType, 3),
		);

		expect(new Set(phrases).size).toBe(EVERY_UPDATE_TYPE.length);
	});

	// @AC-01.3 @AC-01.5 — the row is one sentence built from two vocabularies. The noun is whatever the
	// tenant renamed it to; the verb is Lighthouse's own word and is never looked up.
	it.skip("uses the tenant's noun and Lighthouse's own verb", () => {
		mockGetTerm.mockImplementation((key: string) =>
			key === "team" ? "Squad" : "Programme",
		);

		renderRows([
			aTask({ updateType: "Features", status: "InProgress" }),
			aTask({ updateType: "Team", id: 7, name: "Voyager", status: "Queued" }),
		]);

		expect(rowText("Features", 3)).toContain("Refreshing Programme");
		expect(rowText("Team", 7)).toContain("Refreshing Squad");
		expect(rowText("Features", 3)).not.toContain("Portfolio");
	});

	// @AC-01.4 — the suffix is replaced, not joined. Two ways of saying one thing in one column is how
	// they come to disagree.
	it.skip("says a removal is a removal without also appending one", () => {
		renderRows([
			aTask({ updateType: "PortfolioDelete", status: "InProgress" }),
		]);

		expect(rowText("PortfolioDelete", 3)).toContain("Removing");
		expect(rowText("PortfolioDelete", 3)).not.toContain("(removal)");
	});

	// @AC-01.6 — the id the existing #5511 suite addresses rows by is a fixed point, so that suite keeps
	// working across this change rather than being rewritten alongside it.
	//
	// Runs now rather than waiting for DELIVER: this is a pin on behaviour that already exists and must
	// survive, not a promise about behaviour that does not. A pin that sits skipped guards nothing during
	// the change it exists to guard.
	it("keeps the handle every other specification addresses a row by", () => {
		renderRows([aTask({ updateType: "Forecasts", status: "Queued" })]);

		expect(screen.getByTestId("task-manager-row-Forecasts-3")).toBeVisible();
	});
});

describe("Activity rows explain a wait without naming themselves", () => {
	// @AC-02.1 — the clause names the holder's activity when the holder is this row's own entity.
	it.skip("says a forecast is behind its own portfolio's refresh", () => {
		renderRows([
			aTask({
				updateType: "Forecasts",
				status: "Queued",
				waitingBehind: {
					name: "Ocean Explorer",
					updateType: "Features",
					isSameEntity: true,
				},
			}),
		]);

		expect(rowText("Forecasts", 3)).toContain("Queued behind its own refresh");
		expect(rowText("Forecasts", 3)).not.toContain(
			"Queued behind Ocean Explorer",
		);
	});

	// @AC-02.3 — and the word follows what the HOLDER is doing, not what this row is doing.
	it.skip("says a refresh is behind its own portfolio's removal", () => {
		renderRows([
			aTask({
				updateType: "Features",
				status: "Queued",
				waitingBehind: {
					name: "Ocean Explorer",
					updateType: "PortfolioDelete",
					isSameEntity: true,
				},
			}),
		]);

		expect(rowText("Features", 3)).toContain("Queued behind its own removal");
	});

	// @AC-02.2 — the reading that already shipped, which must not regress. A different entity is still
	// named, because that name is the difference between waiting and wedged.
	it.skip("still names a different entity that is holding the lane", () => {
		renderRows([
			aTask({
				updateType: "Team",
				id: 7,
				name: "Voyager",
				status: "Queued",
				waitingBehind: {
					name: "Ocean Explorer",
					updateType: "Features",
					isSameEntity: false,
				},
			}),
		]);

		expect(rowText("Team", 7)).toContain("Queued behind Ocean Explorer");
	});

	// @AC-02.5 — work whose lane is free is waiting for nothing, and says nothing rather than naming an
	// arbitrary running row. Already true, and a pin for the same reason as AC-01.6: the clause gains a
	// branch in this story, and "gains a branch" is exactly when a row with no clause starts growing one.
	it("says nothing extra when the row is waiting for nothing", () => {
		renderRows([aTask({ updateType: "Team", id: 7, status: "Queued" })]);

		expect(rowText("Team", 7)).toContain("Queued");
		expect(rowText("Team", 7)).not.toContain("behind");
	});

	// @AC-02.8 — one decider. The payload below is deliberately self-contradictory: the flag says "your
	// own entity" while the name says otherwise. A browser that recomputed sameness from the row and the
	// holder would print the name; one that trusts the instance prints the activity. Only the second
	// keeps a partial re-read from making the two disagree.
	it.skip("trusts the instance's verdict rather than recomputing it", () => {
		renderRows([
			aTask({
				updateType: "Forecasts",
				status: "Queued",
				waitingBehind: {
					name: "Some Other Portfolio",
					updateType: "Features",
					isSameEntity: true,
				},
			}),
		]);

		expect(rowText("Forecasts", 3)).toContain("Queued behind its own refresh");
		expect(rowText("Forecasts", 3)).not.toContain("Some Other Portfolio");
	});
});
