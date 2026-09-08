import type React from "react";

export interface DateWindowStepperProps {
	/** How far one click moves the window. Named in the accessible label, because below the
	 *  `sm` breakpoint the header hides its date text and the icon is the only visible signal. */
	stepDays: number;
	canStepForward: boolean;
	onStep: (direction: -1 | 1) => void;
}

const DateWindowStepper: React.FC<DateWindowStepperProps> = ({
	stepDays,
	canStepForward,
	onStep,
}) => {
	throw new Error(
		`DateWindowStepper(${stepDays}, ${canStepForward}, ${typeof onStep}) is not implemented`,
	);
};

export default DateWindowStepper;
