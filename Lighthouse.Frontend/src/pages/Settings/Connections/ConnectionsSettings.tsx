import DeleteIcon from "@mui/icons-material/Delete";
import EditIcon from "@mui/icons-material/Edit";
import {
	Alert,
	Box,
	Container,
	Fade,
	IconButton,
	Link as MuiLink,
	Tooltip,
	Typography,
	useTheme,
} from "@mui/material";
import type { GridValidRowModel } from "@mui/x-data-grid";
import type React from "react";
import { useCallback, useContext, useMemo, useState } from "react";
import { Link } from "react-router";
import ReconnectBanner from "../../../components/Common/Connections/ReconnectBanner";
import DataGridBase from "../../../components/Common/DataGrid/DataGridBase";
import type { DataGridColumn } from "../../../components/Common/DataGrid/types";
import DeleteConfirmationDialog from "../../../components/Common/DeleteConfirmationDialog/DeleteConfirmationDialog";
import {
	editConnectionPath,
	settingsTabPath,
} from "../../../models/Settings/settingsAreas";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import type { IWorkTrackingSystemConnection } from "../../../models/WorkTracking/WorkTrackingSystemConnection";
import { ApiError } from "../../../services/Api/ApiError";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import { useTerminology } from "../../../services/TerminologyContext";

type ConnectionRow = IWorkTrackingSystemConnection & GridValidRowModel;

interface ConnectionsSettingsProps {
	readonly connections: IWorkTrackingSystemConnection[];
	readonly filterText: string;
	readonly onConnectionDeleted: (connectionId: number) => void;
}

const linkStyle = (color: string) => ({
	color,
	textDecoration: "none",
	fontWeight: 500,
});

const underlineOnHover = { "&:hover": { textDecoration: "underline" } };

/** The configured connections: their reconnect banners, the list with Edit and Delete, or the way to a first one. */
const ConnectionsSettings: React.FC<ConnectionsSettingsProps> = ({
	connections,
	filterText,
	onConnectionDeleted,
}) => {
	const theme = useTheme();
	const { getTerm } = useTerminology();
	const connectionTerm = getTerm(TERMINOLOGY_KEYS.WORK_TRACKING_SYSTEM);
	const connectionsTerm = getTerm(TERMINOLOGY_KEYS.WORK_TRACKING_SYSTEMS);
	const { workTrackingSystemService } = useContext(ApiServiceContext);

	const [selectedConnection, setSelectedConnection] =
		useState<IWorkTrackingSystemConnection | null>(null);
	const [deleteError, setDeleteError] = useState<string | null>(null);

	const askToDelete = useCallback(
		(connection: IWorkTrackingSystemConnection) => {
			setSelectedConnection(connection);
			setDeleteError(null);
		},
		[],
	);

	const closeDeleteDialog = useCallback(() => {
		setSelectedConnection(null);
		setDeleteError(null);
	}, []);

	const confirmDelete = async () => {
		if (!selectedConnection?.id) return;

		try {
			await workTrackingSystemService.deleteWorkTrackingSystemConnection(
				selectedConnection.id,
			);
			onConnectionDeleted(selectedConnection.id);
			closeDeleteDialog();
		} catch (error) {
			if (error instanceof ApiError && error.code === 409) {
				setDeleteError(error.message);
			} else {
				setDeleteError("Failed to delete connection. Please try again.");
			}
		}
	};

	const iconButtonStyle = useMemo(
		() => ({
			color: theme.palette.primary.main,
			transition: "transform 0.2s",
			"&:hover": { transform: "scale(1.1)" },
		}),
		[theme],
	);

	const columns: DataGridColumn<ConnectionRow>[] = useMemo(
		() => [
			{
				field: "name",
				headerName: "Name",
				flex: 1,
				hideable: false,
				sortable: true,
				renderCell: ({ row }) => (
					<Link
						to={editConnectionPath(row.id)}
						style={{
							textDecoration: "none",
							color: theme.palette.primary.main,
							fontWeight: "bold",
						}}
					>
						{row.name}
					</Link>
				),
			},
			{
				field: "workTrackingSystem",
				headerName: "Type",
				width: 180,
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
							justifyContent: "flex-end",
							gap: 1,
							width: "100%",
						}}
					>
						<Tooltip title="Edit">
							<IconButton
								component={Link}
								to={editConnectionPath(row.id)}
								size="medium"
								sx={iconButtonStyle}
								data-testid="edit-connection-button"
							>
								<EditIcon fontSize="medium" />
							</IconButton>
						</Tooltip>
						<Tooltip title="Delete">
							<IconButton
								onClick={() => askToDelete(row)}
								size="medium"
								sx={iconButtonStyle}
								data-testid="delete-connection-button"
							>
								<DeleteIcon fontSize="medium" />
							</IconButton>
						</Tooltip>
					</Box>
				),
			},
		],
		[theme, iconButtonStyle, askToDelete],
	);

	const filteredConnections = useMemo(
		() =>
			connections.filter((connection) =>
				connection.name.toLowerCase().includes(filterText.toLowerCase()),
			),
		[connections, filterText],
	);

	return (
		<Container maxWidth={false} sx={{ pb: 4 }}>
			<Box
				sx={{
					display: "flex",
					justifyContent: "space-between",
					alignItems: "center",
					gap: 2,
					mb: 2,
				}}
			>
				<Typography
					variant="h5"
					sx={{
						fontWeight: 600,
						color: theme.palette.primary.main,
						textTransform: "capitalize",
					}}
				>
					{connectionsTerm}
				</Typography>
			</Box>

			{connections.length === 0 && (
				<Fade in={true} timeout={800}>
					<Alert
						severity="info"
						variant="outlined"
						sx={{ mb: 3, borderRadius: 2, boxShadow: theme.shadows[1] }}
						data-testid="no-connections-alert"
					>
						<Typography variant="body1">
							No {connectionTerm} found.{" "}
							<MuiLink
								component={Link}
								to={settingsTabPath("system", "demo-data")}
								style={linkStyle(theme.palette.primary.main)}
								sx={underlineOnHover}
							>
								Load Demo Data
							</MuiLink>{" "}
							or{" "}
							<MuiLink
								href="https://docs.lighthouse.letpeople.work"
								target="_blank"
								rel="noopener noreferrer"
								style={linkStyle(theme.palette.primary.main)}
								sx={underlineOnHover}
							>
								Check the documentation
							</MuiLink>{" "}
							for more information.
						</Typography>
					</Alert>
				</Fade>
			)}

			{connections.length > 0 && filteredConnections.length === 0 && (
				<Fade in={true} timeout={500}>
					<Alert
						severity="warning"
						variant="outlined"
						sx={{ mb: 3, borderRadius: 2, boxShadow: theme.shadows[1] }}
					>
						No connections found matching the filter{" "}
						<strong>{filterText}</strong>
					</Alert>
				</Fade>
			)}

			{filteredConnections
				.filter((connection) => connection.requiresReconnect === true)
				.map((connection) => (
					<Box key={`reconnect-${connection.id}`} sx={{ mb: 2 }}>
						<ReconnectBanner connection={connection} />
					</Box>
				))}

			{filteredConnections.length > 0 && (
				<Box data-testid="connections-datagrid-container">
					<DataGridBase
						rows={filteredConnections as ConnectionRow[]}
						columns={columns}
						storageKey="overview-connection-table"
					/>
				</Box>
			)}

			{selectedConnection && (
				<DeleteConfirmationDialog
					open={true}
					itemName={selectedConnection.name}
					onConfirm={confirmDelete}
					onCancel={closeDeleteDialog}
					errorMessage={deleteError ?? undefined}
				/>
			)}
		</Container>
	);
};

export default ConnectionsSettings;
