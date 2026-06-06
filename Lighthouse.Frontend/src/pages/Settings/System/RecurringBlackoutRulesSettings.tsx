import AddIcon from "@mui/icons-material/Add";
import DeleteIcon from "@mui/icons-material/Delete";
import EditIcon from "@mui/icons-material/Edit";
import {
	Alert,
	Box,
	Button,
	Checkbox,
	CircularProgress,
	Dialog,
	DialogActions,
	DialogContent,
	DialogTitle,
	FormControlLabel,
	FormGroup,
	IconButton,
	Table,
	TableBody,
	TableCell,
	TableContainer,
	TableHead,
	TableRow,
	TextField,
	Typography,
} from "@mui/material";
import type React from "react";
import { useCallback, useContext, useEffect, useState } from "react";
import { LicenseTooltip } from "../../../components/App/License/LicenseToolTip";
import {
	type DayOfWeek,
	type IRecurringBlackoutRule,
	ORDERED_WEEKDAYS,
} from "../../../models/RecurringBlackoutRule";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";

interface RecurringRuleFormData {
	weekdays: DayOfWeek[];
	intervalWeeks: number;
	start: string;
	end: string;
	description: string;
}

const emptyForm: RecurringRuleFormData = {
	weekdays: [],
	intervalWeeks: 1,
	start: "",
	end: "",
	description: "",
};

const messageFromError = (error: unknown, fallback: string): string =>
	error instanceof Error && error.message ? error.message : fallback;

interface RecurringBlackoutRulesSettingsProps {
	isPremium: boolean;
}

const RecurringBlackoutRulesSettings: React.FC<
	RecurringBlackoutRulesSettingsProps
> = ({ isPremium }) => {
	const [rules, setRules] = useState<IRecurringBlackoutRule[]>([]);
	const [loading, setLoading] = useState(true);
	const [error, setError] = useState<string | null>(null);
	const [dialogOpen, setDialogOpen] = useState(false);
	const [editingRule, setEditingRule] = useState<IRecurringBlackoutRule | null>(
		null,
	);
	const [formData, setFormData] = useState<RecurringRuleFormData>(emptyForm);
	const [formError, setFormError] = useState<string | null>(null);
	const [saving, setSaving] = useState(false);
	const [deleteConfirmId, setDeleteConfirmId] = useState<number | null>(null);

	const { recurringBlackoutRuleService } = useContext(ApiServiceContext);

	const fetchRules = useCallback(async () => {
		try {
			setLoading(true);
			setError(null);
			const data = await recurringBlackoutRuleService.getAll();
			setRules(data);
		} catch {
			setError("Failed to load recurring blackout rules");
		} finally {
			setLoading(false);
		}
	}, [recurringBlackoutRuleService]);

	useEffect(() => {
		fetchRules();
	}, [fetchRules]);

	const handleOpenAdd = () => {
		setEditingRule(null);
		setFormData(emptyForm);
		setFormError(null);
		setDialogOpen(true);
	};

	const handleOpenEdit = (rule: IRecurringBlackoutRule) => {
		setEditingRule(rule);
		setFormData({
			weekdays: rule.weekdays,
			intervalWeeks: rule.intervalWeeks,
			start: rule.start,
			end: rule.end ?? "",
			description: rule.description,
		});
		setFormError(null);
		setDialogOpen(true);
	};

	const handleCloseDialog = () => {
		setDialogOpen(false);
		setEditingRule(null);
		setFormData(emptyForm);
		setFormError(null);
	};

	const toggleWeekday = (day: DayOfWeek) => {
		setFormData((current) => ({
			...current,
			weekdays: current.weekdays.includes(day)
				? current.weekdays.filter((selected) => selected !== day)
				: [...current.weekdays, day],
		}));
	};

	const handleSave = async () => {
		if (formData.weekdays.length === 0 || !formData.start) {
			setFormError("Select at least one weekday and a start date.");
			return;
		}

		if (formData.end && formData.start > formData.end) {
			setFormError("Start date must be on or before end date.");
			return;
		}

		try {
			setSaving(true);
			setFormError(null);

			const payload = {
				weekdays: formData.weekdays,
				intervalWeeks: formData.intervalWeeks,
				start: formData.start,
				end: formData.end === "" ? null : formData.end,
				description: formData.description,
			};

			if (editingRule) {
				await recurringBlackoutRuleService.update(editingRule.id, payload);
			} else {
				await recurringBlackoutRuleService.create(payload);
			}

			handleCloseDialog();
			await fetchRules();
		} catch (saveError) {
			setFormError(
				messageFromError(saveError, "Failed to save recurring blackout rule."),
			);
		} finally {
			setSaving(false);
		}
	};

	const handleDelete = async (id: number) => {
		try {
			setError(null);
			await recurringBlackoutRuleService.delete(id);
			setDeleteConfirmId(null);
			await fetchRules();
		} catch (deleteError) {
			setError(
				messageFromError(
					deleteError,
					"Failed to delete recurring blackout rule.",
				),
			);
		}
	};

	if (loading) {
		return (
			<Box
				sx={{
					display: "flex",
					justifyContent: "center",
					alignItems: "center",
					p: 2,
				}}
			>
				<CircularProgress />
				<Typography variant="body1" sx={{ ml: 2 }}>
					Loading recurring blackout rules...
				</Typography>
			</Box>
		);
	}

	return (
		<Box>
			{error && (
				<Alert severity="error" sx={{ mb: 2 }} onClose={() => setError(null)}>
					{error}
				</Alert>
			)}

			{!isPremium && (
				<Alert severity="info" sx={{ mb: 2 }}>
					Managing recurring blackout rules requires a premium license. Existing
					rules remain active.
				</Alert>
			)}

			<Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
				Define recurring blackout rules (e.g. every weekend, every other Friday)
				to exclude repeating non-working days from forecasts.
			</Typography>

			<Box sx={{ display: "flex", justifyContent: "flex-end", mb: 2 }}>
				<LicenseTooltip
					canUseFeature={isPremium}
					defaultTooltip="Add a new recurring blackout rule"
					premiumExtraInfo="Please obtain a premium license to manage recurring blackout rules."
				>
					<span>
						<Button
							variant="contained"
							startIcon={<AddIcon />}
							onClick={handleOpenAdd}
							disabled={!isPremium}
							data-testid="add-recurring-blackout-rule-button"
						>
							Add Recurring Rule
						</Button>
					</span>
				</LicenseTooltip>
			</Box>

			<TableContainer>
				<Table data-testid="recurring-blackout-rules-table">
					<TableHead>
						<TableRow>
							<TableCell>Rule</TableCell>
							{isPremium && <TableCell align="right">Actions</TableCell>}
						</TableRow>
					</TableHead>
					<TableBody>
						{rules.length === 0 ? (
							<TableRow>
								<TableCell colSpan={isPremium ? 2 : 1} align="center">
									<Typography variant="body2" color="text.secondary">
										No recurring blackout rules configured.
									</Typography>
								</TableCell>
							</TableRow>
						) : (
							rules.map((rule) => (
								<TableRow
									key={rule.id}
									data-testid={`recurring-blackout-row-${rule.id}`}
								>
									<TableCell>{rule.summary}</TableCell>
									{isPremium && (
										<TableCell align="right">
											<IconButton
												onClick={() => handleOpenEdit(rule)}
												size="small"
												data-testid={`edit-recurring-blackout-${rule.id}`}
											>
												<EditIcon />
											</IconButton>
											<IconButton
												onClick={() => setDeleteConfirmId(rule.id)}
												size="small"
												color="error"
												data-testid={`delete-recurring-blackout-${rule.id}`}
											>
												<DeleteIcon />
											</IconButton>
										</TableCell>
									)}
								</TableRow>
							))
						)}
					</TableBody>
				</Table>
			</TableContainer>

			<Dialog
				open={dialogOpen}
				onClose={handleCloseDialog}
				maxWidth="sm"
				fullWidth
			>
				<DialogTitle>
					{editingRule
						? "Edit Recurring Blackout Rule"
						: "Add Recurring Blackout Rule"}
				</DialogTitle>
				<DialogContent>
					{formError && (
						<Alert
							severity="error"
							sx={{ mb: 2, mt: 1 }}
							onClose={() => setFormError(null)}
						>
							{formError}
						</Alert>
					)}
					<Typography variant="subtitle2" sx={{ mt: 2, mb: 1 }}>
						Weekdays
					</Typography>
					<FormGroup row>
						{ORDERED_WEEKDAYS.map((day) => (
							<FormControlLabel
								key={day}
								control={
									<Checkbox
										checked={formData.weekdays.includes(day)}
										onChange={() => toggleWeekday(day)}
										data-testid={`recurring-weekday-${day}`}
									/>
								}
								label={day}
							/>
						))}
					</FormGroup>
					<TextField
						label="Repeat every (weeks)"
						type="number"
						value={formData.intervalWeeks}
						onChange={(e) =>
							setFormData({
								...formData,
								intervalWeeks: Number.parseInt(e.target.value, 10) || 1,
							})
						}
						fullWidth
						sx={{ mt: 2, mb: 2 }}
						slotProps={{ htmlInput: { min: 1 } }}
						data-testid="recurring-interval-weeks"
					/>
					<TextField
						label="Start Date"
						type="date"
						value={formData.start}
						onChange={(e) =>
							setFormData({ ...formData, start: e.target.value })
						}
						fullWidth
						sx={{ mb: 2 }}
						slotProps={{ inputLabel: { shrink: true } }}
						data-testid="recurring-start-date"
					/>
					<TextField
						label="End Date (optional)"
						type="date"
						value={formData.end}
						onChange={(e) => setFormData({ ...formData, end: e.target.value })}
						fullWidth
						sx={{ mb: 2 }}
						slotProps={{ inputLabel: { shrink: true } }}
						data-testid="recurring-end-date"
					/>
					<TextField
						label="Description (optional)"
						value={formData.description}
						onChange={(e) =>
							setFormData({ ...formData, description: e.target.value })
						}
						fullWidth
						sx={{ mb: 2 }}
						data-testid="recurring-description"
					/>
				</DialogContent>
				<DialogActions>
					<Button onClick={handleCloseDialog} disabled={saving}>
						Cancel
					</Button>
					<Button
						onClick={handleSave}
						variant="contained"
						disabled={saving}
						data-testid="save-recurring-blackout-rule"
					>
						{saving ? <CircularProgress size={20} /> : "Save"}
					</Button>
				</DialogActions>
			</Dialog>

			<Dialog
				open={deleteConfirmId !== null}
				onClose={() => setDeleteConfirmId(null)}
			>
				<DialogTitle>Delete Recurring Blackout Rule</DialogTitle>
				<DialogContent>
					<Typography>
						Are you sure you want to delete this recurring blackout rule?
					</Typography>
				</DialogContent>
				<DialogActions>
					<Button onClick={() => setDeleteConfirmId(null)}>Cancel</Button>
					<Button
						onClick={() =>
							deleteConfirmId !== null && handleDelete(deleteConfirmId)
						}
						variant="contained"
						color="error"
						data-testid="confirm-delete-recurring-blackout"
					>
						Delete
					</Button>
				</DialogActions>
			</Dialog>
		</Box>
	);
};

export default RecurringBlackoutRulesSettings;
