import ChevronLeftIcon from "@mui/icons-material/ChevronLeft";
import ChevronRightIcon from "@mui/icons-material/ChevronRight";
import { IconButton, Stack } from "@mui/material";
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
}) => (
	<Stack direction="row" spacing={0.5}>
		<IconButton
			size="small"
			aria-label={`Previous ${stepDays} days`}
			onClick={() => onStep(-1)}
		>
			<ChevronLeftIcon fontSize="small" />
		</IconButton>
		<IconButton
			size="small"
			aria-label={`Next ${stepDays} days`}
			disabled={!canStepForward}
			onClick={() => onStep(1)}
		>
			<ChevronRightIcon fontSize="small" />
		</IconButton>
	</Stack>
);

export default DateWindowStepper;
