import AddIcon from "@mui/icons-material/Add";
import DeleteIcon from "@mui/icons-material/Delete";
import EditIcon from "@mui/icons-material/Edit";
import {
	Alert,
	Box,
	CircularProgress,
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
import { Link, useNavigate } from "react-router";
import ActionButton from "../../../components/Common/ActionButton/ActionButton";
import ReconnectBanner from "../../../components/Common/Connections/ReconnectBanner";
import DataGridBase from "../../../components/Common/DataGrid/DataGridBase";
import type { DataGridColumn } from "../../../components/Common/DataGrid/types";
import DeleteConfirmationDialog from "../../../components/Common/DeleteConfirmationDialog/DeleteConfirmationDialog";
import {
	CONNECTIONS_REFUSED_MESSAGE,
	useConfiguredConnections,
	useForgetConfiguredConnections,
} from "../../../hooks/useConfiguredConnections";
import {
	editConnectionPath,
	newConnectionPath,
	settingsTabPath,
} from "../../../models/Settings/settingsAreas";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import type { IWorkTrackingSystemConnection } from "../../../models/WorkTracking/WorkTrackingSystemConnection";
import { ApiError } from "../../../services/Api/ApiError";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import { useTerminology } from "../../../services/TerminologyContext";
import { reportFailedNavigation } from "../../../utils/navigation";

type ConnectionRow = IWorkTrackingSystemConnection & GridValidRowModel;

const linkStyle = (color: string) => ({
	color,
	textDecoration: "none",
	fontWeight: 500,
});

const underlineOnHover = { "&:hover": { textDecoration: "underline" } };

/** The configured connections, once the list is known: their reconnect banners and the list with Edit and Delete. */
const ConnectionsList: React.FC<{
	readonly connections: IWorkTrackingSystemConnection[];
	readonly onChanged: () => void;
}> = ({ connections, onChanged }) => {
	const theme = useTheme();
	const { getTerm } = useTerminology();
	const connectionTerm = getTerm(TERMINOLOGY_KEYS.WORK_TRACKING_SYSTEM);
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
			onChanged();
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

	return (
		<>
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

			{connections
				.filter((connection) => connection.requiresReconnect === true)
				.map((connection) => (
					<Box key={`reconnect-${connection.id}`} sx={{ mb: 2 }}>
						<ReconnectBanner
							connection={connection}
							onReconnected={onChanged}
						/>
					</Box>
				))}

			{connections.length > 0 && (
				<Box data-testid="connections-datagrid-container">
					<DataGridBase
						rows={connections as ConnectionRow[]}
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
		</>
	);
};

/** System, Connections: the one place a System Admin sets up, edits and removes connections. */
const ConnectionsSettings: React.FC = () => {
	const theme = useTheme();
	const navigate = useNavigate();
	const { getTerm } = useTerminology();
	const connectionTerm = getTerm(TERMINOLOGY_KEYS.WORK_TRACKING_SYSTEM);
	const connectionsTerm = getTerm(TERMINOLOGY_KEYS.WORK_TRACKING_SYSTEMS);
	const list = useConfiguredConnections();
	const forgetConnections = useForgetConfiguredConnections();

	if (list.status === "loading") {
		return (
			<Box sx={{ display: "flex", justifyContent: "center", py: 4 }}>
				<CircularProgress aria-label={`Loading the ${connectionsTerm}`} />
			</Box>
		);
	}
	if (list.status === "refused") {
		return (
			<Alert severity="warning" data-testid="connections-refused-alert">
				{CONNECTIONS_REFUSED_MESSAGE}
			</Alert>
		);
	}
	if (list.status === "failed") {
		return (
			<Alert severity="error" data-testid="connections-failed-alert">
				Could not load the {connectionsTerm}.
			</Alert>
		);
	}

	const connectNewOne = () => {
		reportFailedNavigation(navigate(newConnectionPath()));
		return Promise.resolve();
	};

	return (
		<Box sx={{ pb: 4 }}>
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
					component="h2"
					sx={{
						fontWeight: 600,
						color: theme.palette.primary.main,
						textTransform: "capitalize",
					}}
				>
					{connectionsTerm}
				</Typography>
				<ActionButton
					buttonText={`Connect ${connectionTerm}`}
					startIcon={<AddIcon />}
					onClickHandler={connectNewOne}
					buttonVariant="contained"
				/>
			</Box>
			<ConnectionsList
				connections={list.connections}
				onChanged={forgetConnections}
			/>
		</Box>
	);
};

export default ConnectionsSettings;
