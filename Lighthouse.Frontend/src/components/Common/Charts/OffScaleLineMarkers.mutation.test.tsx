import { render } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import OffScaleLineMarkers from "./OffScaleLineMarkers";

vi.mock("@mui/x-charts", () => ({
	useDrawingArea: () => ({ left: 50, top: 100, width: 400 }),
}));

const render_ = (
	markers: Parameters<typeof OffScaleLineMarkers>[0]["markers"],
) =>
	render(
		<svg role="img" aria-label="plot">
			<OffScaleLineMarkers markers={markers} />
		</svg>,
	);

describe("OffScaleLineMarkers", () => {
	it("draws nothing without markers", () => {
		const { container } = render_([]);
		expect(container.querySelector("g")).toBeNull();
	});

	it("puts each marker at the right edge, stacked upward above the plot", () => {
		const { container } = render_([
			{ key: "a", text: "85%: 20 days", color: "red", row: 0 },
			{
				key: "b",
				text: "Target: 90% @ 30 days or less",
				color: "blue",
				row: 2,
			},
		]);
		const texts = container.querySelectorAll("text");
		expect(texts).toHaveLength(2);
		expect(texts[0].getAttribute("x")).toBe("450");
		expect(texts[0].getAttribute("y")).toBe("94");
		expect(texts[1].getAttribute("y")).toBe("62");
		expect(texts[0].textContent).toBe("▲ 85%: 20 days");
		expect(texts[1].getAttribute("fill")).toBe("blue");
		expect(container.querySelector("g")?.getAttribute("aria-hidden")).toBe(
			"true",
		);
	});
});
