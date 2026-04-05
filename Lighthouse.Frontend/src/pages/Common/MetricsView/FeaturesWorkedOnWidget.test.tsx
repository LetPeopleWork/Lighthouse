import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import FeaturesWorkedOnWidget from "./FeaturesWorkedOnWidget";

describe("FeaturesWorkedOnWidget", () => {
	it("renders the feature count", () => {
		render(<FeaturesWorkedOnWidget featureCount={3} />);
		expect(screen.getByTestId("features-worked-on-count")).toHaveTextContent(
			"3",
		);
	});

	it("renders the feature WIP limit when defined", () => {
		render(<FeaturesWorkedOnWidget featureCount={3} featureWip={5} />);
		expect(screen.getByTestId("features-worked-on-limit")).toHaveTextContent(
			"5",
		);
	});

	it("does not render the limit when feature WIP is 0", () => {
		render(<FeaturesWorkedOnWidget featureCount={3} featureWip={0} />);
		expect(
			screen.queryByTestId("features-worked-on-limit"),
		).not.toBeInTheDocument();
	});

	it("does not render the limit when feature WIP is undefined", () => {
		render(<FeaturesWorkedOnWidget featureCount={3} />);
		expect(
			screen.queryByTestId("features-worked-on-limit"),
		).not.toBeInTheDocument();
	});

	it("renders title text", () => {
		render(
			<FeaturesWorkedOnWidget
				featureCount={2}
				title="Features being Worked On"
			/>,
		);
		expect(screen.getByText("Features being Worked On")).toBeInTheDocument();
	});
});
