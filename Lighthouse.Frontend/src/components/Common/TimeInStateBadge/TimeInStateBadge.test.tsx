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
	])("renders days in current state for entered date %s", (enteredAt, expectedText) => {
		render(
			<TimeInStateBadge
				currentStateEnteredAt={new Date(enteredAt)}
				currentStateName="In Progress"
				now={now}
			/>,
		);

		expect(screen.getByText(expectedText)).toBeInTheDocument();
	});

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
		["2026-05-23T23:00:00Z", 3, false],
		["2026-05-23T23:00:00Z", 4, false],
		["2026-05-23T23:00:00Z", 2, true],
		["2026-05-23T23:00:00Z", 1, true],
	])("applies the stale treatment only when days strictly exceed the threshold for %s with threshold %d", (enteredAt, stalenessThresholdDays, expectsStale) => {
		render(
			<TimeInStateBadge
				currentStateEnteredAt={new Date(enteredAt)}
				currentStateName="In Progress"
				stalenessThresholdDays={stalenessThresholdDays}
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
	});

	test.each([
		[0],
		[undefined],
	])("never applies the stale treatment when the threshold is %s (highlighting disabled)", (stalenessThresholdDays) => {
		render(
			<TimeInStateBadge
				currentStateEnteredAt={new Date("2026-05-15T12:00:00Z")}
				currentStateName="In Progress"
				stalenessThresholdDays={stalenessThresholdDays}
				now={now}
			/>,
		);

		expect(screen.queryByTestId("time-in-state-stale")).not.toBeInTheDocument();
		expect(screen.getByText("11d in In Progress")).toBeInTheDocument();
	});

	test("renders an em dash and no stale treatment when there is no entered date even past a threshold", () => {
		render(
			<TimeInStateBadge
				currentStateEnteredAt={null}
				currentStateName="In Progress"
				stalenessThresholdDays={1}
				now={now}
			/>,
		);

		expect(screen.getByText("—")).toBeInTheDocument();
		expect(screen.queryByTestId("time-in-state-stale")).not.toBeInTheDocument();
	});

	test("does not apply the stale treatment to a blocked item over the threshold (blocked precedence)", () => {
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
});
