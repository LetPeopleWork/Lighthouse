import {
	Box,
	Button,
	Dialog,
	DialogActions,
	DialogContent,
	DialogTitle,
	TextField,
	Typography,
} from "@mui/material";
import type React from "react";
import { useCallback, useContext, useEffect, useState } from "react";
import { FeatureSelector } from "../../../../../components/Common/FeatureSelector";
import type { IDelivery } from "../../../../../models/Delivery";
import type { IFeature } from "../../../../../models/Feature";
import type { Portfolio } from "../../../../../models/Portfolio/Portfolio";
import { TERMINOLOGY_KEYS } from "../../../../../models/TerminologyKeys";
import { ApiServiceContext } from "../../../../../services/Api/ApiServiceContext";
import { useTerminology } from "../../../../../services/TerminologyContext";

interface DeliveryCreateModalProps {
	open: boolean;
	portfolio: Portfolio;
	editingDelivery?: IDelivery | null;
	onClose: () => void;
	onSave: (deliveryData: {
		name: string;
		date: string;
		featureIds: number[];
	}) => void;
	onUpdate?: (deliveryData: {
		id: number;
		name: string;
		date: string;
		featureIds: number[];
	}) => void;
}

export const DeliveryCreateModal: React.FC<DeliveryCreateModalProps> = ({
	open,
	portfolio,
	editingDelivery,
	onClose,
	onSave,
	onUpdate,
}) => {
	const { featureService } = useContext(ApiServiceContext);
	const { getTerm } = useTerminology();
	const deliveryTerm = getTerm(TERMINOLOGY_KEYS.DELIVERY);
	const isEditMode = !!editingDelivery;
	const featuresTerm = getTerm(TERMINOLOGY_KEYS.FEATURES);
	const featureTerm = getTerm(TERMINOLOGY_KEYS.FEATURE);
	const [name, setName] = useState("");
	const [date, setDate] = useState("");
	const [selectedFeatureIds, setSelectedFeatureIds] = useState<number[]>([]);
	const [allFeatures, setAllFeatures] = useState<IFeature[]>([]);
	const [errors, setErrors] = useState<{
		name?: string;
		date?: string;
		features?: string;
	}>({});

	useEffect(() => {
		if (open && portfolio.features.length > 0) {
			const featureIds = portfolio.features.map((f) => f.id);
			featureService
				.getFeaturesByIds(featureIds)
				.then((features) => setAllFeatures(features))
				.catch((err) => console.error("Failed to load features:", err));
		}
	}, [open, portfolio.features, featureService]);

	const validateForm = () => {
		const newErrors: typeof errors = {};

		if (!name.trim()) {
			newErrors.name = `${deliveryTerm} name is required`;
		}

		if (!date) {
			newErrors.date = `${deliveryTerm} date is required`;
		} else {
			const selectedDate = new Date(date);
			const today = new Date();
			today.setHours(0, 0, 0, 0);
			selectedDate.setHours(0, 0, 0, 0);

			if (selectedDate <= today) {
				newErrors.date = `${deliveryTerm} date must be in the future`;
			}
		}

		if (selectedFeatureIds.length === 0) {
			newErrors.features = `At least one ${featureTerm.toLowerCase()} must be selected`;
		}

		setErrors(newErrors);
		return Object.keys(newErrors).length === 0;
	};

	const handleSave = () => {
		if (validateForm()) {
			if (isEditMode && editingDelivery && onUpdate) {
				onUpdate({
					id: editingDelivery.id,
					name: name.trim(),
					date,
					featureIds: selectedFeatureIds,
				});
			} else {
				onSave({
					name: name.trim(),
					date,
					featureIds: selectedFeatureIds,
				});
			}
		}
	};

	const resetForm = useCallback(() => {
		setName("");
		setDate("");
		setSelectedFeatureIds([]);
		setErrors({});
	}, []);

	// Initialize form with editing delivery data
	useEffect(() => {
		if (open && editingDelivery) {
			setName(editingDelivery.name);
			setDate(editingDelivery.date.split("T")[0]); // Extract date part from ISO string
			setSelectedFeatureIds(editingDelivery.features || []);
		}
	}, [open, editingDelivery]);

	useEffect(() => {
		if (!open) {
			resetForm();
		}
	}, [open, resetForm]);

	return (
		<Dialog open={open} onClose={onClose} maxWidth="md" fullWidth>
			<DialogTitle>
				{isEditMode ? `Edit ${deliveryTerm}` : `Add ${deliveryTerm}`}
			</DialogTitle>
			<DialogContent>
				<Box sx={{ pt: 1 }}>
					<TextField
						autoFocus
						margin="dense"
						label={`${deliveryTerm} Name`}
						type="text"
						fullWidth
						variant="outlined"
						value={name}
						onChange={(e) => setName(e.target.value)}
						error={!!errors.name}
						helperText={errors.name}
						sx={{ mb: 2 }}
					/>

					<TextField
						margin="dense"
						label={`${deliveryTerm} Date`}
						type="date"
						fullWidth
						variant="outlined"
						value={date}
						onChange={(e) => setDate(e.target.value)}
						error={!!errors.date}
						helperText={errors.date}
						InputLabelProps={{ shrink: true }}
						sx={{ mb: 2 }}
					/>

					<Typography variant="h6" sx={{ mb: 2 }}>
						Select {featuresTerm}
					</Typography>
					{errors.features && (
						<Typography color="error" sx={{ mb: 1 }}>
							{errors.features}
						</Typography>
					)}

					<Box sx={{ height: 300 }}>
						<FeatureSelector
							features={allFeatures}
							selectedFeatureIds={selectedFeatureIds}
							onChange={setSelectedFeatureIds}
							storageKey={`delivery-create-features-${portfolio.id}`}
						/>
					</Box>
				</Box>
			</DialogContent>
			<DialogActions>
				<Button onClick={onClose}>Cancel</Button>
				<Button onClick={handleSave} variant="contained">
					{isEditMode ? "Update" : "Save"}
				</Button>
			</DialogActions>
		</Dialog>
	);
};
