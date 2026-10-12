import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import {
	type RenderOptions,
	type RenderResult,
	render,
} from "@testing-library/react";
import type { ReactElement, ReactNode } from "react";

/** Renders with a fresh query cache that lives as long as the render, re-renders included. */
export function renderWithQueryClient(
	ui: ReactElement,
	options?: Omit<RenderOptions, "wrapper">,
): RenderResult {
	const queryClient = new QueryClient({
		defaultOptions: { queries: { retry: false } },
	});
	const withQueryClient = ({ children }: { children: ReactNode }) => (
		<QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
	);
	return render(ui, { ...options, wrapper: withQueryClient });
}
