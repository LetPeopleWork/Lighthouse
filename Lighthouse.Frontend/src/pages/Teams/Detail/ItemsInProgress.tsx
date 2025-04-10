import CloseIcon from "@mui/icons-material/Close";
import {
	Card,
	CardContent,
	Chip,
	Dialog,
	DialogContent,
	DialogTitle,
	IconButton,
	Link,
	Table,
	TableBody,
	TableCell,
	TableRow,
	Typography,
} from "@mui/material";
import { useState } from "react";
import type { IWorkItem } from "../../../models/WorkItem";

interface ItemsInProgressProps {
	title: string;
	items: IWorkItem[];
	idealWip?: number;
}

const ItemsInProgress: React.FC<ItemsInProgressProps> = ({
	title,
	items,
	idealWip,
}) => {
	const [open, setOpen] = useState(false);
	const count = items.length;

	const getChipColor = () => {
		if (idealWip == null) return "default";
		if (count === idealWip) return "success";
		if (count < idealWip) return "info";
		return "warning";
	};

	const handleOpen = () => setOpen(true);
	const handleClose = () => setOpen(false);

	return (
		<>
			<Card
				sx={{ m: 2, p: 1, borderRadius: 2, cursor: "pointer" }}
				onClick={handleOpen}
			>
				<CardContent sx={{ display: "flex", alignItems: "center", gap: 1 }}>
					<Typography variant="h6" sx={{ flexGrow: 1 }}>
						{title}
					</Typography>
					<Typography variant="h4">{count}</Typography>
					{idealWip != null && idealWip > 0 && (
						<Chip
							label={`Goal: ${idealWip}`}
							color={getChipColor()}
							size="small"
						/>
					)}
				</CardContent>
			</Card>

			<Dialog open={open} onClose={handleClose} fullWidth maxWidth="md">
				<DialogTitle>
					{title}
					<IconButton
						onClick={handleClose}
						sx={{ position: "absolute", right: 8, top: 8 }}
					>
						<CloseIcon />
					</IconButton>
				</DialogTitle>
				<DialogContent>
					{count > 0 ? (
						<Table>
							<TableBody>
								{items.map((item) => (
									<TableRow key={item.id}>
										<TableCell>
											{item.url ? (
												<Link
													href={item.url}
													target="_blank"
													rel="noopener noreferrer"
												>
													{item.name}
												</Link>
											) : (
												item.name
											)}
										</TableCell>
										<TableCell>{item.type}</TableCell>
										<TableCell>{item.state}</TableCell>
									</TableRow>
								))}
							</TableBody>
						</Table>
					) : (
						<Typography variant="body2" color="text.secondary">
							No items currently in progress
						</Typography>
					)}
				</DialogContent>
			</Dialog>
		</>
	);
};

export default ItemsInProgress;
