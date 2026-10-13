import { Box, Paper, Stack, Typography } from "@mui/material";
import type React from "react";
import { useId } from "react";

interface ThenNowPanelFrameProps {
	title: string;
	then: React.ReactNode;
	now: React.ReactNode;
	/** Left empty for now: the place a note on the panel will go. */
	footer?: React.ReactNode;
}

const ColumnLabel: React.FC<{ readonly children: string }> = ({ children }) => (
	<Typography
		variant="overline"
		color="text.secondary"
		component="div"
		sx={{ lineHeight: 1.5 }}
	>
		{children}
	</Typography>
);

/** The shared look of every Then & Now panel: the title, Then on the left, an accent bar, Now on the right. */
const ThenNowPanelFrame: React.FC<Readonly<ThenNowPanelFrameProps>> = ({
	title,
	then,
	now,
	footer,
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
					gridTemplateColumns: {
						xs: "1fr",
						md: "minmax(0, 1fr) minmax(0, 2fr)",
					},
					gap: 2,
				}}
			>
				<Stack spacing={0.5}>
					<ColumnLabel>Then</ColumnLabel>
					{then}
				</Stack>
				<Stack
					spacing={0.5}
					sx={{ borderLeft: 3, borderColor: "primary.main", pl: 2 }}
				>
					<ColumnLabel>Now</ColumnLabel>
					{now}
				</Stack>
			</Box>
			{footer}
		</Paper>
	);
};

export default ThenNowPanelFrame;
