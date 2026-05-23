import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it } from "vitest";
import FilteredThroughputChip from "./FilteredThroughputChip";

const D5_FALLBACK_SUMMARY =
	"Filter excluded all throughput; showing unfiltered forecast";

describe("FilteredThroughputChip", () => {
	it("renders the chip with the 'Filtered throughput' label when visible is true", () => {
		render(
			<FilteredThroughputChip
				visible={true}
				excludedSummary={'Type = Bug; Tags contains "maintenance"'}
			/>,
		);

		expect(screen.getByText("Filtered throughput")).toBeInTheDocument();
	});

	it("does not render anything when visible is false", () => {
		const { container } = render(
			<FilteredThroughputChip
				visible={false}
				excludedSummary={'Type = Bug; Tags contains "maintenance"'}
			/>,
		);

		expect(container).toBeEmptyDOMElement();
	});

	it("shows the excluded summary in the tooltip on hover", async () => {
		const user = userEvent.setup();
		const summary =
			'Type = Bug; Parent Reference ID = (empty); Tags contains "maintenance"';

		render(<FilteredThroughputChip visible={true} excludedSummary={summary} />);

		await user.hover(screen.getByText("Filtered throughput"));

		expect(await screen.findByRole("tooltip")).toHaveTextContent(summary);
	});

	it("renders the D5 warning copy when excludedSummary indicates fallback to unfiltered", async () => {
		const user = userEvent.setup();

		render(
			<FilteredThroughputChip
				visible={true}
				excludedSummary={D5_FALLBACK_SUMMARY}
			/>,
		);

		const chip = screen
			.getByText("Filtered throughput")
			.closest(".MuiChip-root");
		expect(chip).toHaveClass("MuiChip-colorWarning");

		await user.hover(screen.getByText("Filtered throughput"));
		expect(await screen.findByRole("tooltip")).toHaveTextContent(
			D5_FALLBACK_SUMMARY,
		);
	});
});
