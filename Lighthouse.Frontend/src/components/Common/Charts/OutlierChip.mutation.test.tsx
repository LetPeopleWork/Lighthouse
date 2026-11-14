import { fireEvent, render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import { outlierWording } from "../../../utils/charts/outlierWording";
import OutlierChip from "./OutlierChip";

const wording = outlierWording({
	workItems: "Work Items",
	sle: "SLE",
	cycleTime: "Cycle Time",
});

const chip = (over: Partial<Parameters<typeof OutlierChip>[0]> = {}) => (
	<OutlierChip
		wording={wording}
		count={2}
		aboveTheAxis={null}
		disabled={false}
		onOpen={() => {}}
		chipRef={null}
		{...over}
	/>
);

const phone = (matches: boolean) =>
	vi.stubGlobal("matchMedia", (query: string) => ({
		matches,
		media: query,
		addEventListener: () => {},
		removeEventListener: () => {},
		addListener: () => {},
		removeListener: () => {},
		onchange: null,
		dispatchEvent: () => false,
	}));

describe("OutlierChip", () => {
	it("shows only the count on a phone, but the words while disabled", () => {
		phone(true);
		const { rerender } = render(chip());
		expect(screen.getByTestId("outlier-chip").textContent).toContain("2");
		expect(screen.getByTestId("outlier-chip").textContent).not.toContain(
			"outliers",
		);
		rerender(chip({ disabled: true }));
		expect(screen.getByTestId("outlier-chip").textContent).toBe("Outliers");
		vi.unstubAllGlobals();
	});

	it("drops the words only below the phone breakpoint", () => {
		vi.stubGlobal("matchMedia", (query: string) => ({
			matches: query.replaceAll(" ", "") === "(max-width:599.95px)",
			media: query,
			addEventListener: () => {},
			removeEventListener: () => {},
			addListener: () => {},
			removeListener: () => {},
			onchange: null,
			dispatchEvent: () => false,
		}));
		render(chip());
		expect(screen.getByTestId("outlier-chip").textContent).toBe("2");
		vi.unstubAllGlobals();
	});

	it("shows the words on a wide screen", () => {
		phone(false);
		render(chip());
		expect(screen.getByTestId("outlier-chip").textContent).toBe("2 outliers");
		vi.unstubAllGlobals();
	});

	it("is amber and outlined whether ready or disabled, on a phone or a wide screen", () => {
		const amberOutlined = () =>
			expect(screen.getByTestId("outlier-chip")).toHaveClass(
				"MuiChip-outlined",
				"MuiChip-colorWarning",
			);
		phone(false);
		const { rerender } = render(chip());
		amberOutlined();
		rerender(chip({ disabled: true }));
		amberOutlined();
		phone(true);
		rerender(chip());
		amberOutlined();
		vi.unstubAllGlobals();
	});

	it("reports each click once, opens nothing of its own, and does nothing while disabled", () => {
		phone(false);
		const onOpen = vi.fn();
		const { rerender } = render(chip({ onOpen }));
		const button = screen.getByTestId("outlier-chip");
		expect(button.getAttribute("aria-haspopup")).toBeNull();
		expect(button.getAttribute("aria-expanded")).toBeNull();

		fireEvent.click(button);
		expect(onOpen).toHaveBeenCalledTimes(1);
		expect(screen.queryByRole("presentation")).toBeNull();

		fireEvent.click(button);
		expect(onOpen).toHaveBeenCalledTimes(2);

		rerender(chip({ onOpen, disabled: true }));
		fireEvent.click(screen.getByTestId("outlier-chip"));
		expect(onOpen).toHaveBeenCalledTimes(2);
		vi.unstubAllGlobals();
	});

	it("gives the loading chip no tooltip and the ready chip its accessible name as the tooltip", async () => {
		phone(false);
		const { rerender } = render(chip({ disabled: true }));
		fireEvent.mouseOver(screen.getByTestId("outlier-chip"));
		expect(screen.queryByRole("tooltip")).toBeNull();
		rerender(chip());
		fireEvent.mouseOver(screen.getByTestId("outlier-chip"));
		expect((await screen.findByRole("tooltip")).textContent).toBe(
			"2 outliers not drawn on the chart. Show the list.",
		);
		vi.unstubAllGlobals();
	});

	it("names the lines above the axis in the accessible name only when it is given one", () => {
		phone(false);
		const { rerender } = render(chip());
		expect(screen.getByTestId("outlier-chip").getAttribute("aria-label")).toBe(
			"2 outliers not drawn on the chart. Show the list.",
		);
		rerender(chip({ aboveTheAxis: "Above the axis: 95% at 33 days." }));
		expect(screen.getByTestId("outlier-chip").getAttribute("aria-label")).toBe(
			"2 outliers not drawn on the chart. Above the axis: 95% at 33 days. Show the list.",
		);
		rerender(chip({ disabled: true, aboveTheAxis: "Above the axis: x." }));
		expect(screen.getByTestId("outlier-chip").getAttribute("aria-label")).toBe(
			"Outliers, loading",
		);
		vi.unstubAllGlobals();
	});
});
