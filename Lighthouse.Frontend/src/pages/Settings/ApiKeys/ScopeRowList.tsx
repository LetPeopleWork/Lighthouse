import DeleteIcon from "@mui/icons-material/Delete";
import Box from "@mui/material/Box";
import Button from "@mui/material/Button";
import IconButton from "@mui/material/IconButton";
import MenuItem from "@mui/material/MenuItem";
import Stack from "@mui/material/Stack";
import TextField from "@mui/material/TextField";
import Tooltip from "@mui/material/Tooltip";
import type React from "react";
import type {
	ApiKeyScopeRole,
	ApiKeyScopeType,
} from "../../../models/ApiKey/ApiKey";

export interface IScopeRow {
	role?: ApiKeyScopeRole;
	scopeType?: ApiKeyScopeType;
	scopeId?: number;
}

export interface ITeamOption {
	id: number;
	name: string;
}

export interface IPortfolioOption {
	id: number;
	name: string;
}

export interface IScopeRowListProps {
	rows: IScopeRow[];
	onChange: (rows: IScopeRow[]) => void;
	availableTeams: ITeamOption[];
	availablePortfolios: IPortfolioOption[];
}

type AccessLabel = "Read access" | "Write access" | "System administrator";

const SCOPE_TYPE_OPTIONS: ApiKeyScopeType[] = ["System", "Team", "Portfolio"];

const accessLabelsFor = (
	scopeType: ApiKeyScopeType | undefined,
): AccessLabel[] => {
	if (scopeType === "Team" || scopeType === "Portfolio") {
		return ["Read access", "Write access"];
	}
	if (scopeType === "System") {
		return ["System administrator"];
	}
	return [];
};

const accessLabelToWireRole = (
	label: AccessLabel,
	scopeType: ApiKeyScopeType | undefined,
): ApiKeyScopeRole | undefined => {
	if (label === "System administrator" && scopeType === "System") {
		return "SystemAdmin";
	}
	if (label === "Read access" && scopeType === "Team") return "Viewer";
	if (label === "Read access" && scopeType === "Portfolio") return "Viewer";
	if (label === "Write access" && scopeType === "Team") return "TeamAdmin";
	if (label === "Write access" && scopeType === "Portfolio") {
		return "PortfolioAdmin";
	}
	return undefined;
};

const wireRoleToAccessLabel = (
	role: ApiKeyScopeRole | undefined,
): AccessLabel | "" => {
	if (role === "SystemAdmin") return "System administrator";
	if (role === "Viewer") return "Read access";
	if (role === "TeamAdmin" || role === "PortfolioAdmin") return "Write access";
	return "";
};

const updateRow = (
	rows: IScopeRow[],
	index: number,
	patch: Partial<IScopeRow>,
): IScopeRow[] =>
	rows.map((row, rowIndex) =>
		rowIndex === index ? { ...row, ...patch } : row,
	);

const removeRowAt = (rows: IScopeRow[], index: number): IScopeRow[] =>
	rows.filter((_, rowIndex) => rowIndex !== index);

const appendEmptyRow = (rows: IScopeRow[]): IScopeRow[] => [...rows, {}];

const targetOptionsFor = (
	scopeType: ApiKeyScopeType | undefined,
	availableTeams: ITeamOption[],
	availablePortfolios: IPortfolioOption[],
): { id: number; name: string }[] => {
	if (scopeType === "Team") return availableTeams;
	if (scopeType === "Portfolio") return availablePortfolios;
	return [];
};

const ScopeRowList: React.FC<IScopeRowListProps> = ({
	rows,
	onChange,
	availableTeams,
	availablePortfolios,
}) => {
	const handleRoleChange = (index: number, value: string) => {
		const row = rows[index];
		const role = accessLabelToWireRole(value as AccessLabel, row.scopeType);
		onChange(updateRow(rows, index, { role }));
	};

	const handleScopeTypeChange = (index: number, value: string) => {
		onChange(
			updateRow(rows, index, {
				scopeType: value as ApiKeyScopeType,
				role: undefined,
				scopeId: undefined,
			}),
		);
	};

	const handleScopeIdChange = (index: number, value: string) => {
		const parsed = value === "" ? undefined : Number(value);
		onChange(updateRow(rows, index, { scopeId: parsed }));
	};

	const handleRemove = (index: number) => {
		onChange(removeRowAt(rows, index));
	};

	const handleAdd = () => {
		onChange(appendEmptyRow(rows));
	};

	return (
		<Box data-testid="scope-row-list">
			<Stack spacing={2}>
				{rows.map((row, index) => {
					const rowKey = `scope-row-${index}`;
					const targets = targetOptionsFor(
						row.scopeType,
						availableTeams,
						availablePortfolios,
					);
					const targetDisabled =
						row.scopeType !== "Team" && row.scopeType !== "Portfolio";
					const roleOptions = accessLabelsFor(row.scopeType);
					const roleDisabled = roleOptions.length === 0;
					const roleValue = wireRoleToAccessLabel(row.role);

					return (
						<Stack
							key={rowKey}
							direction="row"
							spacing={1}
							sx={{ alignItems: "center" }}
							data-testid={`scope-row-${index}`}
						>
							<TextField
								select
								label="Scope type"
								value={row.scopeType ?? ""}
								onChange={(event) =>
									handleScopeTypeChange(index, event.target.value)
								}
								size="small"
								sx={{ minWidth: 140 }}
								slotProps={{
									htmlInput: { "data-testid": `scope-row-${index}-scope-type` },
								}}
							>
								{SCOPE_TYPE_OPTIONS.map((scopeType) => (
									<MenuItem key={scopeType} value={scopeType}>
										{scopeType}
									</MenuItem>
								))}
							</TextField>
							<TextField
								select
								label="Access"
								value={roleValue}
								onChange={(event) =>
									handleRoleChange(index, event.target.value)
								}
								size="small"
								sx={{ minWidth: 200 }}
								disabled={roleDisabled}
								slotProps={{
									htmlInput: { "data-testid": `scope-row-${index}-role` },
								}}
							>
								{roleOptions.map((label) => (
									<MenuItem key={label} value={label}>
										{label}
									</MenuItem>
								))}
							</TextField>
							<TextField
								select
								label="Target"
								value={
									row.scopeId !== undefined && row.scopeId !== null
										? String(row.scopeId)
										: ""
								}
								onChange={(event) =>
									handleScopeIdChange(index, event.target.value)
								}
								size="small"
								sx={{ minWidth: 200 }}
								disabled={targetDisabled || targets.length === 0}
								slotProps={{
									htmlInput: { "data-testid": `scope-row-${index}-scope-id` },
								}}
							>
								{targets.map((target) => (
									<MenuItem key={target.id} value={String(target.id)}>
										{target.name}
									</MenuItem>
								))}
							</TextField>
							<Tooltip title="Remove scope">
								<IconButton
									onClick={() => handleRemove(index)}
									size="small"
									aria-label={`Remove scope row ${index + 1}`}
									data-testid={`scope-row-${index}-remove`}
								>
									<DeleteIcon fontSize="small" />
								</IconButton>
							</Tooltip>
						</Stack>
					);
				})}
				<Box>
					<Button
						onClick={handleAdd}
						size="small"
						data-testid="scope-row-list-add-button"
					>
						Add scope
					</Button>
				</Box>
			</Stack>
		</Box>
	);
};

export default ScopeRowList;
