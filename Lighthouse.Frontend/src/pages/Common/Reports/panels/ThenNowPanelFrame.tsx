import { Box, Paper, Stack, Typography } from "@mui/material";
import type React from "react";
import { useId } from "react";

type PanelSegment = "then" | "now" | "change";

interface ThenNowPanelFrameProps {
	title: string;
	thenHeading: string;
	then: React.ReactNode;
	nowHeading: string;
	now: React.ReactNode;
	change: React.ReactNode;
}

interface SegmentProps {
	segment: PanelSegment;
	heading: string;
	children: React.ReactNode;
}

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

const Segment: React.FC<Readonly<SegmentProps>> = ({
	segment,
	heading,
	children,
}) => {
	const first = segment === "then";
	return (
		<Stack
			spacing={0.5}
			data-segment={segment}
			sx={{
				minWidth: 0,
				pl: { md: first ? 0 : 2 },
				borderLeft: { md: first ? 0 : 1 },
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
};

/** The shared look of every Then & Now panel: the metric's name over three segments, Then, Now and Change. */
const ThenNowPanelFrame: React.FC<Readonly<ThenNowPanelFrameProps>> = ({
	title,
	thenHeading,
	then,
	nowHeading,
	now,
	change,
}) => {
	const titleId = useId();
	return (
		<Paper
			component="section"
			aria-labelledby={titleId}
			variant="outlined"
			sx={{ p: 2, width: "100%" }}
		>
			<Typography id={titleId} variant="h6" component="h2" gutterBottom>
				{title}
			</Typography>
			<Box
				sx={{
					display: "grid",
					gridTemplateColumns: { xs: "1fr", md: "repeat(3, minmax(0, 1fr))" },
					gap: 2,
				}}
			>
				<Segment segment="then" heading={thenHeading}>
					{then}
				</Segment>
				<Segment segment="now" heading={nowHeading}>
					{now}
				</Segment>
				<Segment segment="change" heading="Change">
					{change}
				</Segment>
			</Box>
		</Paper>
	);
};

export default ThenNowPanelFrame;
