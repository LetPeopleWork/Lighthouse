import { render, screen } from "@testing-library/react";
import type { ReactElement } from "react";
import { describe, expect, it } from "vitest";
import type { IWorkItem } from "../../../models/WorkItem";
import { outlierWording } from "../../../utils/charts/outlierWording";
import { outlierHeaderChip } from "./outlierHeaderChip";

const wording = outlierWording({
	workItems: "Work Items",
	sle: "SLE",
	cycleTime: "Cycle Time",
});

const oneOutlier = [{ id: 1, cycleTime: 40 } as IWorkItem];

const chipFor = (disabled: boolean, verdictPending: boolean) =>
	outlierHeaderChip({
		wording,
		outliers: oneOutlier,
		offScaleLines: [],
		disabled,
		verdictPending,
		onOpen: () => {},
		chipRef: null,
	}) as ReactElement;

describe("outlierHeaderChip", () => {
	it("disables the chip when the chart disables it, even with the verdict landed", () => {
		render(chipFor(true, false));

		expect(screen.getByTestId("outlier-chip")).toHaveClass("Mui-disabled");
	});

	it("keeps the chip usable when neither the chart nor a pending verdict disables it", () => {
		render(chipFor(false, false));

		expect(screen.getByTestId("outlier-chip")).not.toHaveClass("Mui-disabled");
	});
});
