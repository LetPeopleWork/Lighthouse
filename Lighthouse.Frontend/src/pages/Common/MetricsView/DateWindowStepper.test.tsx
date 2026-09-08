import { fireEvent, render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import DateWindowStepper from "./DateWindowStepper";

describe.skip("DateWindowStepper", () => {
	it("says how far it moves the window, because on a narrow screen the icon is all there is", () => {
		// Below the `sm` breakpoint the header drops its date text and the two icons are the only
		// visible signal, so the step size has to live in the accessible name or it is not stated
		// anywhere a narrow-screen reader can reach it.
		render(
			<DateWindowStepper stepDays={7} canStepForward={true} onStep={vi.fn()} />,
		);

		expect(
			screen.getByRole("button", { name: "Previous 7 days" }),
		).toBeInTheDocument();
		expect(
			screen.getByRole("button", { name: "Next 7 days" }),
		).toBeInTheDocument();
	});

	it("says four weeks when it is a portfolio's stepper", () => {
		render(
			<DateWindowStepper
				stepDays={28}
				canStepForward={true}
				onStep={vi.fn()}
			/>,
		);

		expect(
			screen.getByRole("button", { name: "Previous 28 days" }),
		).toBeInTheDocument();
	});

	it("asks for the period before this one", () => {
		const onStep = vi.fn();
		render(
			<DateWindowStepper stepDays={7} canStepForward={true} onStep={onStep} />,
		);

		fireEvent.click(screen.getByRole("button", { name: "Previous 7 days" }));

		expect(onStep).toHaveBeenCalledWith(-1);
	});

	it("asks for the period after this one", () => {
		const onStep = vi.fn();
		render(
			<DateWindowStepper stepDays={7} canStepForward={true} onStep={onStep} />,
		);

		fireEvent.click(screen.getByRole("button", { name: "Next 7 days" }));

		expect(onStep).toHaveBeenCalledWith(1);
	});

	it("offers no way forward when the window already ends today", () => {
		render(
			<DateWindowStepper
				stepDays={7}
				canStepForward={false}
				onStep={vi.fn()}
			/>,
		);

		expect(screen.getByRole("button", { name: "Next 7 days" })).toBeDisabled();
	});

	it("still offers the way back when the window ends today", () => {
		render(
			<DateWindowStepper
				stepDays={7}
				canStepForward={false}
				onStep={vi.fn()}
			/>,
		);

		expect(
			screen.getByRole("button", { name: "Previous 7 days" }),
		).toBeEnabled();
	});

	it("asks for nothing when the way forward is closed", () => {
		const onStep = vi.fn();
		render(
			<DateWindowStepper stepDays={7} canStepForward={false} onStep={onStep} />,
		);

		fireEvent.click(screen.getByRole("button", { name: "Next 7 days" }));

		expect(onStep).not.toHaveBeenCalled();
	});
});
