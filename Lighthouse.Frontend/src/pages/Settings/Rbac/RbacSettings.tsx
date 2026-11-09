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
import CircularProgress from "@mui/material/CircularProgress";
import Dialog from "@mui/material/Dialog";
import DialogActions from "@mui/material/DialogActions";
import DialogContent from "@mui/material/DialogContent";
import DialogContentText from "@mui/material/DialogContentText";
import DialogTitle from "@mui/material/DialogTitle";
import FormControlLabel from "@mui/material/FormControlLabel";
import MenuItem from "@mui/material/MenuItem";
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
	CreateRbacGroupMappingRequest,
	RbacGroupMapping,
	RbacStatus,
	RbacUser,
} from "../../../models/Authorization/RbacModels";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import { UsageDataEventName } from "../../../services/Api/UsageDataService";
import { useTerminology } from "../../../services/TerminologyContext";
import { useUsageDataReporter } from "../../../services/UsageData/usageDataReporter";
import { fireAndForget } from "../../../utils/fireAndForget";
import EveryoneSignedInSection from "./EveryoneSignedInSection";

type SsoGroupRole = "SystemAdmin" | "ViewerOnAll";

const ssoGroupRoleChoices: { value: SsoGroupRole; label: string }[] = [
	{ value: "SystemAdmin", label: "System Admin" },
	{ value: "ViewerOnAll", label: "Viewer on all" },
];

const mappingFor = (
	role: SsoGroupRole,
	groupValue: string,
): CreateRbacGroupMappingRequest =>
	role === "ViewerOnAll"
		? { groupValue, role: "Viewer", scopeType: "AllUnits", scopeId: null }
		: { groupValue, role: "SystemAdmin", scopeType: "System", scopeId: null };

const isSsoGroupRule = (mapping: RbacGroupMapping): boolean =>
	(mapping.scopeType === "System" && mapping.role === "SystemAdmin") ||
	mapping.scopeType === "AllUnits";

const roleLabelOf = (mapping: RbacGroupMapping): string =>
	mapping.role === "SystemAdmin" ? "System Admin" : mapping.role;

type RulesLoad = "loading" | "failed" | "loaded";

const RbacSettings: React.FC = () => {
	const { rbacService, authService } = useContext(ApiServiceContext);
	const { getTerm } = useTerminology();
	const reportUsage = useUsageDataReporter();
	const [status, setStatus] = useState<RbacStatus>();
	const [users, setUsers] = useState<RbacUser[]>([]);
	const [groupMappings, setGroupMappings] = useState<RbacGroupMapping[]>([]);
	const [rulesLoad, setRulesLoad] = useState<RulesLoad>("loading");
	const [roleChoice, setRoleChoice] = useState<SsoGroupRole>("SystemAdmin");
	const [error, setError] = useState<string>();
	const [showUnassignedOnly, setShowUnassignedOnly] = useState(false);
	const [groupValueInput, setGroupValueInput] = useState("");
	const [userSearchText, setUserSearchText] = useState("");
	const [removeDialogUserId, setRemoveDialogUserId] = useState<number | null>(
		null,
	);
	const [currentUserSubject, setCurrentUserSubject] = useState<string>();

	const loadRules = useCallback(async () => {
		try {
			const nextGroupMappings = await rbacService.getGroupMappings();
			setGroupMappings(nextGroupMappings.filter(isSsoGroupRule));
			setRulesLoad("loaded");
		} catch {
			setRulesLoad("failed");
		}
	}, [rbacService]);

	const load = useCallback(async () => {
		setError(undefined);
		try {
			const nextStatus = await rbacService.getStatus();
			setStatus(nextStatus);

			if (nextStatus.hasSystemAdmin) {
				const [nextUsers] = await Promise.all([
					rbacService.getUsers(),
					loadRules(),
				]);
				setUsers(nextUsers);
			} else {
				setUsers([]);
				setGroupMappings([]);
			}
		} catch {
			setError("Failed to load RBAC configuration.");
		}
	}, [rbacService, loadRules]);

	useEffect(() => {
		fireAndForget(load());
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
			await rbacService.createGroupMapping(
				mappingFor(roleChoice, normalizedGroupValue),
			);
			if (roleChoice === "ViewerOnAll") {
				reportUsage({ name: UsageDataEventName.SystemWideAccessGranted });
			}
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

	const appliesToLabelOf = (mapping: RbacGroupMapping): string =>
		mapping.scopeType === "AllUnits"
			? `All ${getTerm(TERMINOLOGY_KEYS.TEAMS)} and ${getTerm(TERMINOLOGY_KEYS.PORTFOLIOS)}`
			: "Everything";

	const renderAdminLabel = (user: RbacUser): React.ReactNode => {
		if (user.isEmergencyAdmin) {
			return (
				<Box sx={{ display: "flex", alignItems: "center", gap: 0.5 }}>
					<Typography variant="body2">Emergency Admin</Typography>
					<LockIcon fontSize="small" />
				</Box>
			);
		}
		if (user.isSystemAdmin) {
			return "Yes";
		}
		return "No";
	};

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
							label={`No direct role: ${status?.unassignedUserCount ?? 0}`}
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
					label="Show users with no direct role only"
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
						<TableCell>No direct role</TableCell>
						<TableCell align="right">Actions</TableCell>
					</TableRow>
				</TableHead>
				<TableBody>
					{visibleUsers.map((user) => {
						return (
							<TableRow key={user.id} data-testid={`rbac-user-row-${user.id}`}>
								<TableCell>{user.displayName || "(Unnamed user)"}</TableCell>
								<TableCell>{user.email || "-"}</TableCell>
								<TableCell>{user.subject}</TableCell>
								<TableCell>{renderAdminLabel(user)}</TableCell>
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
						);
					})}
				</TableBody>
			</Table>

			{status?.hasSystemAdmin && (
				<Box sx={{ mt: 4 }}>
					<Typography variant="h6" sx={{ mb: 1 }}>
						SSO Groups
					</Typography>

					<Box sx={{ display: "flex", gap: 1, mb: 2, flexWrap: "wrap" }}>
						<TextField
							label="Group value"
							size="small"
							value={groupValueInput}
							onChange={(event) => setGroupValueInput(event.target.value)}
							data-testid="rbac-group-mapping-group-value"
						/>
						<TextField
							select
							id="rbac-group-mapping-role"
							label="Role"
							size="small"
							value={roleChoice}
							onChange={(event) =>
								setRoleChoice(event.target.value as SsoGroupRole)
							}
							data-testid="rbac-group-mapping-role"
							sx={{ minWidth: 180 }}
						>
							{ssoGroupRoleChoices.map((choice) => (
								<MenuItem key={choice.value} value={choice.value}>
									{choice.label}
								</MenuItem>
							))}
						</TextField>
						<Button
							variant="contained"
							onClick={handleCreateGroupMapping}
							data-testid="rbac-create-group-mapping"
						>
							Add Mapping
						</Button>
					</Box>

					{rulesLoad === "loading" && (
						<Box sx={{ display: "flex", justifyContent: "center", py: 2 }}>
							<CircularProgress size={24} />
						</Box>
					)}
					{rulesLoad === "failed" && (
						<Alert severity="warning">
							Could not load the SSO group rules. Reload the page to try again.
						</Alert>
					)}
					{rulesLoad === "loaded" && (
						<Table size="small" data-testid="rbac-group-mappings-table">
							<TableHead>
								<TableRow>
									<TableCell>Group</TableCell>
									<TableCell>Role</TableCell>
									<TableCell>Applies to</TableCell>
									<TableCell align="right">Actions</TableCell>
								</TableRow>
							</TableHead>
							<TableBody>
								{groupMappings.map((mapping) => (
									<TableRow key={mapping.id}>
										<TableCell>{mapping.groupValue}</TableCell>
										<TableCell>{roleLabelOf(mapping)}</TableCell>
										<TableCell>{appliesToLabelOf(mapping)}</TableCell>
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
					)}
				</Box>
			)}

			{status?.hasSystemAdmin && <EveryoneSignedInSection />}

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
