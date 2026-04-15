import AccountTreeOutlinedIcon from "@mui/icons-material/AccountTreeOutlined";
import DashboardOutlinedIcon from "@mui/icons-material/DashboardOutlined";
import HourglassEmptyOutlinedIcon from "@mui/icons-material/HourglassEmptyOutlined";
import InsightsOutlinedIcon from "@mui/icons-material/InsightsOutlined";
import ShowChartOutlinedIcon from "@mui/icons-material/ShowChartOutlined";
import TimerOutlinedIcon from "@mui/icons-material/TimerOutlined";
import { Box, Chip, Tooltip } from "@mui/material";
import type React from "react";
import type { CategoryKey } from "./categoryMetadata";
import { getCategories } from "./categoryMetadata";

const iconMap: Record<string, React.ReactElement> = {
	Dashboard: <DashboardOutlinedIcon fontSize="small" />,
	Timer: <TimerOutlinedIcon fontSize="small" />,
	ShowChart: <ShowChartOutlinedIcon fontSize="small" />,
	HourglassEmpty: <HourglassEmptyOutlinedIcon fontSize="small" />,
	Insights: <InsightsOutlinedIcon fontSize="small" />,
	AccountTree: <AccountTreeOutlinedIcon fontSize="small" />,
};

interface CategorySelectorProps {
	selectedCategory: CategoryKey;
	onSelectCategory: (key: CategoryKey) => void;
}

const CategorySelector: React.FC<CategorySelectorProps> = ({
	selectedCategory,
	onSelectCategory,
}) => {
	const categories = getCategories();

	return (
		<Box
			sx={{ display: "flex", gap: 1, flexWrap: "wrap" }}
			data-testid="category-selector"
		>
			{categories.map((cat) => (
				<Tooltip key={cat.key} title={cat.hoverText} arrow>
					<Chip
						icon={iconMap[cat.icon]}
						label={cat.displayName}
						variant={selectedCategory === cat.key ? "filled" : "outlined"}
						color={selectedCategory === cat.key ? "primary" : "default"}
						onClick={() => onSelectCategory(cat.key)}
						data-testid={`category-chip-${cat.key}`}
						size="small"
					/>
				</Tooltip>
			))}
		</Box>
	);
};

export default CategorySelector;
