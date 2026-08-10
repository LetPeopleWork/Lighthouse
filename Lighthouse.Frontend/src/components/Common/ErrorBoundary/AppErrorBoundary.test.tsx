import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import AppErrorBoundary from "./AppErrorBoundary";

const originalError = console.error;

beforeEach(() => {
	console.error = vi.fn();
});

afterEach(() => {
	console.error = originalError;
});

const Boom = ({ shouldThrow }: { shouldThrow: boolean }) => {
	if (shouldThrow) {
		throw new TypeError("r.find is not a function");
	}

	return <span data-testid="app-content">Lighthouse</span>;
};

describe("AppErrorBoundary", () => {
	it("renders its children while nothing throws", () => {
		render(
			<AppErrorBoundary>
				<Boom shouldThrow={false} />
			</AppErrorBoundary>,
		);

		expect(screen.getByTestId("app-content")).toBeInTheDocument();
	});

	// Bug #5732: a render-time throw used to unmount the whole tree and leave a white screen.
	it("shows a readable fallback instead of a blank page when a child throws", () => {
		render(
			<AppErrorBoundary>
				<Boom shouldThrow={true} />
			</AppErrorBoundary>,
		);

		expect(screen.queryByTestId("app-content")).not.toBeInTheDocument();
		expect(screen.getByText(/something went wrong/i)).toBeInTheDocument();
		expect(screen.getByRole("button", { name: /reload/i })).toBeInTheDocument();
	});

	it("surfaces the underlying error message so the failure is diagnosable", () => {
		render(
			<AppErrorBoundary>
				<Boom shouldThrow={true} />
			</AppErrorBoundary>,
		);

		expect(screen.getByText(/r\.find is not a function/i)).toBeInTheDocument();
	});

	it("reloads the page when the reload button is pressed", async () => {
		const reload = vi.fn();
		Object.defineProperty(globalThis, "location", {
			configurable: true,
			value: { ...globalThis.location, reload },
		});

		render(
			<AppErrorBoundary>
				<Boom shouldThrow={true} />
			</AppErrorBoundary>,
		);

		await userEvent.click(screen.getByRole("button", { name: /reload/i }));

		expect(reload).toHaveBeenCalled();
	});
});
