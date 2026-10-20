import { Box, Paper, Stack, Typography } from "@mui/material";
import type React from "react";
import { useId } from "react";

interface ThenNowPanelFrameProps {
	title: string;
	then: React.ReactNode;
	now: React.ReactNode;
	change: React.ReactNode;
}

type DataAttributes = { readonly [attribute: `data-${string}`]: string };

type ThenNowColumnProps = DataAttributes & {
	readonly heading: string;
	readonly first: boolean;
	readonly children: React.ReactNode;
};

/**
 * One row of a segment. Rows share a height so a percentile lines up across Then, Now and Change, and each
 * ends in a space so the panel reads as separate lines to a screen reader or anything else taking its text.
 */
export const PanelLine: React.FC<{ readonly children: React.ReactNode }> = ({
	children,
}) => (
	<>
		<Box sx={{ display: "flex", alignItems: "center", gap: 1, minHeight: 28 }}>
			{children}
		</Box>{" "}
	</>
);

/** Side-by-side columns that stack on a narrow screen, but never on paper, which is narrower than a wide screen. */
export const ThenNowColumns: React.FC<{
	readonly count: number;
	readonly children: React.ReactNode;
}> = ({ count, children }) => {
	const sideBySide = `repeat(${count}, minmax(0, 1fr))`;
	return (
		<Box
			sx={{
				display: "grid",
				gridTemplateColumns: { xs: "1fr", md: sideBySide },
				"@media print": { gridTemplateColumns: sideBySide },
				gap: 2,
			}}
		>
			{children}
		</Box>
	);
};

/** A small heading over its lines; every column after the first is set off by a divider on a wide screen and on paper. */
export const ThenNowColumn: React.FC<ThenNowColumnProps> = ({
	heading,
	first,
	children,
	...dataAttributes
}) => (
	<Stack
		{...dataAttributes}
		spacing={0.5}
		sx={{
			minWidth: 0,
			pl: { md: first ? 0 : 2 },
			borderLeft: { md: first ? 0 : 1 },
			"@media print": { pl: first ? 0 : 2, borderLeft: first ? 0 : 1 },
			borderColor: "divider",
		}}
	>
		<Typography
			variant="overline"
			color="text.secondary"
			component="div"
			sx={{ lineHeight: 1.5 }}
		>
			{heading}
		</Typography>{" "}
		{children}
	</Stack>
);

/** The shared look of every Then & Now panel: the metric's name over three segments, Then, Now and Change. */
const ThenNowPanelFrame: React.FC<Readonly<ThenNowPanelFrameProps>> = ({
	title,
	then,
	now,
	change,
}) => {
	const titleId = useId();
	return (
		<Paper
			component="section"
			aria-labelledby={titleId}
			variant="outlined"
			sx={{ p: 2, width: "100%", breakInside: "avoid" }}
		>
			<Typography id={titleId} variant="h6" component="h2" gutterBottom>
				{title}
			</Typography>
			<ThenNowColumns count={3}>
				<ThenNowColumn data-segment="then" heading="Then" first>
					{then}
				</ThenNowColumn>
				<ThenNowColumn data-segment="now" heading="Now" first={false}>
					{now}
				</ThenNowColumn>
				<ThenNowColumn data-segment="change" heading="Change" first={false}>
					{change}
				</ThenNowColumn>
			</ThenNowColumns>
		</Paper>
	);
};

export default ThenNowPanelFrame;
