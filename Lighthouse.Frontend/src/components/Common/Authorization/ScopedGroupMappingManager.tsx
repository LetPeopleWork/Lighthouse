import Alert from "@mui/material/Alert";
import Box from "@mui/material/Box";
import Button from "@mui/material/Button";
import CircularProgress from "@mui/material/CircularProgress";
import MenuItem from "@mui/material/MenuItem";
import Paper from "@mui/material/Paper";
import Stack from "@mui/material/Stack";
import Table from "@mui/material/Table";
import TableBody from "@mui/material/TableBody";
import TableCell from "@mui/material/TableCell";
import TableHead from "@mui/material/TableHead";
import TableRow from "@mui/material/TableRow";
import TextField from "@mui/material/TextField";
import Typography from "@mui/material/Typography";
import type React from "react";
import { useState } from "react";
import type {
	GroupMappingRole,
	RbacGroupMapping,
	ScopedRbacRole,
} from "../../../models/Authorization/RbacModels";

interface ScopedGroupMappingManagerProps {
	title: string;
	mappings: RbacGroupMapping[];
	allowedRoles: ScopedRbacRole[];
	loading: boolean;
	error?: string;
	onCreateMapping: (groupValue: string, role: ScopedRbacRole) => Promise<void>;
	onRemoveMapping: (mappingId: number) => Promise<void>;
}

const roleLabels: Record<ScopedRbacRole, string> = {
	Viewer: "Viewer",
	TeamAdmin: "Team Admin",
	PortfolioAdmin: "Portfolio Admin",
};

const getScopedRoleLabel = (role: GroupMappingRole): string => {
	if (role === "SystemAdmin") {
		return "System Admin";
	}

	return roleLabels[role] ?? "Viewer";
};

const ScopedGroupMappingManager: React.FC<ScopedGroupMappingManagerProps> = ({
	title,
	mappings,
	allowedRoles,
	loading,
	error,
	onCreateMapping,
	onRemoveMapping,
}) => {
	const [groupValueInput, setGroupValueInput] = useState("");
	const [roleInput, setRoleInput] = useState<ScopedRbacRole>(allowedRoles[0]);
	const [localError, setLocalError] = useState<string>();
	const [searchText, setSearchText] = useState("");

	const filteredMappings = mappings.filter((mapping) => {
		if (!searchText.trim()) return true;
		return mapping.groupValue.toLowerCase().includes(searchText.toLowerCase());
	});

	const handleCreate = async () => {
		setLocalError(undefined);
		const normalizedGroupValue = groupValueInput.trim();
		if (!normalizedGroupValue) {
			setLocalError("Group value is required.");
			return;
		}

		try {
			await onCreateMapping(normalizedGroupValue, roleInput);
			setGroupValueInput("");
		} catch {
			setLocalError("Failed to create group mapping.");
		}
	};

	return (
		<Paper variant="outlined" sx={{ p: 2 }}>
			<Stack spacing={2}>
				<Typography variant="h6" data-testid="scoped-groups-title">
					{title}
				</Typography>

				{error && <Alert severity="error">{error}</Alert>}
				{localError && <Alert severity="error">{localError}</Alert>}

				<Box sx={{ display: "flex", gap: 1, flexWrap: "wrap" }}>
					<TextField
						label="Group value"
						size="small"
						value={groupValueInput}
						onChange={(event) => setGroupValueInput(event.target.value)}
						data-testid="scoped-group-value-input"
					/>
					<TextField
						select
						label="Role"
						size="small"
						value={roleInput}
						onChange={(event) =>
							setRoleInput(event.target.value as ScopedRbacRole)
						}
						data-testid="scoped-group-role-input"
					>
						{allowedRoles.map((role) => (
							<MenuItem key={role} value={role}>
								{roleLabels[role]}
							</MenuItem>
						))}
					</TextField>
					<Button
						variant="contained"
						onClick={handleCreate}
						data-testid="scoped-group-add-button"
					>
						Add Group Mapping
					</Button>
				</Box>

				<Box>
					<TextField
						label="Search groups"
						size="small"
						value={searchText}
						onChange={(event) => setSearchText(event.target.value)}
						data-testid="scoped-groups-search"
						sx={{ minWidth: 220 }}
					/>
				</Box>

				{loading ? (
					<Box
						sx={{
							display: "flex",
							alignItems: "center",
							justifyContent: "center",
							py: 2,
						}}
						data-testid="scoped-groups-loading"
					>
						<CircularProgress size={24} />
					</Box>
				) : (
					<Table size="small" data-testid="scoped-groups-table">
						<TableHead>
							<TableRow>
								<TableCell>Group</TableCell>
								<TableCell>Role</TableCell>
								<TableCell align="right">Actions</TableCell>
							</TableRow>
						</TableHead>
						<TableBody>
							{filteredMappings.map((mapping) => (
								<TableRow
									key={mapping.id}
									data-testid={`scoped-group-row-${mapping.id}`}
								>
									<TableCell>{mapping.groupValue}</TableCell>
									<TableCell>{getScopedRoleLabel(mapping.role)}</TableCell>
									<TableCell align="right">
										<Button
											size="small"
											color="error"
											onClick={() => onRemoveMapping(mapping.id)}
											data-testid={`scoped-group-remove-${mapping.id}`}
										>
											Remove
										</Button>
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

export default ScopedGroupMappingManager;
