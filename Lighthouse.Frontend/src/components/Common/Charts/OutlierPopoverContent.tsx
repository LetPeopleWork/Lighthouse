import {
	Box,
	Button,
	Table,
	TableBody,
	TableCell,
	TableHead,
	TableRow,
	Typography,
} from "@mui/material";
import type React from "react";
import type { OutlierWording } from "../../../utils/charts/outlierWording";

// Read out by a screen reader, in place of the short figure that is drawn.
const visuallyHidden: React.CSSProperties = {
	position: "absolute",
	width: 1,
	height: 1,
	margin: -1,
	padding: 0,
	overflow: "hidden",
	clip: "rect(0 0 0 0)",
	whiteSpace: "nowrap",
	border: 0,
};

export interface OutlierRow {
	readonly key: string;
	readonly referenceId: string;
	readonly name: string;
	readonly closedDate: string;
	readonly days: number;
	readonly timesShort: string;
	readonly timesSpoken: string;
}

interface OutlierPopoverContentProps {
	readonly wording: OutlierWording;
	readonly count: number;
	readonly cut: number;
	readonly median: number;
	readonly rows: readonly OutlierRow[];
	readonly moreCount: number;
	/** Set when a percentile or SLE line lies above the axis. */
	readonly aboveTheAxis: string | null;
	readonly onOpenInList: () => void;
}

const OutlierPopoverContent: React.FC<OutlierPopoverContentProps> = ({
	wording,
	count,
	cut,
	median,
	rows,
	moreCount,
	aboveTheAxis,
	onOpenInList,
}) => (
	<Box sx={{ p: 2, maxWidth: 520 }}>
		<Typography variant="subtitle2" component="h3">
			{wording.popoverTitle(count)}
		</Typography>
		<Typography variant="body2">{wording.notDrawn(cut, median)}</Typography>
		<Typography variant="body2">{wording.stillCounted}</Typography>
		{aboveTheAxis && <Typography variant="body2">{aboveTheAxis}</Typography>}
		<Table size="small" sx={{ my: 1 }}>
			<TableHead>
				<TableRow>
					<TableCell>{wording.columns.reference}</TableCell>
					<TableCell>{wording.columns.name}</TableCell>
					<TableCell>{wording.columns.closed}</TableCell>
					<TableCell align="right">{wording.columns.days}</TableCell>
					<TableCell align="right">{wording.columns.times}</TableCell>
				</TableRow>
			</TableHead>
			<TableBody>
				{rows.map((row) => (
					<TableRow key={row.key}>
						<TableCell>{row.referenceId}</TableCell>
						<TableCell>{row.name}</TableCell>
						<TableCell>{row.closedDate}</TableCell>
						<TableCell align="right">{row.days}</TableCell>
						<TableCell align="right">
							<span aria-hidden="true">{row.timesShort}</span>
							<Box component="span" sx={visuallyHidden}>
								{row.timesSpoken}
							</Box>
						</TableCell>
					</TableRow>
				))}
			</TableBody>
		</Table>
		{moreCount > 0 && (
			<Typography variant="body2" color="text.secondary">
				{wording.moreRows(moreCount)}
			</Typography>
		)}
		<Button size="small" onClick={onOpenInList}>
			{wording.openInAList}
		</Button>
	</Box>
);

export default OutlierPopoverContent;
