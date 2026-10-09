import { createTheme, ThemeProvider } from "@mui/material/styles";
import { render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { type ReactNode, useState } from "react";
import { describe, expect, it, vi } from "vitest";
import ThroughputChartFilterToggle from "../../../components/Common/Charts/ThroughputChart/ThroughputChartFilterToggle";
import type { IWorkItem, StateCategory } from "../../../models/WorkItem";
import WidgetShell, {
	COULD_NOT_LOAD_MESSAGE,
	type WidgetShellProps,
} from "./WidgetShell";
import { useReportWidgetStatus, type WidgetStatus } from "./widgetStatus";

vi.mock("../../../components/Common/WorkItemsDialog/WorkItemsDialog", () => ({
	default: ({ open }: { open: boolean }) =>
		open ? <div data-testid="work-items-dialog" /> : null,
}));

vi.mock("../../../services/TerminologyContext", () => ({
	useTerminology: () => ({
		getTerm: (key: string) => (key === "throughput" ? "Throughput" : key),
	}),
}));

const COULD_NOT_LOAD =
	"This chart couldn't be loaded. Change the dates or reload to try again.";

const KEY = "throughput";

const oneItem: IWorkItem = {
	id: 1,
	name: "Item 1",
	state: "Done",
	stateCategory: "Done" as StateCategory,
	type: "Story",
	referenceId: "ITEM-1",
	url: "https://example.com/work/1",
	startedDate: new Date("2026-01-01"),
	closedDate: new Date("2026-01-10"),
	cycleTime: 9,
	workItemAge: 9,
	parentWorkItemReference: "",
	isBlocked: false,
};

const fullHeader: Omit<WidgetShellProps, "children" | "widgetKey"> = {
	title: "Throughput Run Chart",
	info: {
		description: "How many Work Items were finished each day",
		learnMoreUrl: "https://docs.lighthouse.example/throughput",
	},
	header: { ragStatus: "green", tipText: "Steady" },
	trend: { direction: "up", metricLabel: "Throughput" },
	viewData: { title: "Completed Work Items", items: [oneItem] },
};

function renderFrame(
	props: Partial<WidgetShellProps> = {},
	children: ReactNode = <div data-testid="the-chart">chart</div>,
) {
	return render(
		<WidgetShell widgetKey={KEY} {...fullHeader} {...props}>
			{children}
		</WidgetShell>,
	);
}

const frame = () => screen.getByTestId(`widget-shell-${KEY}`);
const body = () => screen.getByTestId(`widget-shell-body-${KEY}`);

// A chart that fetches for itself has to stay mounted to keep fetching, so "gone" may mean
// hidden rather than removed. Either satisfies the reader, who must not see it.
function expectOutOfSight(testId: string) {
	const element = screen.queryByTestId(testId);
	if (element !== null) {
		expect(element).not.toBeVisible();
	}
}

function fadedAncestorOf(element: HTMLElement): HTMLElement | null {
	for (
		let node: HTMLElement | null = element;
		node !== null && node !== document.body;
		node = node.parentElement
	) {
		const opacity = getComputedStyle(node).opacity;
		if (opacity !== "" && Number(opacity) < 1) return node;
	}
	return null;
}

describe("a chart's frame while its data is current", () => {
	it("a frame left at its defaults shows its chart and header as before", () => {
		renderFrame();

		expect(screen.getByTestId("the-chart")).toBeInTheDocument();
		expect(screen.getByTestId(`widget-rag-${KEY}`)).toBeInTheDocument();
		expect(within(frame()).queryByRole("progressbar")).not.toBeInTheDocument();
	});

	it("a frame nobody gave a status says it is ready", () => {
		renderFrame();

		expect(frame()).toHaveAttribute("data-widget-status", "ready");
		expect(frame()).not.toHaveAttribute("aria-busy", "true");
	});

	it("a chart that has its data shows at full strength with nothing over it", async () => {
		renderFrame({ status: "ready", hasContentToDim: true });

		expect(frame()).toHaveAttribute("data-widget-status", "ready");
		expect(within(frame()).queryByRole("progressbar")).not.toBeInTheDocument();
		expect(screen.getByTestId(`widget-view-data-${KEY}`)).toBeEnabled();
		await userEvent.setup().click(screen.getByTestId("the-chart"));
	});
});

describe("a chart's frame while it is behind the selected window", () => {
	it("keeps the older chart on screen, dimmed, with a spinner over it", () => {
		renderFrame({ status: "loading", hasContentToDim: true });

		expect(frame()).toHaveAttribute("data-widget-status", "loading");
		expect(frame()).toHaveAttribute("aria-busy", "true");
		expect(screen.getByTestId("the-chart")).toBeInTheDocument();
		expect(body()).toHaveStyle({ opacity: "0.4" });
		expect(within(frame()).getByRole("progressbar")).toHaveStyle({
			width: "24px",
		});
	});

	it("keeps the title, rating and trend at full strength, outside anything that is faded", () => {
		renderFrame({ status: "loading", hasContentToDim: true });

		for (const headerPart of [
			screen.getByText("Throughput Run Chart"),
			screen.getByTestId(`widget-rag-${KEY}`),
			screen.getByTestId(`widget-trend-${KEY}`),
		]) {
			expect(fadedAncestorOf(headerPart)).toBeNull();
		}
	});

	it("lets nobody point at the dimmed chart, nor open its data", async () => {
		renderFrame({ status: "loading", hasContentToDim: true });

		expect(body()).toHaveStyle({ pointerEvents: "none" });
		await expect(
			userEvent.setup().click(screen.getByTestId("the-chart")),
		).rejects.toThrow(/pointer-events: none/);
		expect(screen.getByTestId(`widget-view-data-${KEY}`)).toBeDisabled();
	});

	it("a switch inside the chart does not respond while the chart is loading", async () => {
		const onFilterChange = vi.fn();
		renderFrame(
			{ status: "loading", hasContentToDim: true },
			<div>
				<div data-testid="the-chart">chart</div>
				<ThroughputChartFilterToggle
					isPremium
					hasFilter
					onChange={onFilterChange}
				/>
			</div>,
		);

		await expect(
			userEvent.setup().click(screen.getByLabelText("Use filtered Throughput")),
		).rejects.toThrow(/pointer-events: none/);
		expect(onFilterChange).not.toHaveBeenCalled();
	});

	it("still explains the chart when the reader asks", async () => {
		renderFrame({ status: "loading", hasContentToDim: true });

		await userEvent.setup().click(screen.getByTestId(`widget-info-${KEY}`));

		expect(
			await screen.findByText("How many Work Items were finished each day"),
		).toBeInTheDocument();
	});

	it("with no older chart to dim, holds a lone spinner under just the title and info", () => {
		renderFrame({ status: "loading", hasContentToDim: false }, null);

		expect(frame()).toHaveAttribute("data-widget-status", "loading");
		expect(within(frame()).getByRole("progressbar")).toHaveStyle({
			width: "24px",
		});
		expect(screen.getByText("Throughput Run Chart")).toBeInTheDocument();
		expect(screen.getByTestId(`widget-info-${KEY}`)).toBeInTheDocument();
		expect(screen.queryByTestId(`widget-rag-${KEY}`)).not.toBeInTheDocument();
		expect(screen.queryByTestId(`widget-trend-${KEY}`)).not.toBeInTheDocument();
		expect(
			screen.queryByTestId(`widget-view-data-${KEY}`),
		).not.toBeInTheDocument();
	});

	it.each(["light", "dark"] as const)(
		"is dimmed to 40 percent under a spinner in the %s theme",
		(mode) => {
			render(
				<ThemeProvider theme={createTheme({ palette: { mode } })}>
					<WidgetShell
						widgetKey={KEY}
						{...fullHeader}
						status="loading"
						hasContentToDim
					>
						<div data-testid="the-chart">chart</div>
					</WidgetShell>
				</ThemeProvider>,
			);

			expect(frame()).toHaveAttribute("data-widget-status", "loading");
			expect(body()).toHaveStyle({ opacity: "0.4" });
			expect(within(frame()).getByRole("progressbar")).toBeInTheDocument();
		},
	);
});

describe("a chart's frame when its data could not be loaded", () => {
	it("the could-not-load message reads exactly as agreed", () => {
		expect(COULD_NOT_LOAD_MESSAGE).toBe(COULD_NOT_LOAD);
	});

	it("removes the chart and says so in plain words, with a warning icon", () => {
		renderFrame({ status: "error", hasContentToDim: true });

		expect(frame()).toHaveAttribute("data-widget-status", "error");
		expect(frame()).not.toHaveAttribute("aria-busy", "true");
		expectOutOfSight("the-chart");
		expect(within(frame()).queryByRole("progressbar")).not.toBeInTheDocument();
		const message = screen.getByText(COULD_NOT_LOAD);
		expect(message).toBeInTheDocument();
		expect(within(body()).getByTestId("WarningAmberIcon")).toBeInTheDocument();
	});

	it("shows no rating, trend or data for the chart that is gone, only its title and info", () => {
		renderFrame({ status: "error", hasContentToDim: true });

		expect(screen.getByText("Throughput Run Chart")).toBeInTheDocument();
		expect(screen.getByTestId(`widget-info-${KEY}`)).toBeInTheDocument();
		expect(screen.queryByTestId(`widget-rag-${KEY}`)).not.toBeInTheDocument();
		expect(screen.queryByTestId(`widget-trend-${KEY}`)).not.toBeInTheDocument();
		expect(
			screen.queryByTestId(`widget-view-data-${KEY}`),
		).not.toBeInTheDocument();
	});

	it("offers no retry: changing the dates or reloading is the way back", () => {
		renderFrame({ status: "error", hasContentToDim: true });

		const buttons = within(frame()).getAllByRole("button");
		expect(buttons).toEqual([screen.getByTestId(`widget-info-${KEY}`)]);
	});
});

function ChartWithASetting() {
	const [isOn, setIsOn] = useState(false);
	return (
		<button type="button" onClick={() => setIsOn((was) => !was)}>
			{isOn ? "Setting on" : "Setting off"}
		</button>
	);
}

describe("a chart's own settings across a reload", () => {
	it.each<[string, WidgetStatus, boolean]>([
		["dims behind the window", "loading", true],
		["waits with nothing to dim", "loading", false],
		["could not be loaded", "error", true],
	])(
		"a chart keeps what the reader set inside it while it %s and comes back",
		async (_, inBetween, hasContentToDim) => {
			const shellAt = (status: WidgetStatus) => (
				<WidgetShell
					widgetKey={KEY}
					{...fullHeader}
					status={status}
					hasContentToDim={hasContentToDim}
				>
					<ChartWithASetting />
				</WidgetShell>
			);
			const { rerender } = render(shellAt("ready"));
			await userEvent
				.setup()
				.click(screen.getByRole("button", { name: "Setting off" }));

			rerender(shellAt(inBetween));
			rerender(shellAt("ready"));

			expect(
				screen.getByRole("button", { name: "Setting on" }),
			).toBeInTheDocument();
		},
	);
});

function SelfFetchingChart({ status }: { readonly status: WidgetStatus }) {
	useReportWidgetStatus(status);
	return <div data-testid="self-fetching-chart">series</div>;
}

describe("a chart the page feeds", () => {
	it.each<WidgetStatus>(["loading", "error", "ready"])(
		"with nothing inside reporting for itself, the frame reads what the page says: %s",
		(fromPage) => {
			renderFrame({ status: fromPage, hasContentToDim: true });

			expect(frame()).toHaveAttribute("data-widget-status", fromPage);
		},
	);
});

describe("a chart that fetches its own data", () => {
	it.skip("puts its own frame into loading while its series is on its way", () => {
		renderFrame(
			{ status: "ready", hasContentToDim: false },
			<SelfFetchingChart status="loading" />,
		);

		expect(frame()).toHaveAttribute("data-widget-status", "loading");
		expect(within(frame()).getByRole("progressbar")).toBeInTheDocument();
	});

	it.skip.each<[WidgetStatus, WidgetStatus, WidgetStatus]>([
		["ready", "ready", "ready"],
		["ready", "error", "error"],
		["error", "ready", "error"],
		["loading", "error", "error"],
		["error", "loading", "error"],
		["loading", "ready", "loading"],
		["ready", "loading", "loading"],
	])(
		"with the page saying %s and the chart reporting %s, the frame reads %s",
		(fromPage, fromChart, expected) => {
			renderFrame(
				{ status: fromPage, hasContentToDim: true },
				<SelfFetchingChart status={fromChart} />,
			);

			expect(frame()).toHaveAttribute("data-widget-status", expected);
		},
	);
});
