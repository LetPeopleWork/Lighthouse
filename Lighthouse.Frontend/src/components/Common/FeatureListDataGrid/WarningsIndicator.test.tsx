import { render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import WarningsIndicator from "./WarningsIndicator";

vi.mock("../../../services/TerminologyContext", () => ({
	useTerminology: () => ({
		getTerm: (key: string) => {
			if (key === "workItems") return "Work Items";
			if (key === "feature") return "Feature";
			return key;
		},
	}),
}));

describe("WarningsIndicator", () => {
	it("should render check icon when no warnings apply", () => {
		render(
			<WarningsIndicator
				isDoneWithRemainingWork={false}
				isUsingDefaultFeatureSize={false}
			/>,
		);

		expect(screen.getByTestId("no-warnings")).toBeInTheDocument();
	});

	it("should render warning icon when isDoneWithRemainingWork is true", () => {
		render(
			<WarningsIndicator
				isDoneWithRemainingWork={true}
				isUsingDefaultFeatureSize={false}
			/>,
		);

		expect(
			screen.getByTestId("warning-done-with-remaining-work"),
		).toBeInTheDocument();
	});

	it("should render warning icon when isUsingDefaultFeatureSize is true", () => {
		render(
			<WarningsIndicator
				isDoneWithRemainingWork={false}
				isUsingDefaultFeatureSize={true}
			/>,
		);

		expect(
			screen.getByTestId("warning-default-feature-size"),
		).toBeInTheDocument();
	});

	it("should render two warning icons when both conditions are true", () => {
		render(
			<WarningsIndicator
				isDoneWithRemainingWork={true}
				isUsingDefaultFeatureSize={true}
			/>,
		);

		expect(
			screen.getByTestId("warning-done-with-remaining-work"),
		).toBeInTheDocument();
		expect(
			screen.getByTestId("warning-default-feature-size"),
		).toBeInTheDocument();
	});

	it("should have accessible aria-label on done with remaining work warning", () => {
		render(
			<WarningsIndicator
				isDoneWithRemainingWork={true}
				isUsingDefaultFeatureSize={false}
			/>,
		);

		const button = screen.getByTestId("warning-done-with-remaining-work");
		expect(button).toHaveAttribute("aria-label");
	});

	it("should have accessible aria-label on default feature size warning", () => {
		render(
			<WarningsIndicator
				isDoneWithRemainingWork={false}
				isUsingDefaultFeatureSize={true}
			/>,
		);

		const button = screen.getByTestId("warning-default-feature-size");
		expect(button).toHaveAttribute("aria-label");
	});
});
