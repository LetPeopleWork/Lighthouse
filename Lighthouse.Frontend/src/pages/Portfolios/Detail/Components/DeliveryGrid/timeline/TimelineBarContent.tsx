import InfoOutlinedIcon from "@mui/icons-material/InfoOutlined";
import WarningAmberIcon from "@mui/icons-material/WarningAmber";
import { Box, Tooltip } from "@mui/material";
import type React from "react";
import { createContext, useContext } from "react";
import type { BarMark } from "./deliveryDependencyOverlay";
import type { TimelineBar } from "./deliveryTimelineModel";
import { barTooltip } from "./ganttShapes";

const BarMarks = createContext<ReadonlyMap<number, BarMark>>(new Map());

/**
 * What each bar has to say about its dependencies, made available to every bar at once.
 *
 * Handed down rather than passed in because the chart library owns the tree between the chart and
 * the bars it draws: a bar is rendered by the library from a template, so there is no prop to give
 * it. The alternative is the adapter carrying marks it has no use for, and the adapter is the one
 * file that exists to know nothing about them.
 */
export const TimelineBarMarks: React.FC<{
	marks: ReadonlyMap<number, BarMark>;
	children: React.ReactNode;
}> = ({ marks, children }) => (
	<BarMarks.Provider value={marks}>{children}</BarMarks.Provider>
);

/**
 * The inside of one bar on the timeline.
 *
 * The chart library lets a caller render a bar's content, and this is it — which is what keeps the
 * hover text and the click in ordinary React rather than in the library's own event system. That
 * system is a trap here: its props carry an `on${string}` index signature, so a misremembered
 * event name typechecks, compiles and silently never fires.
 *
 * It lives in its own file so it can be rendered on its own in a test. Reached only through the
 * chart, everything it decides — whether the bar is clickable, what the hover says, what happens
 * to a bar the chart asks for and we do not have — is behind a library that draws to a canvas this
 * environment does not provide.
 */
export interface TimelineBarContentProps {
	bar?: TimelineBar;
	/** Absent means the bar is inert: no pointer, no click, and nothing promised on hover. */
	onSelect?: (featureId: number) => void;
}

const TimelineBarContent: React.FC<TimelineBarContentProps> = ({
	bar,
	onSelect,
}) => {
	const marks = useContext(BarMarks);

	// The chart asked for a bar we do not have. Rendering an empty one would put a nameless
	// clickable box on the timeline; rendering nothing leaves the chart's own bar as it was.
	if (!bar) {
		return null;
	}

	const isClickable = onSelect !== undefined;
	const mark = marks.get(bar.featureId);

	return (
		<Tooltip title={barHoverText(bar, isClickable, mark)} followCursor>
			<Box
				component={isClickable ? "button" : "div"}
				type={isClickable ? "button" : undefined}
				data-testid="timeline-bar-content"
				onClick={isClickable ? () => onSelect(bar.featureId) : undefined}
				sx={{
					width: "100%",
					height: "100%",
					display: "flex",
					alignItems: "center",
					justifyContent: "center",
					overflow: "hidden",
					whiteSpace: "nowrap",
					textOverflow: "ellipsis",
					px: 1,
					background: "none",
					border: "none",
					font: "inherit",
					color: "inherit",
					cursor: isClickable ? "pointer" : "default",
				}}
			>
				{bar.name}
				{mark && <BarMarkSymbol mark={mark} />}
			</Box>
		</Tooltip>
	);
};

/**
 * One symbol for the whole bar, and which of the two it is.
 *
 * Whether any of this is worth warning about was decided once, where the notes were written, so a
 * Feature cannot read as clean here and marked in the table. A bar with nothing to say gets nothing
 * at all - the table's green check is right in a column a reader scans and wrong in a bar a few
 * pixels tall, where it would sit on almost every bar and crowd out the name.
 */
const BarMarkSymbol: React.FC<{ mark: BarMark }> = ({ mark }) => {
	const isWarning = mark.notes.some((note) => note.isWarning);
	const MarkIcon = isWarning ? WarningAmberIcon : InfoOutlinedIcon;

	return (
		<MarkIcon
			data-testid="timeline-bar-mark"
			fontSize="small"
			// The hover text is unreachable without a pointer, so the whole of it is spoken here.
			// The leading word is what tells the two symbols apart when only one is being read out.
			titleAccess={`${isWarning ? "Warning" : "Note"}. ${sentencesOf(mark)}`}
			sx={{
				ml: 0.5,
				flexShrink: 0,
				color: isWarning ? "warning.main" : "inherit",
			}}
		/>
	);
};

const sentencesOf = (mark: BarMark): string =>
	mark.notes.map((note) => note.text).join(" ");

// The symbol says there is something to read; this is where it is read. The span comes first
// because it is what a reader hovers a bar for, and it is on every bar.
const barHoverText = (
	bar: TimelineBar,
	isClickable: boolean,
	mark?: BarMark,
): React.ReactNode => {
	const span = barTooltip(bar, isClickable);

	if (!mark) {
		return span;
	}

	return (
		<>
			{span}
			<Box component="ul" sx={{ m: 0, mt: 0.5, pl: 2 }}>
				{mark.notes.map((note) => (
					<li key={note.text}>{note.text}</li>
				))}
			</Box>
		</>
	);
};

export default TimelineBarContent;
