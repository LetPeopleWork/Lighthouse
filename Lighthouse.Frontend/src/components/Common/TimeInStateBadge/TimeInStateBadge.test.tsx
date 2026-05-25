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
});
