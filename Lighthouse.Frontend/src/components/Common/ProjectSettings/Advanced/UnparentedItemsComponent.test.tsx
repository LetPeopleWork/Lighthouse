import { fireEvent, render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import { createMockProjectSettings } from "../../../../tests/TestDataProvider";
import UnparentedItemsComponent from "./UnparentedItemsComponent";

describe("UnparentedItemsComponent", () => {
	const initialSettings = createMockProjectSettings();

	const mockOnProjectSettingsChange = vi.fn();

	beforeEach(() => {
		vi.clearAllMocks();
	});

	it("renders correctly with initial settings", () => {
		render(
			<UnparentedItemsComponent
				projectSettings={initialSettings}
				onProjectSettingsChange={mockOnProjectSettingsChange}
			/>,
		);

		expect(screen.getByLabelText(/Unparented Work Items Query/i)).toHaveValue(
			"Unparented Query",
		);
	});

	it("calls onProjectSettingsChange with correct arguments when unparentedItemsQuery changes", () => {
		render(
			<UnparentedItemsComponent
				projectSettings={initialSettings}
				onProjectSettingsChange={mockOnProjectSettingsChange}
			/>,
		);

		fireEvent.change(screen.getByLabelText(/Unparented Work Items Query/i), {
			target: { value: "Updated Query" },
		});

		expect(mockOnProjectSettingsChange).toHaveBeenCalledWith(
			"unparentedItemsQuery",
			"Updated Query",
		);
	});
});
