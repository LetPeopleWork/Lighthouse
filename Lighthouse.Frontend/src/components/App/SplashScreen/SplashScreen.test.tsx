import { createTheme, ThemeProvider } from "@mui/material";
import { render, screen } from "@testing-library/react";
import SplashScreen from "./SplashScreen";
import {
	type Contributor,
	contributors,
	loadingMessages,
	pickRandom,
	tips,
} from "./SplashScreenNews";

const theme = createTheme();

const mockContributor: Contributor = {
	name: "Jane Doe",
	url: "https://example.com/janedoe",
};

const renderSplashScreen = (
	props: React.ComponentProps<typeof SplashScreen> = {},
) => {
	return render(
		<ThemeProvider theme={theme}>
			<SplashScreen {...props} />
		</ThemeProvider>,
	);
};

describe("SplashScreen", () => {
	it("renders the Lighthouse logo", () => {
		renderSplashScreen();
		expect(screen.getByAltText("Lighthouse Logo")).toBeInTheDocument();
	});

	it("renders the app name", () => {
		renderSplashScreen();
		expect(screen.getByText("Lighthouse by LetPeopleWork")).toBeInTheDocument();
	});

	it("renders the loading section label", () => {
		renderSplashScreen({ loadingMessage: "Test loading..." });
		expect(screen.getByTestId("splash-loading")).toBeInTheDocument();
		expect(
			screen.getByText("Setting Things up: Test loading..."),
		).toBeInTheDocument();
	});

	it("renders the tip section label", () => {
		renderSplashScreen({ tip: "Test tip content." });
		expect(screen.getByTestId("splash-tip")).toBeInTheDocument();
		expect(screen.getByText("Did you know?")).toBeInTheDocument();
		expect(screen.getByText("Test tip content.")).toBeInTheDocument();
	});

	it("renders the contributor section", () => {
		renderSplashScreen({ contributor: mockContributor });
		expect(screen.getByTestId("splash-contributor")).toBeInTheDocument();
		expect(
			screen.getByText(
				"Lighthouse is built with ❤️ and the feedback from our Community",
			),
		).toBeInTheDocument();
		const link = screen.getByRole("link", { name: "Jane Doe" });
		expect(link).toBeInTheDocument();
		expect(link).toHaveAttribute("href", "https://example.com/janedoe");
		expect(link).toHaveAttribute("target", "_blank");
	});

	it("shows a value from the loadingMessages list when no prop is given", () => {
		renderSplashScreen();
		const section = screen.getByTestId("splash-loading");
		const shownMessage = section.textContent ?? "";
		const match = loadingMessages.some((m) => shownMessage.includes(m));
		expect(match).toBe(true);
	});

	it("shows a value from the tips list when no prop is given", () => {
		renderSplashScreen();
		const section = screen.getByTestId("splash-tip");
		const shownText = section.textContent ?? "";
		const match = tips.some((t) => shownText.includes(t));
		expect(match).toBe(true);
	});

	it("shows a value from the contributors list when no prop is given", () => {
		renderSplashScreen();
		const section = screen.getByTestId("splash-contributor");
		const shownText = section.textContent ?? "";
		const match = contributors.some((c) => shownText.includes(c.name));
		expect(match).toBe(true);
	});

	it("renders all three info rows simultaneously", () => {
		renderSplashScreen({
			loadingMessage: "Starting up...",
			tip: "A handy tip.",
			contributor: mockContributor,
		});
		expect(screen.getByTestId("splash-loading")).toBeInTheDocument();
		expect(screen.getByTestId("splash-tip")).toBeInTheDocument();
		expect(screen.getByTestId("splash-contributor")).toBeInTheDocument();
	});
});

describe("pickRandom", () => {
	it("returns an element from the given array", () => {
		const items = ["a", "b", "c"] as const;
		const result = pickRandom(items);
		expect(items).toContain(result);
	});

	it("returns the only element when array has one item", () => {
		expect(pickRandom(["only"] as const)).toBe("only");
	});
});
