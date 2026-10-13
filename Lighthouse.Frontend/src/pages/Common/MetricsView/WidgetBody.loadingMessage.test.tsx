import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { WidgetBody } from "./WidgetShell";
import type { WidgetStatus } from "./widgetStatus";

const LOADING = "Loading report…";

const renderBody = (
	status: WidgetStatus,
	hasContentToDim: boolean,
	loadingMessage?: string,
) =>
	render(
		<WidgetBody
			widgetKey="report"
			status={status}
			hasContentToDim={hasContentToDim}
			loadingMessage={loadingMessage}
		>
			<div>the chart</div>
		</WidgetBody>,
	);

describe("the loading message beneath a lone spinner", () => {
	it("is said while there is nothing yet to dim", () => {
		renderBody("loading", false, LOADING);

		expect(screen.getByText(LOADING)).toBeVisible();
	});

	it.each<[string, WidgetStatus, boolean]>([
		["the chart is dimmed instead", "loading", true],
		["the chart is shown", "ready", true],
		["the chart could not be loaded", "error", false],
	])("is not said when %s", (_, status, hasContentToDim) => {
		renderBody(status, hasContentToDim, LOADING);

		expect(screen.queryByText(LOADING)).toBeNull();
	});
});
