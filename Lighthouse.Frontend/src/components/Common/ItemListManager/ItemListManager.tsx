import DeleteIcon from "@mui/icons-material/Delete";
import {
	Button,
	IconButton,
	List,
	ListItem,
	TextField,
	Typography,
} from "@mui/material";
import Grid from "@mui/material/Grid2";
import type React from "react";
import { useState } from "react";

interface ItemListManagerProps {
	title: string;
	items: string[];
	onAddItem: (item: string) => void;
	onRemoveItem: (item: string) => void;
}

const ItemListManager: React.FC<ItemListManagerProps> = ({
	title,
	items,
	onAddItem,
	onRemoveItem,
}) => {
	const [newItem, setNewItem] = useState<string>("");

	const handleAddItem = () => {
		if (newItem.trim()) {
			onAddItem(newItem.trim());
			setNewItem("");
		}
	};

	const handleNewItemChange = (event: React.ChangeEvent<HTMLInputElement>) => {
		setNewItem(event.target.value);
	};

	return (
		<Grid container spacing={3}>
			<Grid size={{ xs: 3 }}>
				<List>
					{items
						.filter((item) => item.trim())
						.map((item) => (
							<ListItem key={item}>
								<Typography variant="body1">{item}</Typography>
								<IconButton
									aria-label="delete"
									edge="end"
									onClick={() => onRemoveItem(item)}
								>
									<DeleteIcon />
								</IconButton>
							</ListItem>
						))}
				</List>
			</Grid>
			<Grid size={{ xs: 9 }}>
				<TextField
					label={`New ${title}`}
					fullWidth
					margin="normal"
					value={newItem}
					onChange={handleNewItemChange}
				/>
				<Button variant="outlined" color="primary" onClick={handleAddItem}>
					Add {title}
				</Button>
			</Grid>
		</Grid>
	);
};

export default ItemListManager;
