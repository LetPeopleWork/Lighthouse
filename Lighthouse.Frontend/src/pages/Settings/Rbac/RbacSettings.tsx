import Alert from "@mui/material/Alert";
import Box from "@mui/material/Box";
import Button from "@mui/material/Button";
import Checkbox from "@mui/material/Checkbox";
import Chip from "@mui/material/Chip";
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
	const { rbacService } = useContext(ApiServiceContext);
	const [status, setStatus] = useState<RbacStatus>();
	const [users, setUsers] = useState<RbacUser[]>([]);
	const [groupMappings, setGroupMappings] = useState<RbacGroupMapping[]>([]);
	const [error, setError] = useState<string>();
	const [showUnassignedOnly, setShowUnassignedOnly] = useState(false);
	const [groupValueInput, setGroupValueInput] = useState("");

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

	const visibleUsers = showUnassignedOnly
		? users.filter((user) => user.isUnassigned)
		: users;

	return (
		<Box>
			{error && <Alert severity="error">{error}</Alert>}

			<Box sx={{ display: "flex", gap: 2, mb: 2, flexWrap: "wrap" }}>
				<Chip
					label={`RBAC: ${status?.enabled ? "Enabled" : "Disabled"}`}
					data-testid="rbac-status-enabled"
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
				User Access
			</Typography>

			<FormControlLabel
				label="Show unassigned users only"
				control={
					<Checkbox
						checked={showUnassignedOnly}
						onChange={(_event, checked) => setShowUnassignedOnly(checked)}
					/>
				}
				sx={{ mb: 1 }}
			/>

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
							<TableCell>{user.isSystemAdmin ? "Yes" : "No"}</TableCell>
							<TableCell>{user.isUnassigned ? "Yes" : "No"}</TableCell>
							<TableCell align="right">
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
		</Box>
	);
};

export default RbacSettings;
