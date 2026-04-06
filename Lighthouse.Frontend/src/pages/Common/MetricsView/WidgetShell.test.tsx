import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it } from "vitest";
import WidgetShell from "./WidgetShell";

describe("WidgetShell", () => {
	it("renders children with correct testid", () => {
		render(
			<WidgetShell widgetKey="test-widget">
				<div data-testid="child-content">Hello</div>
			</WidgetShell>,
		);
		expect(screen.getByTestId("widget-shell-test-widget")).toBeInTheDocument();
		expect(screen.getByTestId("child-content")).toBeInTheDocument();
	});

	it("renders title when provided", () => {
		render(
			<WidgetShell widgetKey="test-widget" title="My Chart">
				<div>Content</div>
			</WidgetShell>,
		);
		expect(screen.getByText("My Chart")).toBeInTheDocument();
		expect(
			screen.getByTestId("widget-shell-header-test-widget"),
		).toBeInTheDocument();
	});

	it("omits header when no title, footer, or info is provided", () => {
		render(
			<WidgetShell widgetKey="test-widget">
				<div>Content</div>
			</WidgetShell>,
		);
		expect(
			screen.queryByTestId("widget-shell-header-test-widget"),
		).not.toBeInTheDocument();
	});

	it("renders RAG chip in header when footer provided and showTips is true", () => {
		render(
			<WidgetShell
				widgetKey="test-widget"
				showTips={true}
				header={{ ragStatus: "red", tipText: "Action needed" }}
			>
				<div>Content</div>
			</WidgetShell>,
		);
		expect(
			screen.getByTestId("widget-shell-header-test-widget"),
		).toBeInTheDocument();
		expect(screen.getByTestId("widget-rag-test-widget")).toBeInTheDocument();
		// No footer element
		expect(
			screen.queryByTestId("widget-shell-footer-test-widget"),
		).not.toBeInTheDocument();
	});

	it("hides RAG chip when showTips is false", () => {
		render(
			<WidgetShell
				widgetKey="test-widget"
				showTips={false}
				header={{ ragStatus: "green", tipText: "All good" }}
			>
				<div>Content</div>
			</WidgetShell>,
		);
		expect(
			screen.queryByTestId("widget-rag-test-widget"),
		).not.toBeInTheDocument();
	});

	it("omits RAG chip when ragStatus is none", () => {
		render(
			<WidgetShell
				widgetKey="test-widget"
				showTips={true}
				header={{ ragStatus: "none", tipText: "Info only" }}
			>
				<div>Content</div>
			</WidgetShell>,
		);
		expect(
			screen.queryByTestId("widget-rag-test-widget"),
		).not.toBeInTheDocument();
	});

	it("shows tip text as tooltip on RAG chip", async () => {
		const user = userEvent.setup();
		render(
			<WidgetShell
				widgetKey="test-widget"
				showTips={true}
				header={{ ragStatus: "amber", tipText: "Review suggested" }}
			>
				<div>Content</div>
			</WidgetShell>,
		);
		const chip = screen.getByTestId("widget-rag-test-widget");
		await user.hover(chip);
		expect(await screen.findByText("Review suggested")).toBeInTheDocument();
	});

	it("renders info button when info prop provided", () => {
		render(
			<WidgetShell
				widgetKey="test-widget"
				info={{
					description: "This is a test widget",
					learnMoreUrl: "https://example.com",
				}}
			>
				<div>Content</div>
			</WidgetShell>,
		);
		expect(screen.getByTestId("widget-info-test-widget")).toBeInTheDocument();
	});

	it("shows description and Learn More link in info popover on click", async () => {
		const user = userEvent.setup();
		render(
			<WidgetShell
				widgetKey="test-widget"
				info={{
					description: "Shows completed items over time",
					learnMoreUrl: "https://docs.example.com#throughput",
				}}
			>
				<div>Content</div>
			</WidgetShell>,
		);
		await user.click(screen.getByTestId("widget-info-test-widget"));
		expect(
			screen.getByText("Shows completed items over time"),
		).toBeInTheDocument();
		const link = screen.getByRole("link", { name: /learn more/i });
		expect(link).toHaveAttribute("href", "https://docs.example.com#throughput");
	});

	it("does not render info button when info prop is absent", () => {
		render(
			<WidgetShell widgetKey="test-widget">
				<div>Content</div>
			</WidgetShell>,
		);
		expect(
			screen.queryByTestId("widget-info-test-widget"),
		).not.toBeInTheDocument();
	});
});
