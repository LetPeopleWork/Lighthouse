import { useQueryClient } from "@tanstack/react-query";
import type React from "react";
import { type ReactNode, useEffect, useState } from "react";
import { useLocation } from "react-router";
import {
	askSharedReadsAgainFor,
	PageMovesContext,
} from "../../../hooks/usePageFreshQuery";
import { fireAndForget } from "../../../utils/fireAndForget";

interface PageSeen {
	lastPathname: string;
	page: number;
}

/**
 * Counts the user's page moves and asks every shared page read again once after each move. The pathname and the
 * count move together in one update during render, so a repeated or abandoned render cannot move one without the
 * other, and a component that first renders on the new page already reads the new count.
 */
export const PageMoveRefresh: React.FC<{ children?: ReactNode }> = ({
	children,
}) => {
	const { pathname } = useLocation();
	const queryClient = useQueryClient();
	const [seen, setSeen] = useState<PageSeen>({
		lastPathname: pathname,
		page: 0,
	});
	let current = seen;
	if (seen.lastPathname !== pathname) {
		current = { lastPathname: pathname, page: seen.page + 1 };
		setSeen(current);
	}
	const page = current.page;

	useEffect(() => {
		if (page === 0) {
			return;
		}
		fireAndForget(askSharedReadsAgainFor(queryClient, page));
	}, [page, queryClient]);

	return (
		<PageMovesContext.Provider value={page}>
			{children}
		</PageMovesContext.Provider>
	);
};

export default PageMoveRefresh;
