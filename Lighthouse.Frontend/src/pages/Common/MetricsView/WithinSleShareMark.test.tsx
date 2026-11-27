import type { MarkElementProps } from "@mui/x-charts";
import { render } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { FilledInDaysContext, WithinSleShareMark } from "./WithinSleShareMark";

const markOfDayZero = (hidden: boolean) =>
	({
		x: 5,
		y: 6,
		dataIndex: 0,
		color: "rgb(1, 2, 3)",
		hidden,
	}) as unknown as MarkElementProps;

describe("WithinSleShareMark", () => {
	it("draws a solid marker in the line colour for a day that was not filled in", () => {
		const { container } = render(
			<svg role="img" aria-label="Within SLE day">
				<WithinSleShareMark {...markOfDayZero(false)} />
			</svg>,
		);

		const marker = container.querySelector("circle");
		expect(marker).not.toBeNull();
		expect(marker?.getAttribute("fill")).toBe("rgb(1, 2, 3)");
	});

	it("draws an open marker for a day filled in from history", () => {
		const { container } = render(
			<svg role="img" aria-label="Within SLE day">
				<FilledInDaysContext.Provider value={[true]}>
					<WithinSleShareMark {...markOfDayZero(false)} />
				</FilledInDaysContext.Provider>
			</svg>,
		);

		expect(container.querySelector("circle")?.getAttribute("fill")).not.toBe(
			"rgb(1, 2, 3)",
		);
	});

	it("draws nothing for a hidden series", () => {
		const { container } = render(
			<svg role="img" aria-label="Within SLE day">
				<WithinSleShareMark {...markOfDayZero(true)} />
			</svg>,
		);

		expect(container.querySelector("circle")).toBeNull();
	});
});
