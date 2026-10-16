import { render, screen } from "@testing-library/react";
import { describe, expect, test } from "vitest";
import TimeInStateBadge from "./TimeInStateBadge";

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

	test.each([
		// [enteredAt, stalenessThresholdDays, blockedThresholdDays, expectsStale]
		["2026-05-23T23:00:00Z", 3, 0, false],
		["2026-05-23T23:00:00Z", 4, 0, false],
		["2026-05-23T23:00:00Z", 2, 0, true],
		["2026-05-23T23:00:00Z", 1, 0, true],
	])(
		"applies the stale treatment only when days strictly exceed the threshold for %s with threshold %d",
		(enteredAt, stalenessThresholdDays, blockedThresholdDays, expectsStale) => {
			render(
				<TimeInStateBadge
					currentStateEnteredAt={new Date(enteredAt)}
					currentStateName="In Progress"
					stalenessThresholdDays={stalenessThresholdDays}
					blockedStalenessThresholdDays={blockedThresholdDays}
					now={now}
				/>,
			);

			const stale = screen.queryByTestId("time-in-state-stale");
			if (expectsStale) {
				expect(stale).toBeInTheDocument();
				expect(stale).toHaveTextContent("3d in In Progress");
				expect(stale).toHaveStyle({ color: "rgb(211, 47, 47)" });
			} else {
				expect(stale).not.toBeInTheDocument();
				expect(screen.getByText("3d in In Progress")).toBeInTheDocument();
			}
		},
	);

	test.each([
		[0, 0],
		[undefined, undefined],
	])(
		"never applies the stale treatment when the threshold is %s (highlighting disabled)",
		(stalenessThresholdDays, blockedStalenessThresholdDays) => {
			render(
				<TimeInStateBadge
					currentStateEnteredAt={new Date("2026-05-15T12:00:00Z")}
					currentStateName="In Progress"
					stalenessThresholdDays={stalenessThresholdDays}
					blockedStalenessThresholdDays={blockedStalenessThresholdDays}
					now={now}
				/>,
			);

			expect(
				screen.queryByTestId("time-in-state-stale"),
			).not.toBeInTheDocument();
			expect(screen.getByText("11d in In Progress")).toBeInTheDocument();
		},
	);

	test("renders an em dash and no stale treatment when there is no entered date even past a threshold", () => {
		render(
			<TimeInStateBadge
				currentStateEnteredAt={null}
				currentStateName="In Progress"
				stalenessThresholdDays={1}
				blockedStalenessThresholdDays={0}
				now={now}
			/>,
		);

		expect(screen.getByText("—")).toBeInTheDocument();
		expect(screen.queryByTestId("time-in-state-stale")).not.toBeInTheDocument();
	});

	test("does not apply the stale treatment to a blocked item over the staleness threshold (blocked has precedence with no blockedThreshold)", () => {
		render(
			<TimeInStateBadge
				currentStateEnteredAt={new Date("2026-05-23T23:00:00Z")}
				currentStateName="In Progress"
				stalenessThresholdDays={1}
				isBlocked={true}
				now={now}
			/>,
		);

		expect(screen.queryByTestId("time-in-state-stale")).not.toBeInTheDocument();
		expect(screen.getByText("3d in In Progress")).toBeInTheDocument();
	});

	test("marks a blocked item as stale when blocked duration exceeds blockedStalenessThresholdDays", () => {
		render(
			<TimeInStateBadge
				currentStateEnteredAt={new Date("2026-05-20T12:00:00Z")}
				currentStateName="In Progress"
				stalenessThresholdDays={10}
				blockedStalenessThresholdDays={2}
				isBlocked={true}
				blockedSince={"2026-05-22T12:00:00Z"}
				now={now}
			/>,
		);

		const stale = screen.queryByTestId("time-in-state-stale");
		expect(stale).toBeInTheDocument();
		expect(stale).toHaveStyle({ color: "rgb(211, 47, 47)" });
	});

	test("renders stale reasons in an accessible label on the stale badge", () => {
		render(
			<TimeInStateBadge
				currentStateEnteredAt={new Date("2026-05-20T12:00:00Z")}
				currentStateName="In Progress"
				stalenessThresholdDays={1}
				isBlocked={false}
				now={now}
			/>,
		);

		const stale = screen.getByTestId("time-in-state-stale");
		expect(stale).toHaveAttribute("aria-label");
		expect(stale.getAttribute("aria-label")).toContain("In Progress");
	});

	test("renders blocked-duration driver and context-time-in-state reasons when both triggers fire", () => {
		render(
			<TimeInStateBadge
				currentStateEnteredAt={new Date("2026-05-20T12:00:00Z")}
				currentStateName="In Progress"
				stalenessThresholdDays={1}
				blockedStalenessThresholdDays={2}
				isBlocked={true}
				blockedSince={"2026-05-22T12:00:00Z"}
				now={now}
			/>,
		);

		const stale = screen.getByTestId("time-in-state-stale");
		expect(stale).toBeInTheDocument();
		expect(stale).toHaveStyle({ color: "rgb(211, 47, 47)" });
		const label = stale.getAttribute("aria-label") ?? "";
		// blocked-duration reason included
		expect(label).toContain("Blocked");
	});

	test("cross-surface consistency: same emphasis colour as BaseMetricsView uses for stale items", () => {
		render(
			<TimeInStateBadge
				currentStateEnteredAt={new Date("2026-05-20T12:00:00Z")}
				currentStateName="In Progress"
				stalenessThresholdDays={1}
				isBlocked={false}
				now={now}
			/>,
		);

		const stale = screen.getByTestId("time-in-state-stale");
		expect(stale).toHaveStyle({ color: "rgb(211, 47, 47)" });
	});
});

// The badge shows Lighthouse's own answer. No staleness threshold is handed to it any more, so none
// of these pass one. Pending until DELIVER switches the badge over.
describe("TimeInStateBadge shows the staleness Lighthouse decided", () => {
	const now = new Date("2026-05-25T12:00:00Z");
	const threeDaysInReview = new Date("2026-05-23T08:00:00Z");

	// @contract-shape:pure-function
	test.skip("is red when Lighthouse marks the Work Item stale, even with no threshold given", () => {
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
	test.skip("is not red when Lighthouse says the Work Item is not stale, however long it has sat", () => {
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
	test.skip("is not red when Lighthouse does not say, as an older Lighthouse does not", () => {
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
	test.skip("a Blocked Work Item not yet stale stays un-red even though Lighthouse names its time in state", () => {
		render(
			<TimeInStateBadge
				currentStateEnteredAt={new Date("2026-05-14T08:00:00Z")}
				currentStateName="Review"
				isBlocked={true}
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
	test.skip.each([
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
});
