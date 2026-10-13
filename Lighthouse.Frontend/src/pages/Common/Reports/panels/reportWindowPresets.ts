import type { IReportOwner } from "../../../../models/Reports/Report";

// One-click shortcuts for a window's length, not limits: a length the server's bounds refuse is refused
// whether it was clicked or typed.
const REPORT_WINDOW_PRESETS: Record<IReportOwner["kind"], readonly number[]> = {
	team: [30, 90],
	portfolio: [90, 180],
};

export const reportWindowPresetsFor = (
	owner: IReportOwner,
): readonly number[] => REPORT_WINDOW_PRESETS[owner.kind];
