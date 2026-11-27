import { render, screen } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import QueryTextField from "./QueryTextField";

const LINE_HEIGHT_PX = 10;

const queryOfLines = (count: number) =>
	Array.from({ length: count }, (_, index) => `line ${index + 1}`).join("\n");

describe("QueryTextField", () => {
	beforeEach(() => {
		// jsdom lays nothing out, so a textarea reports the height its lines would take in a browser.
		vi.spyOn(
			HTMLTextAreaElement.prototype,
			"scrollHeight",
			"get",
		).mockImplementation(function (this: HTMLTextAreaElement) {
			return this.value.split("\n").length * LINE_HEIGHT_PX;
		});
	});

	afterEach(() => {
		vi.restoreAllMocks();
	});

	it("starts at four lines, grows with a long query and stops growing at twenty lines", () => {
		const { rerender } = render(
			<QueryTextField label="WIQL Query" value={queryOfLines(1)} />,
		);
		const field = screen.getByRole("textbox", { name: "WIQL Query" });
		expect(field).toHaveStyle({ height: "40px" });

		rerender(<QueryTextField label="WIQL Query" value={queryOfLines(12)} />);
		expect(field).toHaveStyle({ height: "120px" });

		rerender(<QueryTextField label="WIQL Query" value={queryOfLines(35)} />);
		expect(field).toHaveStyle({ height: "200px" });
	});
	it("keeps a read-only query at four lines however long it is", () => {
		render(
			<QueryTextField label="CSV File" value={queryOfLines(35)} readOnly />,
		);
		const field = screen.getByRole("textbox", { name: "CSV File" });
		expect(field).toHaveAttribute("readonly");
		expect(field).toHaveStyle({ height: "40px" });
	});
});
