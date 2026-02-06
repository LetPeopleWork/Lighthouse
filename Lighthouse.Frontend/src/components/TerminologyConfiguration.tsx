import {
	Alert,
	Box,
	Button,
	CircularProgress,
	Link,
	TextField,
	Typography,
} from "@mui/material";
import type React from "react";
import { useCallback, useContext, useEffect, useState } from "react";
import { useLicenseRestrictions } from "../hooks/useLicenseRestrictions";
import type { ITerminology } from "../models/Terminology";
import { ApiServiceContext } from "../services/Api/ApiServiceContext";
import { useTerminology } from "../services/TerminologyContext";
import FeedbackDialog from "./App/Header/FeedbackDialog";
import { LicenseTooltip } from "./App/License/LicenseToolTip";

interface TerminologyConfigurationProps {
	onClose?: () => void;
}

export const TerminologyConfiguration: React.FC<
	TerminologyConfigurationProps
> = ({ onClose }) => {
	const { terminologyService } = useContext(ApiServiceContext);
	const { refetchTerminology } = useTerminology();
	const { licenseStatus } = useLicenseRestrictions();
	const [terminology, setTerminology] = useState<ITerminology[]>([]);
	const [loading, setLoading] = useState(true);
	const [saving, setSaving] = useState(false);
	const [error, setError] = useState<string | null>(null);
	const [success, setSuccess] = useState<string | null>(null);
	const [feedbackDialogOpen, setFeedbackDialogOpen] = useState(false);

	const isPremium = licenseStatus?.canUsePremiumFeatures ?? true;

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

			{!isPremium && (
				<Alert severity="warning" sx={{ mb: 2 }}>
					<Typography variant="body2">
						Customizing terminology requires a premium license. You can view the
						current terminology below, but editing is restricted to premium
						users.
					</Typography>
				</Alert>
			)}
			<Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
				Use your own terminology so it fits your use case. The naming will be
				used throughout the application - changes will be applied immediately
				after saving. The defaults are aligned with the terminology in the
				Kanban Guide wherever applicable.
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
							disabled={!isPremium}
							slotProps={{
								input: {
									readOnly: !isPremium,
								},
							}}
						/>
					</Box>
				))}
			</Box>

			<Alert severity="info" sx={{ mb: 3 }}>
				<Typography variant="body2">
					<strong>
						Found a place where we don't use your configured terminology?
					</strong>
					<br />
					We'd love to hear from you!{" "}
					<Link
						component="button"
						variant="body2"
						onClick={() => setFeedbackDialogOpen(true)}
						sx={{ cursor: "pointer" }}
					>
						Please reach out
					</Link>{" "}
					if you spot inconsistencies or have requests for making additional
					terminology configurable.
				</Typography>
			</Alert>

			<FeedbackDialog
				open={feedbackDialogOpen}
				onClose={() => setFeedbackDialogOpen(false)}
			/>

			<Box sx={{ display: "flex", justifyContent: "flex-end", gap: 2 }}>
				{onClose && (
					<Button onClick={onClose} variant="outlined" disabled={saving}>
						Cancel
					</Button>
				)}
				<LicenseTooltip
					canUseFeature={isPremium}
					defaultTooltip=""
					premiumExtraInfo="Please obtain a premium license to customize terminology."
				>
					<span>
						<Button
							onClick={handleSave}
							disabled={saving || !isPremium}
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
					</span>
				</LicenseTooltip>
			</Box>
		</Box>
	);
};
