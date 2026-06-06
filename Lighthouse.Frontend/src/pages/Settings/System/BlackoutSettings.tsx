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
import type { IBlackoutPeriod } from "../../../models/BlackoutPeriod";
import {
	type DayOfWeek,
	type IRecurringBlackoutRule,
	ORDERED_WEEKDAYS,
} from "../../../models/RecurringBlackoutRule";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";

interface BlackoutPeriodFormData {
	start: string;
	end: string;
	description: string;
}

interface RecurringRuleFormData {
	weekdays: DayOfWeek[];
	intervalWeeks: number;
	start: string;
	end: string;
	description: string;
}

const emptyPeriodForm: BlackoutPeriodFormData = {
	start: "",
	end: "",
	description: "",
};

const emptyRuleForm: RecurringRuleFormData = {
	weekdays: [],
	intervalWeeks: 1,
	start: "",
	end: "",
	description: "",
};

const messageFromError = (error: unknown, fallback: string): string =>
	error instanceof Error && error.message ? error.message : fallback;

interface BlackoutSettingsProps {
	isPremium: boolean;
}

const BlackoutSettings: React.FC<BlackoutSettingsProps> = ({ isPremium }) => {
	const { blackoutPeriodService, recurringBlackoutRuleService } =
		useContext(ApiServiceContext);

	const [periods, setPeriods] = useState<IBlackoutPeriod[]>([]);
	const [rules, setRules] = useState<IRecurringBlackoutRule[]>([]);
	const [loading, setLoading] = useState(true);
	const [error, setError] = useState<string | null>(null);

	const [periodDialogOpen, setPeriodDialogOpen] = useState(false);
	const [editingPeriod, setEditingPeriod] = useState<IBlackoutPeriod | null>(
		null,
	);
	const [periodForm, setPeriodForm] =
		useState<BlackoutPeriodFormData>(emptyPeriodForm);
	const [periodFormError, setPeriodFormError] = useState<string | null>(null);
	const [periodSaving, setPeriodSaving] = useState(false);
	const [deletePeriodId, setDeletePeriodId] = useState<number | null>(null);

	const [ruleDialogOpen, setRuleDialogOpen] = useState(false);
	const [editingRule, setEditingRule] = useState<IRecurringBlackoutRule | null>(
		null,
	);
	const [ruleForm, setRuleForm] =
		useState<RecurringRuleFormData>(emptyRuleForm);
	const [ruleFormError, setRuleFormError] = useState<string | null>(null);
	const [ruleSaving, setRuleSaving] = useState(false);
	const [deleteRuleId, setDeleteRuleId] = useState<number | null>(null);

	const fetchPeriods = useCallback(async () => {
		const data = await blackoutPeriodService.getAll();
		setPeriods(data);
	}, [blackoutPeriodService]);

	const fetchRules = useCallback(async () => {
		const data = await recurringBlackoutRuleService.getAll();
		setRules(data);
	}, [recurringBlackoutRuleService]);

	const fetchAll = useCallback(async () => {
		setLoading(true);
		setError(null);
		const results = await Promise.allSettled([fetchPeriods(), fetchRules()]);
		if (results.some((result) => result.status === "rejected")) {
			setError("Failed to load blackout periods or recurring rules");
		}
		setLoading(false);
	}, [fetchPeriods, fetchRules]);

	useEffect(() => {
		fetchAll();
	}, [fetchAll]);

	const openAddPeriod = () => {
		setEditingPeriod(null);
		setPeriodForm(emptyPeriodForm);
		setPeriodFormError(null);
		setPeriodDialogOpen(true);
	};

	const openEditPeriod = (period: IBlackoutPeriod) => {
		setEditingPeriod(period);
		setPeriodForm({
			start: period.start,
			end: period.end,
			description: period.description,
		});
		setPeriodFormError(null);
		setPeriodDialogOpen(true);
	};

	const closePeriodDialog = () => {
		setPeriodDialogOpen(false);
		setEditingPeriod(null);
		setPeriodForm(emptyPeriodForm);
		setPeriodFormError(null);
	};

	const savePeriod = async () => {
		if (!periodForm.start || !periodForm.end) {
			setPeriodFormError("Start and end dates are required.");
			return;
		}

		if (periodForm.start > periodForm.end) {
			setPeriodFormError("Start date must be on or before end date.");
			return;
		}

		try {
			setPeriodSaving(true);
			setPeriodFormError(null);
			const payload = {
				start: periodForm.start,
				end: periodForm.end,
				description: periodForm.description,
			};
			if (editingPeriod) {
				await blackoutPeriodService.update(editingPeriod.id, payload);
			} else {
				await blackoutPeriodService.create(payload);
			}
			closePeriodDialog();
			await fetchPeriods();
		} catch {
			setPeriodFormError("Failed to save blackout period.");
		} finally {
			setPeriodSaving(false);
		}
	};

	const deletePeriod = async (id: number) => {
		try {
			setError(null);
			await blackoutPeriodService.delete(id);
			setDeletePeriodId(null);
			await fetchPeriods();
		} catch {
			setError("Failed to delete blackout period.");
		}
	};

	const openAddRule = () => {
		setEditingRule(null);
		setRuleForm(emptyRuleForm);
		setRuleFormError(null);
		setRuleDialogOpen(true);
	};

	const openEditRule = (rule: IRecurringBlackoutRule) => {
		setEditingRule(rule);
		setRuleForm({
			weekdays: rule.weekdays,
			intervalWeeks: rule.intervalWeeks,
			start: rule.start,
			end: rule.end ?? "",
			description: rule.description,
		});
		setRuleFormError(null);
		setRuleDialogOpen(true);
	};

	const closeRuleDialog = () => {
		setRuleDialogOpen(false);
		setEditingRule(null);
		setRuleForm(emptyRuleForm);
		setRuleFormError(null);
	};

	const toggleWeekday = (day: DayOfWeek) => {
		setRuleForm((current) => ({
			...current,
			weekdays: current.weekdays.includes(day)
				? current.weekdays.filter((selected) => selected !== day)
				: [...current.weekdays, day],
		}));
	};

	const saveRule = async () => {
		if (ruleForm.weekdays.length === 0 || !ruleForm.start) {
			setRuleFormError("Select at least one weekday and a start date.");
			return;
		}

		if (ruleForm.end && ruleForm.start > ruleForm.end) {
			setRuleFormError("Start date must be on or before end date.");
			return;
		}

		try {
			setRuleSaving(true);
			setRuleFormError(null);
			const payload = {
				weekdays: ruleForm.weekdays,
				intervalWeeks: ruleForm.intervalWeeks,
				start: ruleForm.start,
				end: ruleForm.end === "" ? null : ruleForm.end,
				description: ruleForm.description,
			};
			if (editingRule) {
				await recurringBlackoutRuleService.update(editingRule.id, payload);
			} else {
				await recurringBlackoutRuleService.create(payload);
			}
			closeRuleDialog();
			await fetchRules();
		} catch (saveError) {
			setRuleFormError(
				messageFromError(saveError, "Failed to save recurring blackout rule."),
			);
		} finally {
			setRuleSaving(false);
		}
	};

	const deleteRule = async (id: number) => {
		try {
			setError(null);
			await recurringBlackoutRuleService.delete(id);
			setDeleteRuleId(null);
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
					Loading blackout settings...
				</Typography>
			</Box>
		);
	}

	const isEmpty = periods.length === 0 && rules.length === 0;
	const columnCount = isPremium ? 3 : 2;

	return (
		<Box>
			{error && (
				<Alert severity="error" sx={{ mb: 2 }} onClose={() => setError(null)}>
					{error}
				</Alert>
			)}

			{!isPremium && (
				<Alert severity="info" sx={{ mb: 2 }}>
					Managing blackout periods and recurring rules requires a premium
					license. Existing entries remain active.
				</Alert>
			)}

			<Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
				Define global blackout periods and recurring rules (non-working days) to
				exclude from forecasts. Blackout days are shown as annotations on
				metrics charts without altering observed values.
			</Typography>

			<Box sx={{ display: "flex", justifyContent: "flex-end", gap: 1, mb: 2 }}>
				<LicenseTooltip
					canUseFeature={isPremium}
					defaultTooltip="Add a new blackout period"
					premiumExtraInfo="Please obtain a premium license to manage blackout periods."
				>
					<span>
						<Button
							variant="contained"
							startIcon={<AddIcon />}
							onClick={openAddPeriod}
							disabled={!isPremium}
							data-testid="add-blackout-period-button"
						>
							Add Blackout Period
						</Button>
					</span>
				</LicenseTooltip>
				<LicenseTooltip
					canUseFeature={isPremium}
					defaultTooltip="Add a new recurring blackout rule"
					premiumExtraInfo="Please obtain a premium license to manage recurring blackout rules."
				>
					<span>
						<Button
							variant="contained"
							startIcon={<AddIcon />}
							onClick={openAddRule}
							disabled={!isPremium}
							data-testid="add-recurring-blackout-rule-button"
						>
							Add Recurring Rule
						</Button>
					</span>
				</LicenseTooltip>
			</Box>

			<TableContainer>
				<Table data-testid="blackout-settings-table">
					<TableHead>
						<TableRow>
							<TableCell>Schedule</TableCell>
							<TableCell>Description</TableCell>
							{isPremium && <TableCell align="right">Actions</TableCell>}
						</TableRow>
					</TableHead>
					<TableBody>
						{isEmpty && (
							<TableRow>
								<TableCell colSpan={columnCount} align="center">
									<Typography variant="body2" color="text.secondary">
										No blackout periods or recurring rules configured.
									</Typography>
								</TableCell>
							</TableRow>
						)}
						{periods.map((period) => (
							<TableRow
								key={`period-${period.id}`}
								data-testid={`blackout-period-row-${period.id}`}
							>
								<TableCell>{`${period.start} → ${period.end}`}</TableCell>
								<TableCell>{period.description}</TableCell>
								{isPremium && (
									<TableCell align="right">
										<IconButton
											onClick={() => openEditPeriod(period)}
											size="small"
											data-testid={`edit-blackout-${period.id}`}
										>
											<EditIcon />
										</IconButton>
										<IconButton
											onClick={() => setDeletePeriodId(period.id)}
											size="small"
											color="error"
											data-testid={`delete-blackout-${period.id}`}
										>
											<DeleteIcon />
										</IconButton>
									</TableCell>
								)}
							</TableRow>
						))}
						{rules.map((rule) => (
							<TableRow
								key={`rule-${rule.id}`}
								data-testid={`recurring-blackout-row-${rule.id}`}
							>
								<TableCell>{rule.summary}</TableCell>
								<TableCell>{rule.description}</TableCell>
								{isPremium && (
									<TableCell align="right">
										<IconButton
											onClick={() => openEditRule(rule)}
											size="small"
											data-testid={`edit-recurring-blackout-${rule.id}`}
										>
											<EditIcon />
										</IconButton>
										<IconButton
											onClick={() => setDeleteRuleId(rule.id)}
											size="small"
											color="error"
											data-testid={`delete-recurring-blackout-${rule.id}`}
										>
											<DeleteIcon />
										</IconButton>
									</TableCell>
								)}
							</TableRow>
						))}
					</TableBody>
				</Table>
			</TableContainer>

			<Dialog
				open={periodDialogOpen}
				onClose={closePeriodDialog}
				maxWidth="sm"
				fullWidth
			>
				<DialogTitle>
					{editingPeriod ? "Edit Blackout Period" : "Add Blackout Period"}
				</DialogTitle>
				<DialogContent>
					{periodFormError && (
						<Alert
							severity="error"
							sx={{ mb: 2, mt: 1 }}
							onClose={() => setPeriodFormError(null)}
						>
							{periodFormError}
						</Alert>
					)}
					<TextField
						label="Start Date"
						type="date"
						value={periodForm.start}
						onChange={(e) =>
							setPeriodForm({ ...periodForm, start: e.target.value })
						}
						fullWidth
						sx={{ mt: 2, mb: 2 }}
						slotProps={{ inputLabel: { shrink: true } }}
						data-testid="blackout-start-date"
					/>
					<TextField
						label="End Date"
						type="date"
						value={periodForm.end}
						onChange={(e) =>
							setPeriodForm({ ...periodForm, end: e.target.value })
						}
						fullWidth
						sx={{ mb: 2 }}
						slotProps={{ inputLabel: { shrink: true } }}
						data-testid="blackout-end-date"
					/>
					<TextField
						label="Description (optional)"
						value={periodForm.description}
						onChange={(e) =>
							setPeriodForm({ ...periodForm, description: e.target.value })
						}
						fullWidth
						sx={{ mb: 2 }}
						data-testid="blackout-description"
					/>
				</DialogContent>
				<DialogActions>
					<Button onClick={closePeriodDialog} disabled={periodSaving}>
						Cancel
					</Button>
					<Button
						onClick={savePeriod}
						variant="contained"
						disabled={periodSaving}
						data-testid="save-blackout-period"
					>
						{periodSaving ? <CircularProgress size={20} /> : "Save"}
					</Button>
				</DialogActions>
			</Dialog>

			<Dialog
				open={ruleDialogOpen}
				onClose={closeRuleDialog}
				maxWidth="sm"
				fullWidth
			>
				<DialogTitle>
					{editingRule
						? "Edit Recurring Blackout Rule"
						: "Add Recurring Blackout Rule"}
				</DialogTitle>
				<DialogContent>
					{ruleFormError && (
						<Alert
							severity="error"
							sx={{ mb: 2, mt: 1 }}
							onClose={() => setRuleFormError(null)}
						>
							{ruleFormError}
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
										checked={ruleForm.weekdays.includes(day)}
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
						value={ruleForm.intervalWeeks}
						onChange={(e) =>
							setRuleForm({
								...ruleForm,
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
						value={ruleForm.start}
						onChange={(e) =>
							setRuleForm({ ...ruleForm, start: e.target.value })
						}
						fullWidth
						sx={{ mb: 2 }}
						slotProps={{ inputLabel: { shrink: true } }}
						data-testid="recurring-start-date"
					/>
					<TextField
						label="End Date (optional)"
						type="date"
						value={ruleForm.end}
						onChange={(e) => setRuleForm({ ...ruleForm, end: e.target.value })}
						fullWidth
						sx={{ mb: 2 }}
						slotProps={{ inputLabel: { shrink: true } }}
						data-testid="recurring-end-date"
					/>
					<TextField
						label="Description (optional)"
						value={ruleForm.description}
						onChange={(e) =>
							setRuleForm({ ...ruleForm, description: e.target.value })
						}
						fullWidth
						sx={{ mb: 2 }}
						data-testid="recurring-description"
					/>
				</DialogContent>
				<DialogActions>
					<Button onClick={closeRuleDialog} disabled={ruleSaving}>
						Cancel
					</Button>
					<Button
						onClick={saveRule}
						variant="contained"
						disabled={ruleSaving}
						data-testid="save-recurring-blackout-rule"
					>
						{ruleSaving ? <CircularProgress size={20} /> : "Save"}
					</Button>
				</DialogActions>
			</Dialog>

			<Dialog
				open={deletePeriodId !== null}
				onClose={() => setDeletePeriodId(null)}
			>
				<DialogTitle>Delete Blackout Period</DialogTitle>
				<DialogContent>
					<Typography>
						Are you sure you want to delete this blackout period?
					</Typography>
				</DialogContent>
				<DialogActions>
					<Button onClick={() => setDeletePeriodId(null)}>Cancel</Button>
					<Button
						onClick={() =>
							deletePeriodId !== null && deletePeriod(deletePeriodId)
						}
						variant="contained"
						color="error"
						data-testid="confirm-delete-blackout"
					>
						Delete
					</Button>
				</DialogActions>
			</Dialog>

			<Dialog
				open={deleteRuleId !== null}
				onClose={() => setDeleteRuleId(null)}
			>
				<DialogTitle>Delete Recurring Blackout Rule</DialogTitle>
				<DialogContent>
					<Typography>
						Are you sure you want to delete this recurring blackout rule?
					</Typography>
				</DialogContent>
				<DialogActions>
					<Button onClick={() => setDeleteRuleId(null)}>Cancel</Button>
					<Button
						onClick={() => deleteRuleId !== null && deleteRule(deleteRuleId)}
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

export default BlackoutSettings;
