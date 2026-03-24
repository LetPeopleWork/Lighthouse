import BackupIcon from "@mui/icons-material/Backup";
import DeleteForeverIcon from "@mui/icons-material/DeleteForever";
import RestoreIcon from "@mui/icons-material/Restore";
import StorageIcon from "@mui/icons-material/Storage";
import WarningAmberIcon from "@mui/icons-material/WarningAmber";
import LoadingButton from "@mui/lab/LoadingButton";
import Alert from "@mui/material/Alert";
import Box from "@mui/material/Box";
import Button from "@mui/material/Button";
import Chip from "@mui/material/Chip";
import Dialog from "@mui/material/Dialog";
import DialogActions from "@mui/material/DialogActions";
import DialogContent from "@mui/material/DialogContent";
import DialogTitle from "@mui/material/DialogTitle";
import Link from "@mui/material/Link";
import Paper from "@mui/material/Paper";
import Stack from "@mui/material/Stack";
import { useTheme } from "@mui/material/styles";
import TextField from "@mui/material/TextField";
import Typography from "@mui/material/Typography";
import useMediaQuery from "@mui/material/useMediaQuery";
import type React from "react";
import {
	useCallback,
	useContext,
	useEffect,
	useId,
	useRef,
	useState,
} from "react";
import type {
	IDatabaseCapabilityStatus,
	IDatabaseOperationStatus,
} from "../../../models/DatabaseManagement/DatabaseManagementTypes";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";

const DatabaseManagementSettings: React.FC = () => {
	const [status, setStatus] = useState<IDatabaseCapabilityStatus | null>(null);
	const [backupPassword, setBackupPassword] = useState("");
	const [restorePassword, setRestorePassword] = useState("");
	const [restoreFile, setRestoreFile] = useState<File | null>(null);
	const [operationLoading, setOperationLoading] = useState<string | null>(null);
	const [error, setError] = useState<string | null>(null);
	const [success, setSuccess] = useState<string | null>(null);
	const [clearDialogOpen, setClearDialogOpen] = useState(false);

	const theme = useTheme();
	const isMobile = useMediaQuery(theme.breakpoints.down("md"));
	const { databaseManagementService } = useContext(ApiServiceContext);
	const fileInputRef = useRef<HTMLInputElement>(null);

	const dialogTitleId = useId();
	const dialogDescriptionId = useId();

	const fetchStatus = useCallback(async () => {
		try {
			const capabilityStatus = await databaseManagementService.getStatus();
			setStatus(capabilityStatus);
		} catch (err) {
			console.error("Failed to fetch database capability status:", err);
		}
	}, [databaseManagementService]);

	useEffect(() => {
		fetchStatus();
	}, [fetchStatus]);

	const isBlocked = status?.isOperationBlocked ?? false;
	const toolingMissing = status !== null && !status.isToolingAvailable;

	const handleBackup = async () => {
		if (!backupPassword) return;

		setOperationLoading("backup");
		setError(null);
		setSuccess(null);

		try {
			const result =
				await databaseManagementService.createBackup(backupPassword);

			if (result.state === "Failed") {
				setError(result.failureReason ?? "Backup failed.");
				return;
			}

			const blob = await databaseManagementService.downloadBackupArtifact(
				result.operationId,
			);
			downloadBlob(
				blob,
				`Lighthouse_Backup_${new Date().toISOString().slice(0, 10)}.zip`,
			);
			setSuccess("Backup created and downloaded successfully.");
			setBackupPassword("");
		} catch (err) {
			setError(err instanceof Error ? err.message : "Backup failed.");
		} finally {
			setOperationLoading(null);
			await fetchStatus();
		}
	};

	const handleRestore = async () => {
		if (!restoreFile || !restorePassword) return;

		setOperationLoading("restore");
		setError(null);
		setSuccess(null);

		try {
			const result = await databaseManagementService.restoreBackup(
				restoreFile,
				restorePassword,
			);

			if (result.state === "Failed") {
				setError(result.failureReason ?? "Restore failed.");
				return;
			}

			setSuccess(
				"Restore completed. The application will restart to apply the changes.",
			);
			setRestoreFile(null);
			setRestorePassword("");
			if (fileInputRef.current) {
				fileInputRef.current.value = "";
			}

			if (result.state === "RestartPending") {
				await pollUntilRestart(result.operationId);
			}
		} catch (err) {
			setError(err instanceof Error ? err.message : "Restore failed.");
		} finally {
			setOperationLoading(null);
			await fetchStatus();
		}
	};

	const handleClear = async () => {
		setClearDialogOpen(false);
		setOperationLoading("clear");
		setError(null);
		setSuccess(null);

		try {
			const result = await databaseManagementService.clearDatabase();

			if (result.state === "Failed") {
				setError(result.failureReason ?? "Clear failed.");
				return;
			}

			setSuccess(
				"Database cleared. The application will restart to recreate the schema.",
			);

			if (result.state === "RestartPending") {
				await pollUntilRestart(result.operationId);
			}
		} catch (err) {
			setError(err instanceof Error ? err.message : "Clear failed.");
		} finally {
			setOperationLoading(null);
			await fetchStatus();
		}
	};

	const pollUntilRestart = async (operationId: string) => {
		const maxAttempts = 60;
		const delay = 2000;

		for (let i = 0; i < maxAttempts; i++) {
			try {
				await new Promise((resolve) => setTimeout(resolve, delay));
				const opStatus =
					await databaseManagementService.getOperationStatus(operationId);

				if (
					opStatus?.state === "RestartComplete" ||
					opStatus?.state === "Completed"
				) {
					if (globalThis.window !== undefined) {
						globalThis.location.reload();
					}
					return;
				}
			} catch {
				// Backend may be restarting
			}
		}
	};

	const downloadBlob = (blob: Blob, filename: string) => {
		const url = URL.createObjectURL(blob);
		const a = document.createElement("a");
		a.href = url;
		a.download = filename;
		document.body.appendChild(a);
		a.click();
		document.body.removeChild(a);
		URL.revokeObjectURL(url);
	};

	const renderActiveOperation = (op: IDatabaseOperationStatus) => (
		<Alert severity="info" sx={{ mb: 2 }}>
			<Typography variant="body2">
				<strong>{op.operationType}</strong> operation is{" "}
				<Chip label={op.state} size="small" color="primary" sx={{ mx: 0.5 }} />
			</Typography>
		</Alert>
	);

	return (
		<Box sx={{ maxWidth: 900, mx: "auto" }}>
			<Typography
				variant="h5"
				component="h2"
				sx={{
					fontWeight: 600,
					color: theme.palette.primary.main,
					mb: 3,
					textAlign: isMobile ? "center" : "left",
				}}
			>
				Database Management
			</Typography>

			{/* Provider Info */}
			{status && (
				<Box sx={{ mb: 2 }}>
					<Chip
						icon={<StorageIcon />}
						label={`Provider: ${status.provider}`}
						variant="outlined"
						size="small"
					/>
				</Box>
			)}

			{/* Blocked Alert */}
			{isBlocked && status?.blockedReason && (
				<Alert severity="warning" sx={{ mb: 2 }}>
					{status.blockedReason}
				</Alert>
			)}

			{/* Tooling Missing Alert */}
			{toolingMissing && (
				<Alert severity="error" icon={<WarningAmberIcon />} sx={{ mb: 2 }}>
					<Typography variant="body2" sx={{ fontWeight: "medium" }}>
						{status.toolingGuidanceMessage}
					</Typography>
					{status.toolingGuidanceUrl && (
						<Typography variant="body2" sx={{ mt: 0.5 }}>
							Tools must be available on the Lighthouse server/host.{" "}
							<Link
								href={status.toolingGuidanceUrl}
								target="_blank"
								rel="noopener noreferrer"
							>
								Download here
							</Link>
						</Typography>
					)}
				</Alert>
			)}

			{/* Active Operation */}
			{status?.activeOperation && renderActiveOperation(status.activeOperation)}

			{/* Error / Success */}
			{error && (
				<Alert severity="error" sx={{ mb: 2 }} onClose={() => setError(null)}>
					{error}
				</Alert>
			)}
			{success && (
				<Alert
					severity="success"
					sx={{ mb: 2 }}
					onClose={() => setSuccess(null)}
				>
					{success}
				</Alert>
			)}

			<Stack spacing={3}>
				{/* Backup Section */}
				<Paper variant="outlined" sx={{ p: 3 }}>
					<Typography
						variant="h6"
						sx={{ mb: 2, display: "flex", alignItems: "center", gap: 1 }}
					>
						<BackupIcon color="primary" /> Backup
					</Typography>
					<Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
						Create an encrypted backup of the database. You will be prompted for
						a password to protect the backup file.
					</Typography>
					<Stack
						direction={isMobile ? "column" : "row"}
						spacing={2}
						alignItems="flex-start"
					>
						<TextField
							label="Backup Password"
							type="password"
							size="small"
							value={backupPassword}
							onChange={(e) => setBackupPassword(e.target.value)}
							inputProps={{ "data-testid": "backup-password" }}
							disabled={isBlocked || toolingMissing}
							sx={{ minWidth: 250 }}
						/>
						<LoadingButton
							variant="contained"
							startIcon={<BackupIcon />}
							loading={operationLoading === "backup"}
							disabled={isBlocked || toolingMissing || !backupPassword}
							onClick={handleBackup}
							data-testid="backup-button"
						>
							Create Backup
						</LoadingButton>
					</Stack>
				</Paper>

				{/* Restore Section */}
				<Paper variant="outlined" sx={{ p: 3 }}>
					<Typography
						variant="h6"
						sx={{ mb: 2, display: "flex", alignItems: "center", gap: 1 }}
					>
						<RestoreIcon color="primary" /> Restore
					</Typography>
					<Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
						Restore the database from a previously created backup file. The
						application will restart after restore.
					</Typography>
					<Stack spacing={2}>
						<Button
							variant="outlined"
							component="label"
							disabled={isBlocked || toolingMissing}
						>
							{restoreFile ? restoreFile.name : "Choose Backup File"}
							<input
								type="file"
								hidden
								accept=".zip"
								ref={fileInputRef}
								onChange={(e) => setRestoreFile(e.target.files?.[0] ?? null)}
								data-testid="restore-file-input"
							/>
						</Button>
						<Stack
							direction={isMobile ? "column" : "row"}
							spacing={2}
							alignItems="flex-start"
						>
							<TextField
								label="Backup Password"
								type="password"
								size="small"
								value={restorePassword}
								onChange={(e) => setRestorePassword(e.target.value)}
								inputProps={{ "data-testid": "restore-password" }}
								disabled={isBlocked || toolingMissing}
								sx={{ minWidth: 250 }}
							/>
							<LoadingButton
								variant="contained"
								color="warning"
								startIcon={<RestoreIcon />}
								loading={operationLoading === "restore"}
								disabled={
									isBlocked ||
									toolingMissing ||
									!restoreFile ||
									!restorePassword
								}
								onClick={handleRestore}
								data-testid="restore-button"
							>
								Restore Backup
							</LoadingButton>
						</Stack>
					</Stack>
				</Paper>

				{/* Clear Section */}
				<Paper variant="outlined" sx={{ p: 3 }}>
					<Typography
						variant="h6"
						sx={{ mb: 2, display: "flex", alignItems: "center", gap: 1 }}
					>
						<DeleteForeverIcon color="error" /> Clear Database
					</Typography>
					<Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
						Remove all data and reset the database. This is a destructive
						operation and cannot be undone. The application will restart after
						clearing.
					</Typography>
					<LoadingButton
						variant="contained"
						color="error"
						startIcon={<DeleteForeverIcon />}
						loading={operationLoading === "clear"}
						disabled={isBlocked || toolingMissing}
						onClick={() => setClearDialogOpen(true)}
						data-testid="clear-button"
					>
						Clear Database
					</LoadingButton>
				</Paper>
			</Stack>

			{/* Clear Confirmation Dialog */}
			<Dialog
				open={clearDialogOpen}
				onClose={() => setClearDialogOpen(false)}
				aria-labelledby={dialogTitleId}
				aria-describedby={dialogDescriptionId}
			>
				<DialogTitle id={dialogTitleId}>
					Are you sure you want to clear the database?
				</DialogTitle>
				<DialogContent>
					<Typography id={dialogDescriptionId} variant="body2">
						This will permanently delete all data including teams, portfolios,
						forecasts, and configuration. The application will restart to
						recreate an empty database. This action cannot be undone.
					</Typography>
				</DialogContent>
				<DialogActions>
					<Button
						onClick={() => setClearDialogOpen(false)}
						data-testid="cancel-clear-button"
					>
						Cancel
					</Button>
					<Button
						onClick={handleClear}
						color="error"
						variant="contained"
						data-testid="confirm-clear-button"
					>
						Clear Everything
					</Button>
				</DialogActions>
			</Dialog>
		</Box>
	);
};

export default DatabaseManagementSettings;
