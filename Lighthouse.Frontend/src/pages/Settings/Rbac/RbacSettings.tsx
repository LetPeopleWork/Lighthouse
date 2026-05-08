import Alert from "@mui/material/Alert";
import Box from "@mui/material/Box";
import Button from "@mui/material/Button";
import Chip from "@mui/material/Chip";
import Table from "@mui/material/Table";
import TableBody from "@mui/material/TableBody";
import TableCell from "@mui/material/TableCell";
import TableHead from "@mui/material/TableHead";
import TableRow from "@mui/material/TableRow";
import Typography from "@mui/material/Typography";
import type React from "react";
import { useCallback, useContext, useEffect, useState } from "react";
import type {
	RbacStatus,
	RbacUser,
} from "../../../models/Authorization/RbacModels";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";

const RbacSettings: React.FC = () => {
	const { rbacService } = useContext(ApiServiceContext);
	const [status, setStatus] = useState<RbacStatus>();
	const [users, setUsers] = useState<RbacUser[]>([]);
	const [error, setError] = useState<string>();

	const load = useCallback(async () => {
		setError(undefined);
		try {
			const nextStatus = await rbacService.getStatus();
			setStatus(nextStatus);

			if (nextStatus.hasSystemAdmin) {
				const nextUsers = await rbacService.getUsers();
				setUsers(nextUsers);
			} else {
				setUsers([]);
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

			<Table size="small" data-testid="rbac-users-table">
				<TableHead>
					<TableRow>
						<TableCell>Display Name</TableCell>
						<TableCell>Email</TableCell>
						<TableCell>Subject</TableCell>
						<TableCell>System Admin</TableCell>
						<TableCell align="right">Actions</TableCell>
					</TableRow>
				</TableHead>
				<TableBody>
					{users.map((user) => (
						<TableRow key={user.id} data-testid={`rbac-user-row-${user.id}`}>
							<TableCell>{user.displayName || "(Unnamed user)"}</TableCell>
							<TableCell>{user.email || "-"}</TableCell>
							<TableCell>{user.subject}</TableCell>
							<TableCell>{user.isSystemAdmin ? "Yes" : "No"}</TableCell>
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
		</Box>
	);
};

export default RbacSettings;
