import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it } from "vitest";
import { expectLearnMoreToOpen } from "../../../tests/FieldTipsTestKit";
import WidgetShell from "./WidgetShell";

/**
 * The dashboard widgets' info button is about to share its popover with the settings forms. Whatever moves
 * underneath, a widget's info button still opens a popover with its description, its guidance and a "Learn More"
 * to the docs in a new tab, and Escape still closes it.
 */

const DESCRIPTION = "Shows completed items over time";
const DOCS = "https://docs.example.com#throughput";

const aWidgetWithInfo = () =>
	render(
		<WidgetShell
			widgetKey="throughput"
			showTips={true}
			info={{
				description: DESCRIPTION,
				learnMoreUrl: DOCS,
				statusGuidance: {
					sustain: "Healthy trend.",
					observe: "Watch this trend.",
					act: "Take action now.",
				},
			}}
		>
			<div>Content</div>
		</WidgetShell>,
	);

describe("Story 6352 slice 02: a dashboard widget's info popover stays as it is", () => {
	// @error @active-guard @contract-shape:unbounded-preservation
	it("opens with the description, the guidance and Learn More, and Escape closes it", async () => {
		const user = userEvent.setup();
		aWidgetWithInfo();

		await user.click(screen.getByTestId("widget-info-throughput"));
		const popover = await screen.findByRole("presentation");

		expect(popover).toHaveTextContent(DESCRIPTION);
		expect(popover).toHaveTextContent("Healthy trend.");
		expectLearnMoreToOpen(popover, DOCS);

		await user.keyboard("{Escape}");
		await waitFor(() =>
			expect(screen.queryByText(DESCRIPTION)).not.toBeInTheDocument(),
		);
	});
});
