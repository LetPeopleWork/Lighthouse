import ArrowDownwardIcon from "@mui/icons-material/ArrowDownward";
import ArrowUpwardIcon from "@mui/icons-material/ArrowUpward";
import {
	Box,
	Button,
	Dialog,
	DialogActions,
	DialogContent,
	DialogTitle,
	IconButton,
	List,
	ListItem,
	ListItemText,
} from "@mui/material";
import type { GridColDef } from "@mui/x-data-grid";
import { useEffect, useState } from "react";

interface ColumnOrderDialogProps {
	open: boolean;
	onClose: () => void;
	columns: GridColDef[];
	columnOrder: string[];
	onSave: (order: string[]) => void;
}

const ColumnOrderDialog: React.FC<ColumnOrderDialogProps> = ({
	open,
	onClose,
	columns,
	columnOrder,
	onSave,
}) => {
	const [order, setOrder] = useState<string[]>([]);

	useEffect(() => {
		if (columnOrder && columnOrder.length > 0) setOrder(columnOrder);
		else setOrder(columns.map((c) => c.field));
	}, [columnOrder, columns]);

	const move = (index: number, toIndex: number) => {
		setOrder((prev) => {
			const copy = [...prev];
			const [moved] = copy.splice(index, 1);
			copy.splice(toIndex, 0, moved);
			return copy;
		});
	};

	const handleSave = () => {
		onSave(order);
		onClose();
	};

	return (
		<Dialog open={open} onClose={onClose} maxWidth="xs" fullWidth>
			<DialogTitle>Reorder columns</DialogTitle>
			<DialogContent>
				<List>
					{order.map((field, i) => {
						const col = columns.find((c) => c.field === field);
						return (
							<ListItem key={field} divider>
								<ListItemText primary={col?.headerName ?? field} />
								<Box>
									<IconButton
										onClick={() => move(i, Math.max(0, i - 1))}
										disabled={i === 0}
										data-testid={`move-up-${field}`}
									>
										<ArrowUpwardIcon fontSize="small" />
									</IconButton>
									<IconButton
										onClick={() => move(i, Math.min(order.length - 1, i + 1))}
										disabled={i === order.length - 1}
										data-testid={`move-down-${field}`}
									>
										<ArrowDownwardIcon fontSize="small" />
									</IconButton>
								</Box>
							</ListItem>
						);
					})}
				</List>
			</DialogContent>
			<DialogActions>
				<Button onClick={onClose}>Cancel</Button>
				<Button onClick={handleSave} variant="contained" color="primary">
					Save
				</Button>
			</DialogActions>
		</Dialog>
	);
};

export default ColumnOrderDialog;
