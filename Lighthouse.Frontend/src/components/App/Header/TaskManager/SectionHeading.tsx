import Box from "@mui/material/Box";
import Typography from "@mui/material/Typography";
import type { ReactNode } from "react";

interface SectionHeadingProps {
	testId: string;
	/**
	 * Decorative, and deliberately so: the words beside it already say what the section is, and an icon
	 * that announced itself as well would have a screen reader read the same thing twice.
	 */
	icon: ReactNode;
	children: string;
}

/**
 * Three sections in a box that is opened for a glance. A mark beside each heading is what lets a reader
 * find the one they came for without reading all three.
 *
 * Nothing folds away. A control to collapse a section puts a click in front of content that is already
 * short enough to read, and leaves a preference to remember afterwards. If a section routinely runs past
 * about a dozen rows that trade changes, and until then it is chrome.
 */
const SectionHeading = ({ testId, icon, children }: SectionHeadingProps) => (
	<Box
		data-testid={testId}
		sx={{ display: "flex", alignItems: "center", gap: 1, mb: 0.5 }}
	>
		{icon}
		<Typography variant="subtitle2">{children}</Typography>
	</Box>
);

export default SectionHeading;
