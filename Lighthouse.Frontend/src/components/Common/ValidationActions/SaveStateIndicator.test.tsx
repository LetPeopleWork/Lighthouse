import { fireEvent, render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import SaveStateIndicator, { type SaveState } from "./SaveStateIndicator";

describe("SaveStateIndicator", () => {
	const visibleCopy: Array<[SaveState, string]> = [
		["saving", "Saving…"],
		["saved", "All changes saved"],
		["error", "Couldn't save"],
	];

	for (const [saveState, copy] of visibleCopy) {
		it(`reports "${copy}" when saveState is "${saveState}"`, () => {
			render(<SaveStateIndicator saveState={saveState} canSave={true} />);

			expect(screen.getByText(copy)).toBeInTheDocument();
		});
	}

	it("offers a Retry action that invokes the handler when saving failed", () => {
		const onRetry = vi.fn();
		render(
			<SaveStateIndicator saveState="error" canSave={true} onRetry={onRetry} />,
		);

		fireEvent.click(screen.getByRole("button", { name: "Retry" }));

		expect(onRetry).toHaveBeenCalledTimes(1);
	});

	const hidden: Array<{ saveState: SaveState; canSave: boolean }> = [
		{ saveState: "idle", canSave: true },
		{ saveState: "saving", canSave: false },
		{ saveState: "saved", canSave: false },
		{ saveState: "error", canSave: false },
	];

	for (const { saveState, canSave } of hidden) {
		it(`renders nothing when saveState is "${saveState}" and canSave is ${canSave}`, () => {
			const { container } = render(
				<SaveStateIndicator saveState={saveState} canSave={canSave} />,
			);

			expect(container).toBeEmptyDOMElement();
		});
	}
});
