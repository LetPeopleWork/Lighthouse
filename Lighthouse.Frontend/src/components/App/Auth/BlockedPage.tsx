import Button from "@mui/material/Button";
import CircularProgress from "@mui/material/CircularProgress";
import Typography from "@mui/material/Typography";
import type React from "react";
import { useRef, useState } from "react";
import type { ILicensingService } from "../../../services/Api/LicensingService";
import AuthPageLayout from "./AuthPageLayout";

interface BlockedPageProps {
	licensingService: ILicensingService;
	onLicenseImported: () => void;
	onLogout: () => void;
}

const BlockedPage: React.FC<BlockedPageProps> = ({
	licensingService,
	onLicenseImported,
	onLogout,
}) => {
	const [isUploading, setIsUploading] = useState(false);
	const [uploadError, setUploadError] = useState<string | null>(null);
	const fileInputRef = useRef<HTMLInputElement>(null);

	const handleFileUpload = async (
		event: React.ChangeEvent<HTMLInputElement>,
	) => {
		const file = event.target.files?.[0];
		if (!file) return;

		if (!file.name.toLowerCase().endsWith(".json")) {
			setUploadError("Please select a JSON file");
			return;
		}

		setIsUploading(true);
		setUploadError(null);

		try {
			await licensingService.importLicense(file);
			onLicenseImported();
		} catch {
			setUploadError(
				"License could not be loaded. Make sure you upload a valid license file.",
			);
		} finally {
			setIsUploading(false);
			if (fileInputRef.current) {
				fileInputRef.current.value = "";
			}
		}
	};

	const handleUploadClick = () => {
		fileInputRef.current?.click();
	};

	return (
		<AuthPageLayout testId="blocked-page">
			<Typography variant="h6" color="warning.main">
				Premium License Required
			</Typography>
			<Typography
				variant="body1"
				color="text.secondary"
				sx={{ textAlign: "center" }}
			>
				Authentication is a Premium feature. Your license is missing or expired.
				Upload a valid Premium license to continue, or disable authentication in
				the server configuration to restore anonymous access.
			</Typography>

			<input
				ref={fileInputRef}
				type="file"
				accept=".json"
				style={{ display: "none" }}
				onChange={handleFileUpload}
				data-testid="license-file-input"
			/>

			<Button
				variant="contained"
				size="large"
				onClick={handleUploadClick}
				disabled={isUploading}
				data-testid="upload-license-button"
			>
				{isUploading ? (
					<CircularProgress size={24} color="inherit" />
				) : (
					"Upload License"
				)}
			</Button>

			{uploadError && (
				<Typography
					variant="body2"
					color="error"
					data-testid="upload-error"
					sx={{ textAlign: "center" }}
				>
					{uploadError}
				</Typography>
			)}

			<Button
				variant="text"
				size="small"
				onClick={onLogout}
				data-testid="blocked-logout-button"
			>
				Sign Out
			</Button>
		</AuthPageLayout>
	);
};

export default BlockedPage;
