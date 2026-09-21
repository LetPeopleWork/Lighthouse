import InfoOutlinedIcon from "@mui/icons-material/InfoOutlined";
import WarningAmberIcon from "@mui/icons-material/WarningAmber";
import { Box, Tooltip } from "@mui/material";
import type React from "react";
import { createContext, useContext } from "react";
import { getContrastText } from "../../../../../../utils/theme/colors";
import type { TeamColour, TeamLane } from "./deliveryTeamLanes";
import type { TimelineBar } from "./deliveryTimelineModel";
import { barTooltip } from "./ganttShapes";

/**
 * One thing a bar has to say, and whether it is worth an alarm. Decided by whoever writes the note
 * rather than here, so a Feature cannot read as clean on this chart and marked in the Feature table.
 */
export interface BarNote {
	text: string;
	isWarning: boolean;
	/**
	 * What the note is about, for the notes whose own words do not say. Two Teams this Portfolio
	 * cannot name produce the same sentence, so a list keyed on the sentence shows one of them and
	 * silently drops the other. Absent where the words are their own identity.
	 */
	subject?: string;
}

/** Everything one bar has to say for itself. */
export interface BarMark {
	notes: BarNote[];
	/**
	 * Names written along the bar rather than left to the symbol and its hover text.
	 *
	 * A name reachable only by hovering does not answer the question the split exists for, which is
	 * seeing which Team it is without opening anything. So while the Teams are being read, the one
	 * Team without a row of its own is named where all the others already are.
	 *
	 * Each carries an id because the names do not have to differ: every Team a Portfolio cannot name
	 * gets the same phrase, and two of them on one Feature would otherwise be one name twice - which
	 * a list keyed by name renders as a single entry, showing fewer Teams than the Feature has.
	 */
	namesOnTheBar?: { teamId: number; name: string }[];
}

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
	/**
	 * One Team's own row beneath the Feature's bar. It carries that Team's name and that Team's
	 * fill, opens the same Feature the bar opens, and never repeats the Feature's mark: every mark
	 * this chart can raise is a statement about the Feature, true once, where the reader looks for
	 * it — repeated down a three-lane Feature it would wear the same symbol four times.
	 */
	lane?: TeamLane;
	/**
	 * The one Team that works on this Feature, where it has exactly one. Such a Feature is never
	 * split - with a single Team the earliest and the latest across the Teams are that Team, so a
	 * row of its own would be a second bar drawn where the first one already is. Its bar carries
	 * the Team's colour and name instead, which is the part a bar never had.
	 */
	team?: TeamColour;
	/**
	 * The colour this bar wears for what it has to say about the Delivery's target date. Absent
	 * unless the reader has asked for the status, and absent for a bar that fits - a colour on every
	 * bar tells them nothing about any of them.
	 *
	 * Never given to a lane. Whether the *Feature* misses the date is not a fact about one of its
	 * Teams, and a Feature with three of them would otherwise say it four times.
	 *
	 * It cannot arrive alongside `team`: both are the bar's fill, and the reader is asked one
	 * question at a time so that each answer gets the whole bar.
	 */
	statusColor?: string;
	/** Absent means the row is inert: no pointer, no click, and nothing promised on hover. */
	onSelect?: (featureId: number) => void;
}

const TimelineBarContent: React.FC<TimelineBarContentProps> = ({
	bar,
	lane,
	team,
	statusColor,
	onSelect,
}) => {
	const marks = useContext(BarMarks);

	if (lane) {
		return (
			<RowBody
				hoverText={barTooltip(lane, onSelect !== undefined)}
				fill={lane.color}
				onSelect={onSelect ? () => onSelect(lane.featureId) : undefined}
			>
				{lane.teamName}
			</RowBody>
		);
	}

	// The chart asked for a bar we do not have. Rendering an empty one would put a nameless
	// clickable box on the timeline; rendering nothing leaves the chart's own bar as it was.
	if (!bar) {
		return null;
	}

	const mark = marks.get(bar.featureId);

	return (
		<RowBody
			hoverText={barHoverText(bar, onSelect !== undefined, mark)}
			fill={team?.color ?? statusColor}
			onSelect={onSelect ? () => onSelect(bar.featureId) : undefined}
		>
			{bar.name}
			{team && <NameOnBar name={team.teamName} />}
			{mark?.namesOnTheBar?.map((named) => (
				<NameOnBar key={named.teamId} name={named.name} />
			))}
			{mark && <BarMarkSymbol mark={mark} />}
		</RowBody>
	);
};

/**
 * A Team's name written along a bar beside the Feature's own - the one Team that works on it, or a
 * Team that has no row of its own.
 *
 * The separator is drawn rather than written, so what a screen reader reads out is the name and not
 * a piece of punctuation.
 */
const NameOnBar: React.FC<{ name: string }> = ({ name }) => (
	<Box
		component="span"
		sx={{ ml: 0.5, flexShrink: 0, "&::before": { content: '"· "' } }}
	>
		{name}
	</Box>
);

/**
 * The box a row of this chart is drawn in, whichever kind of row it is.
 *
 * The fill is painted here, over the library's own bar element, which keeps its border and its
 * default fill underneath — so a rim of that default may show at the edges. Nothing in a test can
 * see it: this environment mocks the library's stylesheet away, which is the same reason the axis
 * format and the link routing are checked by a person or not at all.
 *
 * **The text colour is chosen against the fill rather than inherited.** Everything on this chart
 * used to be one theme colour, and white over it was a decision taken once for both modes. A Team's
 * fill comes from a palette of fourteen built to be told apart on a dark background, several of
 * which are pale enough that white on them is unreadable rather than merely poor. Inherited white
 * is right for a Feature's own bar and wrong for anything wearing a Team's colour, so the rows that
 * carry one work it out per fill.
 */
export const rowTextColour = (fill?: string): string =>
	fill ? getContrastText(fill) : "inherit";

const RowBody: React.FC<{
	hoverText: React.ReactNode;
	fill?: string;
	onSelect?: () => void;
	children: React.ReactNode;
}> = ({ hoverText, fill, onSelect, children }) => {
	const isClickable = onSelect !== undefined;

	return (
		<Tooltip title={hoverText} followCursor>
			<Box
				component={isClickable ? "button" : "div"}
				type={isClickable ? "button" : undefined}
				data-testid="timeline-bar-content"
				onClick={onSelect}
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
					backgroundColor: fill,
					border: "none",
					font: "inherit",
					color: rowTextColour(fill),
					cursor: isClickable ? "pointer" : "default",
				}}
			>
				{children}
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
					<li key={note.subject ?? note.text}>{note.text}</li>
				))}
			</Box>
		</>
	);
};

export default TimelineBarContent;
