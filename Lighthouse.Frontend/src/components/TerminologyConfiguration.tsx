import {
	Alert,
	Box,
	Button,
	CircularProgress,
	Paper,
	TextField,
	Typography,
} from "@mui/material";
import type React from "react";
import { useCallback, useContext, useEffect, useState } from "react";
import type { ITerminology } from "../models/Terminology";
import { ApiServiceContext } from "../services/Api/ApiServiceContext";

interface TerminologyConfigurationProps {
	onClose?: () => void;
}

export const TerminologyConfiguration: React.FC<
	TerminologyConfigurationProps
> = ({ onClose }) => {
	const { terminologyService } = useContext(ApiServiceContext);
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
		setTerminology((prev) => ({
			...prev,
			[key]: value,
		}));
		setSuccess(null);
	};

	if (loading) {
		return (
			<Box display="flex" justifyContent="center" alignItems="center" p={4}>
				<CircularProgress />
				<Typography variant="body1" sx={{ ml: 2 }}>
					Loading terminology configuration...
				</Typography>
			</Box>
		);
	}

	return (
		<Paper elevation={2} sx={{ p: 3, maxWidth: 800, mx: "auto" }}>
			<Box mb={3}>
				<Typography variant="h4" component="h2" gutterBottom>
					Terminology Configuration
				</Typography>
				<Typography variant="body1" color="text.secondary">
					Configure the terminology used throughout the application. Changes
					will be applied immediately after saving.
				</Typography>
			</Box>

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

			<Box sx={{ mb: 3 }}>
				{Object.entries(terminology).map(([key, value]) => (
					<Box
						key={key}
						sx={{ display: "flex", alignItems: "center", mb: 2, gap: 2 }}
					>
						<TextField
							label={key}
							value={value}
							onChange={(e) => handleInputChange(key, e.target.value)}
							placeholder={`Enter value for ${key}`}
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
		</Paper>
	);
};
