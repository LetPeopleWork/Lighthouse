import BlockIcon from "@mui/icons-material/Block";
import CloseIcon from "@mui/icons-material/Close";
import {
	Box,
	Chip,
	Dialog,
	DialogContent,
	DialogTitle,
	IconButton,
	Link,
	Tooltip,
	Typography,
} from "@mui/material";
import type { GridValidRowModel } from "@mui/x-data-grid";
import { useCallback, useMemo } from "react";
import type { IFeature } from "../../../models/Feature";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import type { IWorkItem } from "../../../models/WorkItem";
import { useTerminology } from "../../../services/TerminologyContext";
import {
	certainColor,
	confidentColor,
	getStateColor,
	hexToRgba,
	realisticColor,
	riskyColor,
} from "../../../utils/theme/colors";
import DataGridBase from "../DataGrid/DataGridBase";
import type { DataGridColumn } from "../DataGrid/types";

export interface WorkItemsDialogProps {
	title: string;
	items: IWorkItem[];
	open: boolean;
	onClose: () => void;
	additionalColumnTitle: string;
	additionalColumnDescription: string;
	additionalColumnContent: (workItem: IWorkItem) => number;
	sle?: number;
}

const WorkItemsDialog: React.FC<WorkItemsDialogProps> = ({
	title,
	items,
	open,
	onClose,
	additionalColumnTitle,
	additionalColumnDescription,
	additionalColumnContent,
	sle,
}) => {
	const { getTerm } = useTerminology();
	const workItemTerm = getTerm(TERMINOLOGY_KEYS.WORK_ITEM);
	const blockedTerm = getTerm(TERMINOLOGY_KEYS.BLOCKED);

	// Check if items are Features with owning team
	const isFeature = useCallback((item: IWorkItem): item is IFeature => {
		return "owningTeam" in item;
	}, []);

	const hasOwningTeams = items.some(
		(item) =>
			isFeature(item) && item.owningTeam && item.owningTeam.trim() !== "",
	);

	const sortedItems = [...items].sort((a, b) => {
		return additionalColumnContent(b) - additionalColumnContent(a);
	});

	const getColumnColor = useCallback(
		(value: number) => {
			if (!sle) return undefined;

			const seventyPercentSLE = sle * 0.7;
			const fiftyPercentSLE = sle * 0.5;

			// Using updated forecast colors with better contrast
			if (value > sle) {
				return riskyColor; // Enhanced red
			}
			if (value >= seventyPercentSLE) {
				return realisticColor; // Enhanced orange
			}
			if (value >= fiftyPercentSLE) {
				return confidentColor; // Enhanced light green
			}
			return certainColor; // Enhanced green
		},
		[sle],
	);

	// Define columns for DataGrid
	const columns = useMemo(() => {
		const baseColumns: DataGridColumn<IWorkItem & GridValidRowModel>[] = [
			{
				field: "referenceId",
				headerName: "ID",
				width: 120,
				renderCell: ({ row }) => {
					return row.name?.toLowerCase().includes("unparented")
						? ""
						: row.referenceId;
				},
			},
			{
				field: "name",
				headerName: "Name",
				width: 300,
				hideable: false,
				flex: 1,
				renderCell: ({ row }) => {
					if (row.url) {
						return (
							<Link href={row.url} target="_blank" rel="noopener noreferrer">
								{row.name}
							</Link>
						);
					}
					return row.name;
				},
			},
			{
				field: "type",
				headerName: "Type",
				width: 120,
			},
			{
				field: "state",
				headerName: "State",
				width: 150,
				renderCell: ({ row }) => (
					<Chip
						size="small"
						label={row.state}
						color={getStateColor(row.stateCategory)}
						variant="outlined"
					/>
				),
			},
		];

		// Add Owned by column if needed
		if (hasOwningTeams) {
			baseColumns.push({
				field: "owningTeam",
				headerName: "Owned by",
				width: 150,
				renderCell: ({ row }) => {
					return isFeature(row) ? row.owningTeam : "";
				},
			});
		}

		// Add additional column
		baseColumns.push({
			field: "additionalColumn",
			headerName: `${additionalColumnTitle} (${additionalColumnDescription})`,
			width: 200,
			sortable: true,
			valueGetter: (_, row) => additionalColumnContent(row),
			renderCell: ({ row }) => {
				const value = additionalColumnContent(row);
				return (
					<Typography
						variant="body2"
						data-testid="additionalColumnContent"
						sx={{
							color: getColumnColor(value),
							fontWeight: sle ? "bold" : "normal",
							padding: "4px 8px",
							borderRadius: 1,
							display: "inline-flex",
							alignItems: "center",
							backgroundColor: (theme) => {
								const timeColor = getColumnColor(value);
								return timeColor
									? hexToRgba(timeColor ?? theme.palette.text.primary, 0.1)
									: "transparent";
							},
						}}
					>
						{value}
						{row.isBlocked && (
							<Tooltip title={`This ${workItemTerm} is ${blockedTerm}`}>
								<BlockIcon
									sx={{
										color: "error.main",
										fontSize: "1rem",
										ml: 1,
									}}
								/>
							</Tooltip>
						)}
					</Typography>
				);
			},
		});

		return baseColumns;
	}, [
		hasOwningTeams,
		additionalColumnTitle,
		additionalColumnDescription,
		additionalColumnContent,
		sle,
		workItemTerm,
		blockedTerm,
		isFeature,
		getColumnColor,
	]);

	return (
		<Dialog open={open} onClose={onClose} fullWidth maxWidth="md">
			<DialogTitle sx={{ backgroundColor: "background.paper" }}>
				{title}
				<IconButton
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
							rows={sortedItems as (IWorkItem & GridValidRowModel)[]}
							columns={columns}
							storageKey="work-items-dialog"
							initialSortModel={[
								{ field: "additionalColumn", sort: "desc" as const },
							]}
							enableExport={true}
							exportFileName={title.replaceAll(/\s+/g, "_")}
						/>
					</Box>
				) : (
					<Typography variant="body2" color="text.secondary">
						No items to display
					</Typography>
				)}
			</DialogContent>
		</Dialog>
	);
};

export default WorkItemsDialog;
