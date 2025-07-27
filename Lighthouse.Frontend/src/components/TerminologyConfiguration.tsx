import {
	Alert,
	Box,
	Button,
	CircularProgress,
	TextField,
	Typography,
} from "@mui/material";
import type React from "react";
import { useCallback, useContext, useEffect, useState } from "react";
import type { ITerminology } from "../models/Terminology";
import { ApiServiceContext } from "../services/Api/ApiServiceContext";
import { useTerminology } from "../services/TerminologyContext";

interface TerminologyConfigurationProps {
	onClose?: () => void;
}

export const TerminologyConfiguration: React.FC<
	TerminologyConfigurationProps
> = ({ onClose }) => {
	const { terminologyService } = useContext(ApiServiceContext);
	const { refetchTerminology } = useTerminology();
	const [terminology, setTerminology] = useState<ITerminology[]>([]);
	const [loading, setLoading] = useState(true);
	const [saving, setSaving] = useState(false);
	const [error, setError] = useState<string | null>(null);
	const [success, setSuccess] = useState<string | null>(null);

	const loadTerminology = useCallback(async () => {
		try {
			setLoading(true);
			setError(null);
			const data = await terminologyService.getAllTerminology();
			setTerminology(data);
		} catch (error) {
			setError("Failed to load terminology configuration");
			console.error("Error loading terminology:", error);
		} finally {
			setLoading(false);
		}
	}, [terminologyService]);

	useEffect(() => {
		loadTerminology();
	}, [loadTerminology]);

	const handleSave = async () => {
		try {
			setSaving(true);
			setError(null);
			setSuccess(null);
			await terminologyService.updateTerminology(terminology);
			setSuccess("Terminology configuration updated successfully");

			// Invalidate terminology context cache to fetch latest data
			refetchTerminology();

			if (onClose) {
				setTimeout(() => onClose(), 1500);
			}
		} catch (error) {
			setError("Failed to save terminology configuration");
			console.error("Error saving terminology:", error);
		} finally {
			setSaving(false);
		}
	};

	const handleInputChange = (key: string, value: string) => {
		setTerminology((prev) =>
			prev.map((term) => (term.key === key ? { ...term, value } : term)),
		);
		setSuccess(null);
	};

	if (loading) {
		return (
			<Box display="flex" justifyContent="center" alignItems="center" p={2}>
				<CircularProgress />
				<Typography variant="body1" sx={{ ml: 2 }}>
					Loading terminology configuration...
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

			{success && (
				<Alert
					severity="success"
					sx={{ mb: 2 }}
					onClose={() => setSuccess(null)}
				>
					{success}
				</Alert>
			)}

			<Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
				Configure the terminology used throughout the application. Changes will
				be applied immediately after saving.
			</Typography>

			<Box sx={{ mb: 3 }}>
				{terminology.map((term) => (
					<Box
						key={term.key}
						sx={{ display: "flex", alignItems: "center", mb: 2, gap: 2 }}
					>
						<TextField
							label={term.defaultValue}
							value={term.value}
							onChange={(e) => handleInputChange(term.key, e.target.value)}
							placeholder={term.description}
							fullWidth
							variant="outlined"
						/>
					</Box>
				))}
			</Box>

			<Box sx={{ display: "flex", justifyContent: "flex-end", gap: 2 }}>
				{onClose && (
					<Button onClick={onClose} variant="outlined" disabled={saving}>
						Cancel
					</Button>
				)}
				<Button
					onClick={handleSave}
					disabled={saving}
					variant="contained"
					color="primary"
				>
					{saving ? (
						<>
							<CircularProgress size={20} sx={{ mr: 1 }} />
							Saving...
						</>
					) : (
						"Save Configuration"
					)}
				</Button>
			</Box>
		</Box>
	);
};
