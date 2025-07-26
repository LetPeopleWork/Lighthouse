import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import FeedbackDialog from "./FeedbackDialog";

describe("FeedbackDialog", () => {
	const mockOnClose = vi.fn();

	beforeEach(() => {
		mockOnClose.mockClear();
	});

	it("renders when open is true", () => {
		render(<FeedbackDialog open={true} onClose={mockOnClose} />);

		expect(screen.getByText("We'd Love to Hear from You!")).toBeInTheDocument();
		expect(
			screen.getByText(/We are eager to get feedback/),
		).toBeInTheDocument();
	});

	it("does not render when open is false", () => {
		render(<FeedbackDialog open={false} onClose={mockOnClose} />);

		expect(
			screen.queryByText("We'd Love to Hear from You!"),
		).not.toBeInTheDocument();
	});

	it("displays Slack community link", () => {
		render(<FeedbackDialog open={true} onClose={mockOnClose} />);

		const slackLink = screen.getByText("Join Let People Work Slack Community");
		expect(slackLink).toBeInTheDocument();
		expect(slackLink).toHaveAttribute(
			"href",
			"https://join.slack.com/t/let-people-work/shared_invite/zt-38df4z4sy-iqJEo6S8kmIgIfsgsV0J1A",
		);
		expect(slackLink).toHaveAttribute("target", "_blank");
		expect(slackLink).toHaveAttribute("rel", "noopener noreferrer");
	});

	it("displays email contact link", () => {
		render(<FeedbackDialog open={true} onClose={mockOnClose} />);

		const emailLink = screen.getByText("lighthouse@letpeople.work");
		expect(emailLink).toBeInTheDocument();
		expect(emailLink).toHaveAttribute(
			"href",
			"mailto:lighthouse@letpeople.work",
		);
	});

	it("displays feedback structure information", () => {
		render(<FeedbackDialog open={true} onClose={mockOnClose} />);

		expect(screen.getByText("For Feature Requests:")).toBeInTheDocument();
		expect(screen.getByText("For Bugs:")).toBeInTheDocument();
		expect(
			screen.getByText(/Steps to reproduce the issue/),
		).toBeInTheDocument();
		expect(
			screen.getByText(/Lighthouse version you're using/),
		).toBeInTheDocument();
	});

	it("displays custom development information", () => {
		render(<FeedbackDialog open={true} onClose={mockOnClose} />);

		expect(
			screen.getByText("Sponsored Feature Development"),
		).toBeInTheDocument();
		expect(
			screen.getByText(
				/Looking for a specific feature that would make Lighthouse perfect/,
			),
		).toBeInTheDocument();
		expect(
			screen.getByText(/we offer custom feature development services/),
		).toBeInTheDocument();
	});

	it("calls onClose when close button is clicked", async () => {
		const user = userEvent.setup();
		render(<FeedbackDialog open={true} onClose={mockOnClose} />);

		const closeButton = screen.getByRole("button", { name: "Close" });
		await user.click(closeButton);

		expect(mockOnClose).toHaveBeenCalledTimes(1);
	});

	it("calls onClose when dialog backdrop is clicked", async () => {
		const user = userEvent.setup();
		render(<FeedbackDialog open={true} onClose={mockOnClose} />);

		// Click outside the dialog content
		const backdrop = document.querySelector(".MuiBackdrop-root");
		if (backdrop) {
			await user.click(backdrop);
			expect(mockOnClose).toHaveBeenCalledTimes(1);
		}
	});

	it("has proper accessibility attributes", () => {
		render(<FeedbackDialog open={true} onClose={mockOnClose} />);

		const dialog = screen.getByRole("dialog");
		expect(dialog).toBeInTheDocument();

		const title = screen.getByText("We'd Love to Hear from You!");
		expect(title).toBeInTheDocument();
	});

	it("displays donation section with Ko-fi link", () => {
		render(<FeedbackDialog open={true} onClose={mockOnClose} />);

		expect(screen.getByText("Support Our Work")).toBeInTheDocument();
		expect(
			screen.getByText(/Lighthouse is completely Free and Open Source/),
		).toBeInTheDocument();

		const kofiLink = screen.getByText("Support us on Ko-fi");
		expect(kofiLink).toBeInTheDocument();
		expect(kofiLink).toHaveAttribute("href", "https://ko-fi.com/letpeoplework");
		expect(kofiLink).toHaveAttribute("target", "_blank");
		expect(kofiLink).toHaveAttribute("rel", "noopener noreferrer");
	});

	it("displays all required content sections", () => {
		render(<FeedbackDialog open={true} onClose={mockOnClose} />);

		// Check for main sections
		expect(
			screen.getByText("Preferred Way: Join Our Slack Community"),
		).toBeInTheDocument();
		expect(screen.getByText("Alternative: Email Feedback")).toBeInTheDocument();
		expect(
			screen.getByText("Sponsored Feature Development"),
		).toBeInTheDocument();

		// Check for feedback structure details
		expect(
			screen.getByText(/When reporting issues, please include:/),
		).toBeInTheDocument();
		expect(
			screen.getByText(/Description of the desired functionality/),
		).toBeInTheDocument();
		expect(
			screen.getByText(/Expected behavior vs. actual behavior/),
		).toBeInTheDocument();
	});
});
