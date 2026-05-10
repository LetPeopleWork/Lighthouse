import ExpandMoreIcon from "@mui/icons-material/ExpandMore";
import LockIcon from "@mui/icons-material/Lock";
import Accordion from "@mui/material/Accordion";
import AccordionDetails from "@mui/material/AccordionDetails";
import AccordionSummary from "@mui/material/AccordionSummary";
import Alert from "@mui/material/Alert";
import Box from "@mui/material/Box";
import Button from "@mui/material/Button";
import Checkbox from "@mui/material/Checkbox";
import Chip from "@mui/material/Chip";
import Dialog from "@mui/material/Dialog";
import DialogActions from "@mui/material/DialogActions";
import DialogContent from "@mui/material/DialogContent";
import DialogContentText from "@mui/material/DialogContentText";
import DialogTitle from "@mui/material/DialogTitle";
import FormControlLabel from "@mui/material/FormControlLabel";
import Table from "@mui/material/Table";
import TableBody from "@mui/material/TableBody";
import TableCell from "@mui/material/TableCell";
import TableHead from "@mui/material/TableHead";
import TableRow from "@mui/material/TableRow";
import TextField from "@mui/material/TextField";
import Typography from "@mui/material/Typography";
import type React from "react";
import { useCallback, useContext, useEffect, useState } from "react";
import type {
	RbacGroupMapping,
	RbacStatus,
	RbacUser,
} from "../../../models/Authorization/RbacModels";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";

const RbacSettings: React.FC = () => {
	const { rbacService, authService } = useContext(ApiServiceContext);
	const [status, setStatus] = useState<RbacStatus>();
	const [users, setUsers] = useState<RbacUser[]>([]);
	const [groupMappings, setGroupMappings] = useState<RbacGroupMapping[]>([]);
	const [error, setError] = useState<string>();
	const [showUnassignedOnly, setShowUnassignedOnly] = useState(false);
	const [groupValueInput, setGroupValueInput] = useState("");
	const [userSearchText, setUserSearchText] = useState("");
	const [removeDialogUserId, setRemoveDialogUserId] = useState<number | null>(
		null,
	);
	const [currentUserSubject, setCurrentUserSubject] = useState<string>();

	const load = useCallback(async () => {
		setError(undefined);
		try {
			const nextStatus = await rbacService.getStatus();
			setStatus(nextStatus);

			if (nextStatus.hasSystemAdmin) {
				const [nextUsers, nextGroupMappings] = await Promise.all([
					rbacService.getUsers(),
					rbacService.getGroupMappings(),
				]);
				setUsers(nextUsers);
				setGroupMappings(
					nextGroupMappings.filter(
						(mapping) =>
							mapping.scopeType === "System" && mapping.role === "SystemAdmin",
					),
				);
			} else {
				setUsers([]);
				setGroupMappings([]);
			}
		} catch {
			setError("Failed to load RBAC configuration.");
		}
	}, [rbacService]);

	useEffect(() => {
		void load();
	}, [load]);

	useEffect(() => {
		let cancelled = false;
		authService
			.getCurrentUserProfile()
			.then((profile) => {
				if (!cancelled) {
					setCurrentUserSubject(profile.subject);
				}
			})
			.catch(() => {
				if (!cancelled) {
					setCurrentUserSubject(undefined);
				}
			});
		return () => {
			cancelled = true;
		};
	}, [authService]);

	const handleBootstrap = async () => {
		try {
			await rbacService.bootstrapCurrentUserAsSystemAdmin();
			await load();
		} catch {
			setError("Failed to bootstrap System Admin.");
		}
	};

	const handleGrantSystemAdmin = async (userId: number) => {
		try {
			await rbacService.grantSystemAdmin(userId);
			await load();
		} catch {
			setError("Failed to grant System Admin role.");
		}
	};

	const handleRevokeSystemAdmin = async (userId: number) => {
		try {
			await rbacService.revokeSystemAdmin(userId);
			await load();
		} catch {
			setError("Failed to revoke System Admin role.");
		}
	};

	const handleCreateGroupMapping = async () => {
		const normalizedGroupValue = groupValueInput.trim();
		if (!normalizedGroupValue) {
			setError("Group value is required.");
			return;
		}

		try {
			await rbacService.createGroupMapping({
				groupValue: normalizedGroupValue,
				role: "SystemAdmin",
				scopeType: "System",
				scopeId: null,
			});
			setGroupValueInput("");
			await load();
		} catch {
			setError("Failed to create group mapping.");
		}
	};

	const handleRemoveGroupMapping = async (mappingId: number) => {
		try {
			await rbacService.removeGroupMapping(mappingId);
			await load();
		} catch {
			setError("Failed to remove group mapping.");
		}
	};

	const handleConfirmRemoveUser = async () => {
		if (removeDialogUserId === null) return;
		const userId = removeDialogUserId;
		setRemoveDialogUserId(null);
		try {
			await rbacService.deleteUser(userId);
			await load();
		} catch {
			setError("Failed to remove user.");
		}
	};

	const visibleUsers = users
		.filter((user) => !showUnassignedOnly || user.isUnassigned)
		.filter((user) => {
			if (!userSearchText.trim()) return true;
			const search = userSearchText.toLowerCase();
			return (
				(user.displayName ?? "").toLowerCase().includes(search) ||
				(user.email ?? "").toLowerCase().includes(search)
			);
		});

	return (
		<Box>
			{error && <Alert severity="error">{error}</Alert>}

			<Accordion
				defaultExpanded={false}
				data-testid="rbac-status-enabled"
				sx={{ mb: 2 }}
			>
				<AccordionSummary expandIcon={<ExpandMoreIcon />}>
					<Typography>RBAC Status</Typography>
				</AccordionSummary>
				<AccordionDetails>
					<Box sx={{ display: "flex", gap: 2, flexWrap: "wrap" }}>
						<Chip
							label={`RBAC: ${status?.enabled ? "Enabled" : "Disabled"}`}
							data-testid="rbac-status-chip-enabled"
						/>
						<Chip
							label={`Premium Gate: ${status?.premiumGateSatisfied ? "Ready" : "Blocked"}`}
							data-testid="rbac-status-premium-gate"
						/>
						<Chip
							label={`Emergency Admin: ${status?.hasEmergencyAdminConfigured ? "Configured" : "Not configured"}`}
							data-testid="rbac-status-emergency-admin"
						/>
						<Chip
							label={`Ready: ${status?.readyForEnablement ? "Yes" : "No"}`}
							data-testid="rbac-status-ready"
						/>
						<Chip
							label={`Unassigned users: ${status?.unassignedUserCount ?? 0}`}
							data-testid="rbac-status-unassigned-count"
						/>
						<Chip
							label={`Group claim: ${status?.groupClaimName || "Not configured"}`}
							data-testid="rbac-status-group-claim"
						/>
					</Box>
				</AccordionDetails>
			</Accordion>

			{status && !status.hasSystemAdmin && (
				<Box sx={{ mb: 2 }}>
					<Alert severity="info" sx={{ mb: 1 }}>
						No System Admin is assigned yet.
					</Alert>
					<Button
						variant="contained"
						onClick={handleBootstrap}
						data-testid="rbac-bootstrap-button"
					>
						Become First System Admin
					</Button>
				</Box>
			)}

			<Typography variant="h6" sx={{ mb: 1 }}>
				System Admins
			</Typography>

			<Box
				sx={{
					display: "flex",
					gap: 2,
					mb: 1,
					flexWrap: "wrap",
					alignItems: "center",
				}}
			>
				<TextField
					label="Search by name or email"
					size="small"
					value={userSearchText}
					onChange={(event) => setUserSearchText(event.target.value)}
					data-testid="rbac-user-search"
					sx={{ minWidth: 220 }}
				/>
				<FormControlLabel
					label="Show unassigned users only"
					control={
						<Checkbox
							checked={showUnassignedOnly}
							onChange={(_event, checked) => setShowUnassignedOnly(checked)}
						/>
					}
				/>
			</Box>

			<Table size="small" data-testid="rbac-users-table">
				<TableHead>
					<TableRow>
						<TableCell>Display Name</TableCell>
						<TableCell>Email</TableCell>
						<TableCell>Subject</TableCell>
						<TableCell>System Admin</TableCell>
						<TableCell>Unassigned</TableCell>
						<TableCell align="right">Actions</TableCell>
					</TableRow>
				</TableHead>
				<TableBody>
					{visibleUsers.map((user) => (
						<TableRow key={user.id} data-testid={`rbac-user-row-${user.id}`}>
							<TableCell>{user.displayName || "(Unnamed user)"}</TableCell>
							<TableCell>{user.email || "-"}</TableCell>
							<TableCell>{user.subject}</TableCell>
							<TableCell>
								const adminLabel = user.isEmergencyAdmin ? (
								<Box sx={{ display: "flex", alignItems: "center", gap: 0.5 }}>
									<Typography variant="body2">Emergency Admin</Typography>
									<LockIcon fontSize="small" />
								</Box>
								) : user.isSystemAdmin ? ( "Yes" ) : ( "No" );
							</TableCell>
							<TableCell>{user.isUnassigned ? "Yes" : "No"}</TableCell>
							<TableCell align="right">
								<Box
									sx={{ display: "flex", gap: 1, justifyContent: "flex-end" }}
								>
									{!user.isEmergencyAdmin && (
										<>
											{user.isSystemAdmin ? (
												<Button
													size="small"
													color="error"
													onClick={() => handleRevokeSystemAdmin(user.id)}
													data-testid={`rbac-revoke-system-admin-${user.id}`}
												>
													Revoke
												</Button>
											) : (
												<Button
													size="small"
													onClick={() => handleGrantSystemAdmin(user.id)}
													data-testid={`rbac-grant-system-admin-${user.id}`}
												>
													Grant
												</Button>
											)}
											{user.subject !== currentUserSubject && (
												<Button
													size="small"
													color="error"
													onClick={() => setRemoveDialogUserId(user.id)}
													data-testid={`rbac-remove-user-${user.id}`}
												>
													Remove
												</Button>
											)}
										</>
									)}
								</Box>
							</TableCell>
						</TableRow>
					))}
				</TableBody>
			</Table>

			{status?.hasSystemAdmin && (
				<Box sx={{ mt: 4 }}>
					<Typography variant="h6" sx={{ mb: 1 }}>
						System Admin SSO Groups
					</Typography>

					<Box sx={{ display: "flex", gap: 1, mb: 2, flexWrap: "wrap" }}>
						<TextField
							label="Group value"
							size="small"
							value={groupValueInput}
							onChange={(event) => setGroupValueInput(event.target.value)}
							data-testid="rbac-group-mapping-group-value"
						/>
						<Button
							variant="contained"
							onClick={handleCreateGroupMapping}
							data-testid="rbac-create-group-mapping"
						>
							Add Mapping
						</Button>
					</Box>

					<Table size="small" data-testid="rbac-group-mappings-table">
						<TableHead>
							<TableRow>
								<TableCell>Group</TableCell>
								<TableCell>Role</TableCell>
								<TableCell>Scope</TableCell>
								<TableCell>Scope ID</TableCell>
								<TableCell align="right">Actions</TableCell>
							</TableRow>
						</TableHead>
						<TableBody>
							{groupMappings.map((mapping) => (
								<TableRow key={mapping.id}>
									<TableCell>{mapping.groupValue}</TableCell>
									<TableCell>{mapping.role}</TableCell>
									<TableCell>{mapping.scopeType}</TableCell>
									<TableCell>{mapping.scopeId ?? "-"}</TableCell>
									<TableCell align="right">
										<Button
											size="small"
											color="error"
											onClick={() => handleRemoveGroupMapping(mapping.id)}
											data-testid={`rbac-remove-group-mapping-${mapping.id}`}
										>
											Remove
										</Button>
									</TableCell>
								</TableRow>
							))}
						</TableBody>
					</Table>
				</Box>
			)}

			<Dialog
				open={removeDialogUserId !== null}
				onClose={() => setRemoveDialogUserId(null)}
			>
				<DialogTitle>Remove User</DialogTitle>
				<DialogContent>
					<DialogContentText>
						Are you sure you want to remove this user from the system?
					</DialogContentText>
				</DialogContent>
				<DialogActions>
					<Button onClick={() => setRemoveDialogUserId(null)}>Cancel</Button>
					<Button color="error" onClick={handleConfirmRemoveUser}>
						Confirm
					</Button>
				</DialogActions>
			</Dialog>
		</Box>
	);
};

export default RbacSettings;
