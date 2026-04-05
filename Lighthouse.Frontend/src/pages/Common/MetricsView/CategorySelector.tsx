import { Box, Chip } from "@mui/material";
import type React from "react";
import type { CategoryKey } from "./categoryMetadata";
import { getCategories } from "./categoryMetadata";

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
		<Box display="flex" gap={1} flexWrap="wrap" data-testid="category-selector">
			{categories.map((cat) => (
				<Chip
					key={cat.key}
					label={cat.displayName}
					variant={selectedCategory === cat.key ? "filled" : "outlined"}
					color={selectedCategory === cat.key ? "primary" : "default"}
					onClick={() => onSelectCategory(cat.key)}
					data-testid={`category-chip-${cat.key}`}
					size="small"
				/>
			))}
		</Box>
	);
};

export default CategorySelector;
