import DeleteIcon from "@mui/icons-material/Delete";
import EditIcon from "@mui/icons-material/Edit";
import { Box, IconButton, Tooltip, useTheme } from "@mui/material";
import type { GridValidRowModel } from "@mui/x-data-grid";
import type React from "react";
import { useMemo } from "react";
import DataGridBase from "../../../components/Common/DataGrid/DataGridBase";
import type { DataGridColumn } from "../../../components/Common/DataGrid/types";
import type { IWorkTrackingSystemConnection } from "../../../models/WorkTracking/WorkTrackingSystemConnection";

interface ConnectionDetailTableProps {
	workTrackingSystemConnections: IWorkTrackingSystemConnection[];
	onEditConnectionButtonClicked: (
		system: IWorkTrackingSystemConnection,
	) => void;
	handleDeleteConnection: (system: IWorkTrackingSystemConnection) => void;
}

const ConnectionDetailTable: React.FC<ConnectionDetailTableProps> = ({
	workTrackingSystemConnections,
	onEditConnectionButtonClicked,
	handleDeleteConnection,
}) => {
	const theme = useTheme();

	const columns: DataGridColumn<
		IWorkTrackingSystemConnection & GridValidRowModel
	>[] = useMemo(
		() => [
			{
				field: "name",
				headerName: "Name",
				flex: 1,
				hideable: false,
				sortable: true,
			},
			{
				field: "actions",
				headerName: "Actions",
				width: 150,
				sortable: false,
				hideable: false,
				renderCell: ({ row }) => (
					<Box
						sx={{
							display: "flex",
							justifyContent: "flex-start",
							gap: 1,
							width: "100%",
						}}
					>
						<Tooltip title="Edit">
							<IconButton
								onClick={() => onEditConnectionButtonClicked(row)}
								size="medium"
								sx={{
									color: theme.palette.primary.main,
									transition: "transform 0.2s",
									"&:hover": { transform: "scale(1.1)" },
								}}
								data-testid="edit-connection-button"
							>
								<EditIcon fontSize="medium" />
							</IconButton>
						</Tooltip>
						<Tooltip title="Delete">
							<IconButton
								onClick={() => handleDeleteConnection(row)}
								size="medium"
								sx={{
									color: theme.palette.primary.main,
									transition: "transform 0.2s",
									"&:hover": { transform: "scale(1.1)" },
								}}
								data-testid="delete-connection-button"
							>
								<DeleteIcon fontSize="medium" />
							</IconButton>
						</Tooltip>
					</Box>
				),
			},
		],
		[theme, onEditConnectionButtonClicked, handleDeleteConnection],
	);

	return (
		<DataGridBase
			rows={
				workTrackingSystemConnections as (IWorkTrackingSystemConnection &
					GridValidRowModel)[]
			}
			columns={columns}
			storageKey="connection-detail-table"
		/>
	);
};

export default ConnectionDetailTable;
