import CloseIcon from "@mui/icons-material/Close";
import {
	Box,
	Dialog,
	DialogContent,
	DialogTitle,
	IconButton,
	Link,
	Typography,
} from "@mui/material";
import type { GridValidRowModel } from "@mui/x-data-grid";
import { useId, useMemo } from "react";
import type { ICumulativeStateTimeItemRow } from "../../../models/Metrics/CumulativeStateTimeItems";
import DataGridBase from "../DataGrid/DataGridBase";
import type { DataGridColumn } from "../DataGrid/types";

export interface CumulativeStateTimeDrillDownDialogProps {
	open: boolean;
	state: string;
	items: ICumulativeStateTimeItemRow[];
	onClose: () => void;
}

type DrillDownRow = ICumulativeStateTimeItemRow & GridValidRowModel;

const EMPTY_MESSAGE =
	"No items contributed to this state in the selected window.";

const CumulativeStateTimeDrillDownDialog: React.FC<
	CumulativeStateTimeDrillDownDialogProps
> = ({ open, state, items, onClose }) => {
	const titleId = useId();
	const title = `Items contributing to ${state}`;

	const columns = useMemo<DataGridColumn<DrillDownRow>[]>(
		() => [
			{
				field: "referenceId",
				headerName: "Work Item ID",
				width: 140,
				renderCell: ({ row }) =>
					row.url ? (
						<Link href={row.url} target="_blank" rel="noopener noreferrer">
							{row.referenceId}
						</Link>
					) : (
						row.referenceId
					),
			},
			{
				field: "title",
				headerName: "Title",
				width: 280,
				flex: 1,
			},
			{
				field: "type",
				headerName: "Type",
				width: 120,
			},
			{
				field: "state",
				headerName: "Current State",
				width: 150,
			},
			{
				field: "daysContributed",
				headerName: "Days Contributed",
				width: 160,
				sortable: true,
			},
		],
		[],
	);

	return (
		<Dialog
			open={open}
			onClose={onClose}
			fullWidth
			maxWidth="md"
			aria-labelledby={titleId}
		>
			<DialogTitle id={titleId} sx={{ backgroundColor: "background.paper" }}>
				{title}
				<IconButton
					aria-label="Close"
					onClick={onClose}
					sx={{ position: "absolute", right: 8, top: 8 }}
				>
					<CloseIcon />
				</IconButton>
			</DialogTitle>
			<DialogContent sx={{ backgroundColor: "background.paper" }}>
				{items.length > 0 ? (
					<Box sx={{ mt: 2 }}>
						<DataGridBase
							rows={items as DrillDownRow[]}
							columns={columns}
							idField="referenceId"
							storageKey="cumulative-state-time-drill-down"
							initialSortModel={[{ field: "daysContributed", sort: "desc" }]}
							emptyStateMessage={EMPTY_MESSAGE}
						/>
					</Box>
				) : (
					<Typography variant="body2" color="text.secondary">
						{EMPTY_MESSAGE}
					</Typography>
				)}
			</DialogContent>
		</Dialog>
	);
};

export default CumulativeStateTimeDrillDownDialog;
