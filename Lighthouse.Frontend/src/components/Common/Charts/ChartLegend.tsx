import { Box, Button, Typography } from "@mui/material";
import { type ReactElement, useState } from "react";

export interface ChartLegendItem {
	id: string;
	label: string;
	color: string;
}

export interface ChartLegendProps {
	items: ChartLegendItem[];
	hidden: ReadonlySet<string>;
	onToggle: (id: string) => void;
	onShowAll: () => void;
}

const DOT_SIZE = 12;
const HIDDEN_OPACITY = 0.4;

interface ChartLegendEntryProps {
	item: ChartLegendItem;
	visible: boolean;
	onToggle: (id: string) => void;
}

const ChartLegendEntry = ({
	item,
	visible,
	onToggle,
}: ChartLegendEntryProps): ReactElement => (
	<Box
		component="button"
		type="button"
		onClick={() => onToggle(item.id)}
		aria-pressed={visible}
		sx={{
			display: "flex",
			alignItems: "center",
			gap: 0.75,
			border: "none",
			background: "none",
			cursor: "pointer",
			p: 0,
			color: "text.primary",
			opacity: visible ? 1 : HIDDEN_OPACITY,
		}}
	>
		<Box
			sx={{
				width: DOT_SIZE,
				height: DOT_SIZE,
				borderRadius: "50%",
				backgroundColor: item.color,
			}}
		/>
		<Typography variant="body2">{item.label}</Typography>
	</Box>
);

// Collapsed by default: on a real delivery the entries wrap to eight lines and the Metrics card
// already runs tall, while filtering is a special-case action worth one click (Epic #5585 US-04).
const ChartLegend = ({
	items,
	hidden,
	onToggle,
	onShowAll,
}: ChartLegendProps): ReactElement => {
	const [expanded, setExpanded] = useState(false);

	return (
		<Box sx={{ mt: 1 }}>
			<Box sx={{ display: "flex", alignItems: "center", gap: 1 }}>
				<Button
					size="small"
					variant="text"
					aria-expanded={expanded}
					onClick={() => setExpanded((previous) => !previous)}
				>
					{`Legend (${items.length})`}
				</Button>
				{hidden.size > 0 && (
					<Button size="small" variant="text" onClick={onShowAll}>
						Show all
					</Button>
				)}
			</Box>
			{expanded && (
				<Box sx={{ display: "flex", flexWrap: "wrap", gap: 1.5, mt: 1 }}>
					{items.map((item) => (
						<ChartLegendEntry
							key={item.id}
							item={item}
							visible={!hidden.has(item.id)}
							onToggle={onToggle}
						/>
					))}
				</Box>
			)}
		</Box>
	);
};

export default ChartLegend;
