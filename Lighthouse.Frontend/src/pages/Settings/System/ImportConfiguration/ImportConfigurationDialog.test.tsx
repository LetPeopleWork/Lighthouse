import { fireEvent, render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import ImportConfigurationDialog from "./ImportConfigurationDialog";

describe("ImportConfigurationDialog", () => {
	it("renders the dialog when open is true", () => {
		render(<ImportConfigurationDialog open={true} onClose={vi.fn()} />);

		expect(
			screen.getByTestId("import-configuration-dialog"),
		).toBeInTheDocument();
		expect(screen.getByText("Import Configuration")).toBeInTheDocument();
	});

	it("calls onClose when Cancel button is clicked", () => {
		const onClose = vi.fn();

		render(<ImportConfigurationDialog open={true} onClose={onClose} />);

		fireEvent.click(screen.getByText("Cancel"));
		expect(onClose).toHaveBeenCalled();
	});

	it("is not rendered when open is false", () => {
		render(<ImportConfigurationDialog open={false} onClose={vi.fn()} />);

		expect(
			screen.queryByTestId("import-configuration-dialog"),
		).not.toBeInTheDocument();
	});
});
