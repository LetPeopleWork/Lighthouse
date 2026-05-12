import AddIcon from "@mui/icons-material/Add";
import ContentCopyIcon from "@mui/icons-material/ContentCopy";
import DeleteIcon from "@mui/icons-material/Delete";
import ExpandMoreIcon from "@mui/icons-material/ExpandMore";
import Accordion from "@mui/material/Accordion";
import AccordionDetails from "@mui/material/AccordionDetails";
import AccordionSummary from "@mui/material/AccordionSummary";
import Alert from "@mui/material/Alert";
import Box from "@mui/material/Box";
import Button from "@mui/material/Button";
import CircularProgress from "@mui/material/CircularProgress";
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
import type {
	IApiKeyInfo,
	IApiKeyScope,
	ICreateApiKeyRequest,
} from "../../../models/ApiKey/ApiKey";
import { AuthMode } from "../../../models/Auth/AuthModels";
import { ApiError } from "../../../services/Api/ApiError";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import ScopeRowList, {
	type IPortfolioOption,
	type IScopeRow,
	type ITeamOption,
} from "./ScopeRowList";

interface CreateApiKeyDialogProps {
	open: boolean;
	onClose: () => void;
	onCreated: () => void;
}

const isScopeRowComplete = (row: IScopeRow): boolean =>
	row.role !== undefined &&
	row.scopeType !== undefined &&
	row.scopeId !== undefined &&
	row.scopeId !== null;

const toApiKeyScopes = (rows: IScopeRow[]): IApiKeyScope[] =>
	rows.filter(isScopeRowComplete).map((row) => ({
		role: row.role as IApiKeyScope["role"],
		scopeType: row.scopeType as IApiKeyScope["scopeType"],
		scopeId: row.scopeId as number,
	}));

const resolveCreateErrorMessage = (error: unknown): string => {
	if (error instanceof ApiError && error.message) {
		return error.message;
	}
	return "Failed to create API key. Please try again.";
};

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
	const [scopeExpanded, setScopeExpanded] = useState(false);
	const [scopeRows, setScopeRows] = useState<IScopeRow[]>([]);
	const [availableTeams, setAvailableTeams] = useState<ITeamOption[] | null>(
		null,
	);
	const [availablePortfolios, setAvailablePortfolios] = useState<
		IPortfolioOption[] | null
	>(null);
	const [scopeDataLoaded, setScopeDataLoaded] = useState(false);

	const { apiKeyService, teamService, portfolioService } =
		useContext(ApiServiceContext);
	const titleId = useId();

	useEffect(() => {
		if (!open || !scopeExpanded || scopeDataLoaded) return;

		let cancelled = false;
		const loadScopeOptions = async () => {
			try {
				const [teams, portfolios] = await Promise.all([
					teamService.getTeams(),
					portfolioService.getPortfolios(),
				]);
				if (cancelled) return;
				setAvailableTeams(
					teams.map((team) => ({ id: team.id, name: team.name })),
				);
				setAvailablePortfolios(
					portfolios.map((portfolio) => ({
						id: portfolio.id,
						name: portfolio.name,
					})),
				);
				setScopeDataLoaded(true);
			} catch {
				if (cancelled) return;
				setAvailableTeams([]);
				setAvailablePortfolios([]);
				setScopeDataLoaded(true);
			}
		};

		void loadScopeOptions();
		return () => {
			cancelled = true;
		};
	}, [open, scopeExpanded, scopeDataLoaded, teamService, portfolioService]);

	const hasIncompleteScopeRow = scopeRows.some(
		(row) => !isScopeRowComplete(row),
	);
	const submitDisabled = !name.trim() || loading || hasIncompleteScopeRow;

	const handleCreate = async () => {
		if (!name.trim()) return;

		setLoading(true);
		setError(null);

		const completedScopes = toApiKeyScopes(scopeRows);
		const request: ICreateApiKeyRequest = {
			name: name.trim(),
			description: description.trim() || undefined,
		};
		if (completedScopes.length > 0) {
			request.scope = completedScopes;
		}

		try {
			const result = await apiKeyService.createApiKey(request);
			setCreatedKey(result.plainTextKey);
			onCreated(); // refreshes the key list in the parent; does NOT close the dialog
		} catch (error_) {
			setError(resolveCreateErrorMessage(error_));
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
		setScopeExpanded(false);
		setScopeRows([]);
		setAvailableTeams(null);
		setAvailablePortfolios(null);
		setScopeDataLoaded(false);
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
						<Accordion
							defaultExpanded={false}
							expanded={scopeExpanded}
							onChange={(_, expanded) => setScopeExpanded(expanded)}
							data-testid="scope-accordion"
						>
							<AccordionSummary
								expandIcon={<ExpandMoreIcon />}
								data-testid="scope-accordion-summary"
							>
								<Typography variant="body2">
									Restrict scope (optional)
								</Typography>
							</AccordionSummary>
							<AccordionDetails>
								{scopeDataLoaded ? (
									<ScopeRowList
										rows={scopeRows}
										onChange={setScopeRows}
										availableTeams={availableTeams ?? []}
										availablePortfolios={availablePortfolios ?? []}
									/>
								) : (
									<Box
										sx={{ display: "flex", justifyContent: "center", p: 2 }}
										data-testid="scope-data-loading"
									>
										<CircularProgress size={24} />
									</Box>
								)}
							</AccordionDetails>
						</Accordion>
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
							disabled={submitDisabled}
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
