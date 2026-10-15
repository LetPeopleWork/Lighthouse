import { Box, CircularProgress, Link, Typography } from "@mui/material";
import type React from "react";
import { Link as RouterLink } from "react-router";

export type OverviewCellStatus = "loading" | "error" | "ready";

/** A re-read counts as loading, so a cell never presents an answer it is about to replace as current. */
export const overviewCellStatusOf = ({
	isFetching,
	isError,
}: {
	isFetching: boolean;
	isError: boolean;
}): OverviewCellStatus => {
	if (isFetching) return "loading";
	if (isError) return "error";
	return "ready";
};

// The same dimmed look a dashboard widget takes while its data is read again.
const DIMMED_OPACITY = 0.4;
const SPINNER_SIZE = 16;
const SHOWN_AT_MOST = 2;

export interface OverviewCellChipsProps<T> {
	readonly status: OverviewCellStatus;
	readonly items: readonly T[] | undefined;
	readonly renderItem: (item: T) => React.ReactNode;
	/** Where "+N more" takes the reader to see every item. */
	readonly moreTo: string;
	readonly emptyText: string;
	readonly failureText: string;
}

const SecondaryText: React.FC<{ readonly text: string }> = ({ text }) => (
	<Typography variant="body2" color="text.secondary">
		{text}
	</Typography>
);

const centredSpinner = (
	<CircularProgress
		size={SPINNER_SIZE}
		sx={{
			position: "absolute",
			top: "50%",
			left: "50%",
			marginTop: `${-SPINNER_SIZE / 2}px`,
			marginLeft: `${-SPINNER_SIZE / 2}px`,
		}}
	/>
);

export const OverviewCellChips = <T,>({
	status,
	items,
	renderItem,
	moreTo,
	emptyText,
	failureText,
}: Readonly<OverviewCellChipsProps<T>>): React.ReactElement => {
	const isLoading = status === "loading";
	const hasItems = items !== undefined && items.length > 0;
	const hiddenCount = (items?.length ?? 0) - SHOWN_AT_MOST;

	const content = () => {
		if (status === "error") return <SecondaryText text={failureText} />;
		if (!hasItems) return isLoading ? null : <SecondaryText text={emptyText} />;
		return (
			<Box
				sx={{
					display: "flex",
					flexDirection: "column",
					gap: 0.5,
					...(isLoading && {
						opacity: DIMMED_OPACITY,
						pointerEvents: "none",
					}),
				}}
			>
				{items.slice(0, SHOWN_AT_MOST).map((item) => renderItem(item))}
				{hiddenCount > 0 && (
					<Link component={RouterLink} to={moreTo} variant="body2">
						+{hiddenCount} more
					</Link>
				)}
			</Box>
		);
	};

	return (
		<Box
			data-cell-status={status}
			aria-busy={isLoading ? "true" : undefined}
			sx={{ position: "relative", minHeight: SPINNER_SIZE, py: 0.5 }}
		>
			{content()}
			{isLoading && centredSpinner}
		</Box>
	);
};
