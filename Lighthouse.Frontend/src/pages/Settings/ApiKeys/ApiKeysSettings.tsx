import AddIcon from "@mui/icons-material/Add";
import ContentCopyIcon from "@mui/icons-material/ContentCopy";
import DeleteIcon from "@mui/icons-material/Delete";
import Alert from "@mui/material/Alert";
import Box from "@mui/material/Box";
import Button from "@mui/material/Button";
import Dialog from "@mui/material/Dialog";
import DialogActions from "@mui/material/DialogActions";
import DialogContent from "@mui/material/DialogContent";
import DialogTitle from "@mui/material/DialogTitle";
import IconButton from "@mui/material/IconButton";
import Table from "@mui/material/Table";
import TableBody from "@mui/material/TableBody";
import TableCell from "@mui/material/TableCell";
import TableContainer from "@mui/material/TableContainer";
import TableHead from "@mui/material/TableHead";
import TableRow from "@mui/material/TableRow";
import TextField from "@mui/material/TextField";
import Tooltip from "@mui/material/Tooltip";
import Typography from "@mui/material/Typography";
import type React from "react";
import { useCallback, useContext, useEffect, useId, useState } from "react";
import type { IApiKeyInfo } from "../../../models/ApiKey/ApiKey";
import { AuthMode } from "../../../models/Auth/AuthModels";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";

interface CreateApiKeyDialogProps {
	open: boolean;
	onClose: () => void;
	onCreated: () => void;
}

const CreateApiKeyDialog: React.FC<CreateApiKeyDialogProps> = ({
	open,
	onClose,
	onCreated,
}) => {
	const [name, setName] = useState("");
	const [description, setDescription] = useState("");
	const [createdKey, setCreatedKey] = useState<string | null>(null);
	const [copied, setCopied] = useState(false);
	const [loading, setLoading] = useState(false);
	const [error, setError] = useState<string | null>(null);

	const { apiKeyService } = useContext(ApiServiceContext);
	const titleId = useId();

	const handleCreate = async () => {
		if (!name.trim()) return;

		setLoading(true);
		setError(null);

		try {
			const result = await apiKeyService.createApiKey({
				name: name.trim(),
				description: description.trim() || undefined,
			});
			setCreatedKey(result.plainTextKey);
			onCreated(); // refreshes the key list in the parent; does NOT close the dialog
		} catch {
			setError("Failed to create API key. Please try again.");
		} finally {
			setLoading(false);
		}
	};

	const handleCopy = async () => {
		if (!createdKey) return;
		await navigator.clipboard.writeText(createdKey);
		setCopied(true);
		setTimeout(() => setCopied(false), 2000);
	};

	const handleClose = () => {
		setName("");
		setDescription("");
		setCreatedKey(null);
		setCopied(false);
		setError(null);
		onClose();
	};

	return (
		<Dialog
			open={open}
			onClose={handleClose}
			aria-labelledby={titleId}
			fullWidth
			maxWidth="sm"
		>
			<DialogTitle id={titleId}>
				{createdKey ? "API Key Created" : "Create New API Key"}
			</DialogTitle>
			<DialogContent>
				{createdKey ? (
					<Box>
						<Alert severity="warning" sx={{ mb: 2 }}>
							This is the only time the key will be shown. Copy it now — it
							cannot be retrieved later.
						</Alert>
						<Box
							sx={{
								display: "flex",
								alignItems: "center",
								gap: 1,
								p: 1,
								bgcolor: "action.hover",
								borderRadius: 1,
								fontFamily: "monospace",
								wordBreak: "break-all",
							}}
						>
							<Typography
								variant="body2"
								sx={{ fontFamily: "monospace", flex: 1 }}
								data-testid="created-api-key-value"
							>
								{createdKey}
							</Typography>
							<Tooltip title={copied ? "Copied!" : "Copy to clipboard"}>
								<IconButton
									onClick={handleCopy}
									size="small"
									data-testid="copy-api-key-button"
									aria-label="Copy API key"
								>
									<ContentCopyIcon fontSize="small" />
								</IconButton>
							</Tooltip>
						</Box>
					</Box>
				) : (
					<Box sx={{ display: "flex", flexDirection: "column", gap: 2, pt: 1 }}>
						{error && <Alert severity="error">{error}</Alert>}
						<TextField
							label="Name"
							value={name}
							onChange={(e) => setName(e.target.value)}
							required
							fullWidth
							slotProps={{ htmlInput: { "data-testid": "api-key-name-input" } }}
						/>
						<TextField
							label="Description (optional)"
							value={description}
							onChange={(e) => setDescription(e.target.value)}
							fullWidth
							multiline
							rows={2}
							slotProps={{
								htmlInput: { "data-testid": "api-key-description-input" },
							}}
						/>
					</Box>
				)}
			</DialogContent>
			<DialogActions>
				{createdKey ? (
					<Button onClick={handleClose} data-testid="api-key-done-button">
						Done
					</Button>
				) : (
					<>
						<Button onClick={handleClose}>Cancel</Button>
						<Button
							onClick={handleCreate}
							variant="contained"
							disabled={!name.trim() || loading}
							data-testid="create-api-key-submit-button"
						>
							{loading ? "Creating..." : "Create"}
						</Button>
					</>
				)}
			</DialogActions>
		</Dialog>
	);
};

const ApiKeysSettings: React.FC = () => {
	const [keys, setKeys] = useState<IApiKeyInfo[]>([]);
	const [createDialogOpen, setCreateDialogOpen] = useState(false);
	const [error, setError] = useState<string | null>(null);
	const [authEnabled, setAuthEnabled] = useState<boolean | null>(null);

	const { apiKeyService, authService } = useContext(ApiServiceContext);

	const fetchKeys = useCallback(async () => {
		try {
			const data = await apiKeyService.getApiKeys();
			setKeys(data);
		} catch {
			setError("Failed to load API keys.");
		}
	}, [apiKeyService]);

	useEffect(() => {
		const initialize = async () => {
			try {
				const runtimeAuthStatus = await authService.getRuntimeAuthStatus();
				const isAuthEnabled = runtimeAuthStatus.mode === AuthMode.Enabled;
				setAuthEnabled(isAuthEnabled);

				if (!isAuthEnabled) {
					setKeys([]);
					return;
				}

				await fetchKeys();
			} catch {
				setError("Failed to load API keys.");
			}
		};

		void initialize();
	}, [authService, fetchKeys]);

	const handleDelete = async (id: number) => {
		try {
			await apiKeyService.deleteApiKey(id);
			setKeys((prev) => prev.filter((k) => k.id !== id));
		} catch {
			setError("Failed to delete API key.");
		}
	};

	let keysContent: React.ReactNode;
	if (authEnabled === false) {
		keysContent = (
			<Typography variant="body2" color="text.secondary">
				Enable authentication to manage API keys.
			</Typography>
		);
	} else if (keys.length === 0) {
		keysContent = (
			<Typography
				variant="body2"
				color="text.secondary"
				data-testid="no-api-keys-message"
			>
				No API keys configured. Create one to allow CLI and MCP clients to
				authenticate.
			</Typography>
		);
	} else {
		keysContent = (
			<TableContainer>
				<Table data-testid="api-keys-table" size="small">
					<TableHead>
						<TableRow>
							<TableCell>Name</TableCell>
							<TableCell>Description</TableCell>
							<TableCell>Created By</TableCell>
							<TableCell>Created At</TableCell>
							<TableCell>Last Used</TableCell>
							<TableCell align="right">Actions</TableCell>
						</TableRow>
					</TableHead>
					<TableBody>
						{keys.map((key) => (
							<TableRow key={key.id} data-testid={`api-key-row-${key.id}`}>
								<TableCell>{key.name}</TableCell>
								<TableCell>{key.description}</TableCell>
								<TableCell>{key.createdByUser}</TableCell>
								<TableCell>
									{new Date(key.createdAt).toLocaleDateString()}
								</TableCell>
								<TableCell>
									{key.lastUsedAt
										? new Date(key.lastUsedAt).toLocaleDateString()
										: "Never"}
								</TableCell>
								<TableCell align="right">
									<Tooltip title="Delete API key">
										<IconButton
											onClick={() => handleDelete(key.id)}
											size="small"
											color="error"
											data-testid={`delete-api-key-${key.id}`}
											aria-label={`Delete API key ${key.name}`}
										>
											<DeleteIcon fontSize="small" />
										</IconButton>
									</Tooltip>
								</TableCell>
							</TableRow>
						))}
					</TableBody>
				</Table>
			</TableContainer>
		);
	}

	return (
		<Box>
			{authEnabled === false && (
				<Alert
					severity="info"
					data-testid="api-keys-disabled-message"
					sx={{ mb: 2 }}
				>
					Authentication is not enabled. API keys are unavailable until auth is
					enabled.
				</Alert>
			)}
			{error && (
				<Alert severity="error" sx={{ mb: 2 }}>
					{error}
				</Alert>
			)}
			<Box sx={{ display: "flex", justifyContent: "flex-end", mb: 2 }}>
				<Button
					variant="contained"
					startIcon={<AddIcon />}
					onClick={() => setCreateDialogOpen(true)}
					data-testid="create-api-key-button"
					disabled={authEnabled !== true}
				>
					New API Key
				</Button>
			</Box>

			{keysContent}

			<CreateApiKeyDialog
				open={createDialogOpen}
				onClose={() => setCreateDialogOpen(false)}
				onCreated={() => {
					// Only refresh the list; the dialog stays open to show the plaintext key.
					// It closes only when the user clicks Done (which calls onClose).
					fetchKeys();
				}}
			/>
		</Box>
	);
};

export default ApiKeysSettings;
