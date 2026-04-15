import AddIcon from "@mui/icons-material/Add";
import DeleteIcon from "@mui/icons-material/Delete";
import EditIcon from "@mui/icons-material/Edit";
import {
	Alert,
	Box,
	Button,
	CircularProgress,
	Dialog,
	DialogActions,
	DialogContent,
	DialogTitle,
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
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";

interface BlackoutPeriodFormData {
	start: string;
	end: string;
	description: string;
}

const emptyForm: BlackoutPeriodFormData = {
	start: "",
	end: "",
	description: "",
};

interface BlackoutPeriodsSettingsProps {
	isPremium: boolean;
}

const BlackoutPeriodsSettings: React.FC<BlackoutPeriodsSettingsProps> = ({
	isPremium,
}) => {
	const [periods, setPeriods] = useState<IBlackoutPeriod[]>([]);
	const [loading, setLoading] = useState(true);
	const [error, setError] = useState<string | null>(null);
	const [dialogOpen, setDialogOpen] = useState(false);
	const [editingPeriod, setEditingPeriod] = useState<IBlackoutPeriod | null>(
		null,
	);
	const [formData, setFormData] = useState<BlackoutPeriodFormData>(emptyForm);
	const [formError, setFormError] = useState<string | null>(null);
	const [saving, setSaving] = useState(false);
	const [deleteConfirmId, setDeleteConfirmId] = useState<number | null>(null);

	const { blackoutPeriodService } = useContext(ApiServiceContext);

	const fetchPeriods = useCallback(async () => {
		try {
			setLoading(true);
			setError(null);
			const data = await blackoutPeriodService.getAll();
			setPeriods(data);
		} catch {
			setError("Failed to load blackout periods");
		} finally {
			setLoading(false);
		}
	}, [blackoutPeriodService]);

	useEffect(() => {
		fetchPeriods();
	}, [fetchPeriods]);

	const handleOpenAdd = () => {
		setEditingPeriod(null);
		setFormData(emptyForm);
		setFormError(null);
		setDialogOpen(true);
	};

	const handleOpenEdit = (period: IBlackoutPeriod) => {
		setEditingPeriod(period);
		setFormData({
			start: period.start,
			end: period.end,
			description: period.description,
		});
		setFormError(null);
		setDialogOpen(true);
	};

	const handleCloseDialog = () => {
		setDialogOpen(false);
		setEditingPeriod(null);
		setFormData(emptyForm);
		setFormError(null);
	};

	const handleSave = async () => {
		if (!formData.start || !formData.end) {
			setFormError("Start and end dates are required.");
			return;
		}

		if (formData.start > formData.end) {
			setFormError("Start date must be on or before end date.");
			return;
		}

		try {
			setSaving(true);
			setFormError(null);

			const payload = {
				start: formData.start,
				end: formData.end,
				description: formData.description,
			};

			if (editingPeriod) {
				await blackoutPeriodService.update(editingPeriod.id, payload);
			} else {
				await blackoutPeriodService.create(payload);
			}

			handleCloseDialog();
			await fetchPeriods();
		} catch {
			setFormError("Failed to save blackout period.");
		} finally {
			setSaving(false);
		}
	};

	const handleDelete = async (id: number) => {
		try {
			setError(null);
			await blackoutPeriodService.delete(id);
			setDeleteConfirmId(null);
			await fetchPeriods();
		} catch {
			setError("Failed to delete blackout period.");
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
					Loading blackout periods...
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
					Managing blackout periods requires a premium license. Existing periods
					remain active.
				</Alert>
			)}

			<Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
				Define global blackout periods (non-working days) to exclude from
				forecasts. Blackout days are shown as annotations on metrics charts
				without altering observed values.
			</Typography>

			<Box sx={{ display: "flex", justifyContent: "flex-end", mb: 2 }}>
				<LicenseTooltip
					canUseFeature={isPremium}
					defaultTooltip="Add a new blackout period"
					premiumExtraInfo="Please obtain a premium license to manage blackout periods."
				>
					<span>
						<Button
							variant="contained"
							startIcon={<AddIcon />}
							onClick={handleOpenAdd}
							disabled={!isPremium}
							data-testid="add-blackout-period-button"
						>
							Add Blackout Period
						</Button>
					</span>
				</LicenseTooltip>
			</Box>

			<TableContainer>
				<Table data-testid="blackout-periods-table">
					<TableHead>
						<TableRow>
							<TableCell>Start</TableCell>
							<TableCell>End</TableCell>
							<TableCell>Description</TableCell>
							{isPremium && <TableCell align="right">Actions</TableCell>}
						</TableRow>
					</TableHead>
					<TableBody>
						{periods.length === 0 ? (
							<TableRow>
								<TableCell colSpan={isPremium ? 4 : 3} align="center">
									<Typography variant="body2" color="text.secondary">
										No blackout periods configured.
									</Typography>
								</TableCell>
							</TableRow>
						) : (
							periods.map((period) => (
								<TableRow
									key={period.id}
									data-testid={`blackout-period-row-${period.id}`}
								>
									<TableCell>{period.start}</TableCell>
									<TableCell>{period.end}</TableCell>
									<TableCell>{period.description}</TableCell>
									{isPremium && (
										<TableCell align="right">
											<IconButton
												onClick={() => handleOpenEdit(period)}
												size="small"
												data-testid={`edit-blackout-${period.id}`}
											>
												<EditIcon />
											</IconButton>
											<IconButton
												onClick={() => setDeleteConfirmId(period.id)}
												size="small"
												color="error"
												data-testid={`delete-blackout-${period.id}`}
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

			{/* Add/Edit Dialog */}
			<Dialog
				open={dialogOpen}
				onClose={handleCloseDialog}
				maxWidth="sm"
				fullWidth
			>
				<DialogTitle>
					{editingPeriod ? "Edit Blackout Period" : "Add Blackout Period"}
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
					<TextField
						label="Start Date"
						type="date"
						value={formData.start}
						onChange={(e) =>
							setFormData({ ...formData, start: e.target.value })
						}
						fullWidth
						sx={{ mt: 2, mb: 2 }}
						slotProps={{
							inputLabel: { shrink: true },
						}}
						data-testid="blackout-start-date"
					/>
					<TextField
						label="End Date"
						type="date"
						value={formData.end}
						onChange={(e) => setFormData({ ...formData, end: e.target.value })}
						fullWidth
						sx={{ mb: 2 }}
						slotProps={{
							inputLabel: { shrink: true },
						}}
						data-testid="blackout-end-date"
					/>
					<TextField
						label="Description (optional)"
						value={formData.description}
						onChange={(e) =>
							setFormData({ ...formData, description: e.target.value })
						}
						fullWidth
						sx={{ mb: 2 }}
						data-testid="blackout-description"
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
						data-testid="save-blackout-period"
					>
						{saving ? <CircularProgress size={20} /> : "Save"}
					</Button>
				</DialogActions>
			</Dialog>

			{/* Delete Confirmation Dialog */}
			<Dialog
				open={deleteConfirmId !== null}
				onClose={() => setDeleteConfirmId(null)}
			>
				<DialogTitle>Delete Blackout Period</DialogTitle>
				<DialogContent>
					<Typography>
						Are you sure you want to delete this blackout period?
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
						data-testid="confirm-delete-blackout"
					>
						Delete
					</Button>
				</DialogActions>
			</Dialog>
		</Box>
	);
};

export default BlackoutPeriodsSettings;
