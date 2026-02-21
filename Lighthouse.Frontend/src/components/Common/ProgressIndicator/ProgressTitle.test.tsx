import { render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import ProgressTitle from "./ProgressTitle";

describe("ProgressTitle", () => {
	it("renders plain text when using default feature size", () => {
		render(
			<ProgressTitle
				title="Total"
				isUsingDefaultFeatureSize={true}
				onShowDetails={vi.fn().mockResolvedValue(undefined)}
			/>,
		);

		expect(screen.getByText("Total")).toBeInTheDocument();
		expect(
			screen.queryByRole("button", { name: "Total" }),
		).not.toBeInTheDocument();
	});

	it("renders a button and calls onShowDetails when clicked", async () => {
		const { default: userEvent } = await import("@testing-library/user-event");
		const user = userEvent.setup();
		const onShowDetails = vi.fn().mockResolvedValue(undefined);

		render(
			<ProgressTitle
				title="Overall Progress"
				isUsingDefaultFeatureSize={false}
				onShowDetails={onShowDetails}
			/>,
		);

		await user.click(screen.getByRole("button", { name: "Overall Progress" }));

		expect(onShowDetails).toHaveBeenCalledTimes(1);
	});
});
