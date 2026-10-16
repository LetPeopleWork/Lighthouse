import { render, screen } from "@testing-library/react";
import { describe, expect, test } from "vitest";
import type { StaleReasonKind } from "../../../models/WorkItem";
import TimeInStateBadge, { daysInState } from "./TimeInStateBadge";

describe("TimeInStateBadge", () => {
	const now = new Date("2026-05-25T12:00:00Z");

	test.each([
		["2026-05-25T00:00:00Z", "1d in In Progress"],
		["2026-05-24T00:00:00Z", "2d in In Progress"],
		["2026-05-23T23:00:00Z", "3d in In Progress"],
		["2026-05-15T12:00:00Z", "11d in In Progress"],
	])(
		"renders days in current state for entered date %s",
		(enteredAt, expectedText) => {
			render(
				<TimeInStateBadge
					currentStateEnteredAt={new Date(enteredAt)}
					currentStateName="In Progress"
					now={now}
				/>,
			);

			expect(screen.getByText(expectedText)).toBeInTheDocument();
		},
	);

	test("counts an item that entered its state today as 1d, not 0d", () => {
		render(
			<TimeInStateBadge
				currentStateEnteredAt={new Date("2026-05-25T08:30:00Z")}
				currentStateName="In Progress"
				now={now}
			/>,
		);

		expect(screen.getByText("1d in In Progress")).toBeInTheDocument();
		expect(screen.queryByText("0d in In Progress")).not.toBeInTheDocument();
	});

	test("renders an em dash placeholder when there is no entered date", () => {
		render(
			<TimeInStateBadge
				currentStateEnteredAt={null}
				currentStateName="In Progress"
				now={now}
			/>,
		);

		expect(screen.getByText("—")).toBeInTheDocument();
		expect(screen.queryByText(/in In Progress/)).not.toBeInTheDocument();
	});

	test("renders an em dash and no stale treatment when there is no entered date even when Lighthouse marks it stale", () => {
		render(
			<TimeInStateBadge
				currentStateEnteredAt={null}
				currentStateName="In Progress"
				isStale={true}
				staleReasons={[{ kind: "BlockedDuration", days: 4 }]}
				now={now}
			/>,
		);

		expect(screen.getByText("—")).toBeInTheDocument();
		expect(screen.queryByTestId("time-in-state-stale")).not.toBeInTheDocument();
	});
});

// The badge shows Lighthouse's own answer. No staleness threshold is handed to it any more, so none
// of these pass one.
describe("TimeInStateBadge shows the staleness Lighthouse decided", () => {
	const now = new Date("2026-05-25T12:00:00Z");
	const threeDaysInReview = new Date("2026-05-23T08:00:00Z");

	// @contract-shape:pure-function
	test("is red when Lighthouse marks the Work Item stale, even with no threshold given", () => {
		render(
			<TimeInStateBadge
				currentStateEnteredAt={threeDaysInReview}
				currentStateName="Review"
				isStale={true}
				staleReasons={[{ kind: "TimeInState", days: 3, stateName: "Review" }]}
				now={now}
			/>,
		);

		const stale = screen.getByTestId("time-in-state-stale");
		expect(stale).toHaveTextContent(/^3d in Review$/);
		expect(stale).toHaveStyle({ color: "rgb(211, 47, 47)" });
	});

	// @error @contract-shape:pure-function
	test("is not red when Lighthouse says the Work Item is not stale, however long it has sat", () => {
		render(
			<TimeInStateBadge
				currentStateEnteredAt={new Date("2026-04-25T08:00:00Z")}
				currentStateName="Review"
				isStale={false}
				staleReasons={[]}
				now={now}
			/>,
		);

		expect(screen.queryByTestId("time-in-state-stale")).not.toBeInTheDocument();
		expect(screen.getByText(/^31d in Review$/)).toBeInTheDocument();
	});

	// @error @contract-shape:unbounded-preservation
	test("is not red when Lighthouse does not say, as an older Lighthouse does not", () => {
		render(
			<TimeInStateBadge
				currentStateEnteredAt={new Date("2026-04-25T08:00:00Z")}
				currentStateName="Review"
				now={now}
			/>,
		);

		expect(screen.queryByTestId("time-in-state-stale")).not.toBeInTheDocument();
		expect(screen.getByText(/^31d in Review$/)).toBeInTheDocument();
	});

	// @error @contract-shape:pure-function
	test("a Blocked Work Item not yet stale stays un-red even though Lighthouse names its time in state", () => {
		render(
			<TimeInStateBadge
				currentStateEnteredAt={new Date("2026-05-14T08:00:00Z")}
				currentStateName="Review"
				isStale={false}
				staleReasons={[
					{ kind: "TimeInStateWhileBlocked", days: 12, stateName: "Review" },
				]}
				now={now}
			/>,
		);

		expect(screen.queryByTestId("time-in-state-stale")).not.toBeInTheDocument();
		expect(screen.getByText(/^12d in Review$/)).toBeInTheDocument();
	});

	// @contract-shape:pure-function
	test.each([
		[
			"blocked time",
			[{ kind: "BlockedDuration" as const, days: 6 }],
			"3d in Review — Blocked 6d",
		],
		[
			"time in state",
			[{ kind: "TimeInState" as const, days: 12, stateName: "Review" }],
			"3d in Review — 12d in Review",
		],
		[
			"blocked time and the time in state beside it",
			[
				{ kind: "BlockedDuration" as const, days: 6 },
				{
					kind: "TimeInStateWhileBlocked" as const,
					days: 12,
					stateName: "Review",
				},
			],
			"3d in Review — Blocked 6d; 12d in Review",
		],
	])(
		"its tooltip gives Lighthouse's reason: %s",
		(_reason, staleReasons, expectedTooltip) => {
			render(
				<TimeInStateBadge
					currentStateEnteredAt={threeDaysInReview}
					currentStateName="Review"
					isStale={true}
					staleReasons={staleReasons}
					now={now}
				/>,
			);

			expect(screen.getByTestId("time-in-state-stale")).toHaveAttribute(
				"aria-label",
				expectedTooltip,
			);
		},
	);

	test("a reason that names no state reads as the state the Work Item is in", () => {
		render(
			<TimeInStateBadge
				currentStateEnteredAt={threeDaysInReview}
				currentStateName="Review"
				isStale={true}
				staleReasons={[{ kind: "TimeInState", days: 12 }]}
				now={now}
			/>,
		);

		expect(screen.getByTestId("time-in-state-stale")).toHaveAttribute(
			"aria-label",
			"3d in Review — 12d in Review",
		);
	});

	test("a reason of a kind this Lighthouse does not know is left out of the tooltip", () => {
		render(
			<TimeInStateBadge
				currentStateEnteredAt={threeDaysInReview}
				currentStateName="Review"
				isStale={true}
				staleReasons={[
					{ kind: "BlockedDuration", days: 6 },
					{ kind: "SomethingNewer" as unknown as StaleReasonKind, days: 9 },
				]}
				now={now}
			/>,
		);

		expect(screen.getByTestId("time-in-state-stale")).toHaveAttribute(
			"aria-label",
			"3d in Review — Blocked 6d",
		);
	});
});

describe("TimeInStateBadge counts to the reader's own calendar day", () => {
	// A range end arrives as the reader's local midnight. In Zurich that instant is still the day
	// before in UTC, while Lighthouse is asked about the reader's local day.
	const rangeEnd = new Date(2026, 4, 25);

	test("the suite runs east of Greenwich, so a local midnight is not a UTC midnight", () => {
		expect(rangeEnd.getTimezoneOffset()).toBeLessThan(0);
	});

	test("a Work Item that entered its state on 15 May has been in it 11 days on 25 May, as Lighthouse reports", () => {
		const serverReportedDays = 11;

		render(
			<TimeInStateBadge
				currentStateEnteredAt={new Date("2026-05-15T12:00:00Z")}
				currentStateName="Review"
				isStale={true}
				staleReasons={[
					{
						kind: "TimeInState",
						days: serverReportedDays,
						stateName: "Review",
					},
				]}
				now={rangeEnd}
			/>,
		);

		expect(screen.getByTestId("time-in-state-stale")).toHaveAttribute(
			"aria-label",
			`${serverReportedDays}d in Review — ${serverReportedDays}d in Review`,
		);
	});

	test("daysInState counts to the local day of the instant it is given", () => {
		expect(daysInState(new Date("2026-05-15T12:00:00Z"), rangeEnd)).toBe(11);
	});

	test("daysInState counts no days for a Work Item whose state entry is unknown", () => {
		expect(daysInState(null, rangeEnd)).toBe(0);
	});
});
