import Alert from "@mui/material/Alert";
import Box from "@mui/material/Box";
import Button from "@mui/material/Button";
import CircularProgress from "@mui/material/CircularProgress";
import Paper from "@mui/material/Paper";
import Stack from "@mui/material/Stack";
import Table from "@mui/material/Table";
import TableBody from "@mui/material/TableBody";
import TableCell from "@mui/material/TableCell";
import TableHead from "@mui/material/TableHead";
import TableRow from "@mui/material/TableRow";
import Typography from "@mui/material/Typography";
import type React from "react";
import type {
	RbacScopedMemberSummary,
	ScopedRbacRole,
} from "../../../models/Authorization/RbacModels";

interface ScopedMembershipManagerProps {
	title: string;
	members: RbacScopedMemberSummary[];
	allowedRoles: ScopedRbacRole[];
	loading: boolean;
	error?: string;
	onAssignRole: (userProfileId: number, role: ScopedRbacRole) => Promise<void>;
	onRemoveRole: (userProfileId: number) => Promise<void>;
}

const roleLabels: Record<ScopedRbacRole, string> = {
	Viewer: "Viewer",
	TeamAdmin: "Team Admin",
	PortfolioAdmin: "Portfolio Admin",
};

const getRoleLabel = (role: ScopedRbacRole | null | undefined): string => {
	if (!role) {
		return "No access";
	}

	return roleLabels[role] ?? role;
};

const getMemberName = (member: RbacScopedMemberSummary): string => {
	if (member.displayName && member.displayName.trim().length > 0) {
		return member.displayName;
	}

	return "(Unnamed user)";
};

const ScopedMembershipManager: React.FC<ScopedMembershipManagerProps> = ({
	title,
	members,
	allowedRoles,
	loading,
	error,
	onAssignRole,
	onRemoveRole,
}) => {
	return (
		<Paper variant="outlined" sx={{ p: 2 }}>
			<Stack spacing={2}>
				<Typography variant="h6" data-testid="scoped-members-title">
					{title}
				</Typography>

				{error && <Alert severity="error">{error}</Alert>}

				{loading ? (
					<Box
						sx={{
							display: "flex",
							alignItems: "center",
							justifyContent: "center",
							py: 3,
						}}
						data-testid="scoped-members-loading"
					>
						<CircularProgress size={24} />
					</Box>
				) : (
					<Table size="small" data-testid="scoped-members-table">
						<TableHead>
							<TableRow>
								<TableCell>Name</TableCell>
								<TableCell>Email</TableCell>
								<TableCell>Current Role</TableCell>
								<TableCell align="right">Actions</TableCell>
							</TableRow>
						</TableHead>
						<TableBody>
							{members.map((member) => (
								<TableRow
									key={member.userProfileId}
									data-testid={`scoped-member-row-${member.userProfileId}`}
								>
									<TableCell>{getMemberName(member)}</TableCell>
									<TableCell>{member.email || "-"}</TableCell>
									<TableCell>{getRoleLabel(member.role)}</TableCell>
									<TableCell align="right">
										<Stack
											direction="row"
											spacing={1}
											sx={{ justifyContent: "flex-end" }}
										>
											{allowedRoles.map((role) => (
												<Button
													key={role}
													size="small"
													variant={
														member.role === role ? "contained" : "outlined"
													}
													onClick={() =>
														onAssignRole(member.userProfileId, role)
													}
													data-testid={`assign-${role}-${member.userProfileId}`}
												>
													{getRoleLabel(role)}
												</Button>
											))}
											<Button
												size="small"
												color="error"
												onClick={() => onRemoveRole(member.userProfileId)}
												disabled={!member.role}
												data-testid={`remove-member-${member.userProfileId}`}
											>
												Remove
											</Button>
										</Stack>
									</TableCell>
								</TableRow>
							))}
						</TableBody>
					</Table>
				)}
			</Stack>
		</Paper>
	);
};

export default ScopedMembershipManager;
