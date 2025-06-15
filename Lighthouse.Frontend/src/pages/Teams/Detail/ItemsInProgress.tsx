import { Card, CardContent, Chip, Typography } from "@mui/material";
import { useState } from "react";
import WorkItemsDialog from "../../../components/Common/WorkItemsDialog/WorkItemsDialog";
import type { IWorkItem } from "../../../models/WorkItem";

interface ItemsInProgressProps {
	title: string;
	items: IWorkItem[];
	idealWip?: number;
	sle?: number;
}

const ItemsInProgress: React.FC<ItemsInProgressProps> = ({
	title,
	items,
	idealWip,
	sle,
}) => {
	const [open, setOpen] = useState(false);
	const count = items.length;

	const getChipColor = () => {
		if (idealWip == null) return "default";
		if (count === idealWip) return "success";
		if (count < idealWip) return "info";
		return "error";
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

			<WorkItemsDialog
				title={title}
				items={items}
				open={open}
				onClose={handleClose}
				timeMetric="age"
				sle={sle}
			/>
		</>
	);
};

export default ItemsInProgress;
