import DownloadIcon from "@mui/icons-material/Download";
import GitHubIcon from "@mui/icons-material/GitHub";
import UpdateIcon from "@mui/icons-material/Update";
import {
	Alert,
	Box,
	Button,
	CircularProgress,
	Dialog,
	DialogActions,
	DialogContent,
	DialogTitle,
	List,
	ListItem,
	Link as MuiLink,
	Typography,
} from "@mui/material";
import Grid from "@mui/material/Grid";
import type React from "react";
import Markdown from "react-markdown";
import remarkGfm from "remark-gfm";
import type { ILighthouseRelease } from "../../../models/LighthouseRelease/LighthouseRelease";
import InputGroup from "../../Common/InputGroup/InputGroup";

interface LatestReleaseInformationDialogProps {
	open: boolean;
	onClose: () => void;
	newReleases: ILighthouseRelease[] | null;
	isUpdateSupported: boolean;
	isInstalling: boolean;
	installError: string | null;
	installSuccess: boolean;
	onInstallUpdate: () => void;
}

const LatestReleaseInformationDialog: React.FC<
	LatestReleaseInformationDialogProps
> = ({
	open,
	onClose,
	newReleases,
	isUpdateSupported,
	isInstalling,
	installError,
	installSuccess,
	onInstallUpdate,
}) => {
		return (
			<Dialog open={open} onClose={onClose} maxWidth="md" fullWidth>
				<DialogTitle>Update Available</DialogTitle>
				<DialogContent>
					{isUpdateSupported && (
						<Box display="flex" alignItems="center" mb={2}>
							<UpdateIcon color="primary" sx={{ marginRight: 1 }} />
							<Typography variant="body1">
								Click "Install Update" to automatically download and install the
								latest version. The application will restart automatically after
								the update is complete.
							</Typography>
						</Box>
					)}

					{installError && (
						<Alert severity="error" sx={{ mb: 2 }}>
							{installError}
						</Alert>
					)}

					{installSuccess && (
						<Alert severity="success" sx={{ mb: 2 }}>
							Update installed successfully! The application is restarting...
						</Alert>
					)}

					{isInstalling && (
						<Box display="flex" alignItems="center" mb={2}>
							<CircularProgress size={20} sx={{ marginRight: 1 }} />
							<Typography variant="body1">
								Installing update... Please do not close this window.
							</Typography>
						</Box>
					)}

					<Box display="flex" alignItems="center" mb={2}>
						<DownloadIcon color="primary" sx={{ marginRight: 1 }} />
						<Typography variant="body1">
							To update Lighthouse manually, please consult the{" "}
							<MuiLink
								href="https://docs.lighthouse.letpeople.work/Installation/installation.html"
								target="_blank"
								rel="noopener noreferrer"
								sx={{ marginLeft: 0.5 }}
							>
								documentation for your respective system
							</MuiLink>
							.
						</Typography>
					</Box>

					{newReleases?.map((release, index) => (
						<InputGroup
							key={release.name}
							title={release.name}
							initiallyExpanded={index === 0}
						>
							<Grid size={{ xs: 12 }}>
								<Typography variant="body2">
									<Markdown remarkPlugins={[remarkGfm]}>
										{release.highlights}
									</Markdown>
								</Typography>

								<Typography variant="subtitle1">Downloads:</Typography>
								<List>
									{release.assets.map((asset) => (
										<ListItem key={asset.name} sx={{ padding: 0 }}>
											<MuiLink
												href={asset.link}
												target="_blank"
												rel="noopener noreferrer"
											>
												{asset.name}
											</MuiLink>
										</ListItem>
									))}
								</List>

								<Typography variant="body1" marginTop={2}>
									<GitHubIcon />
									<MuiLink
										href={release.link}
										target="_blank"
										rel="noopener noreferrer"
										marginLeft={2}
									>
										See GitHub for more details
									</MuiLink>
								</Typography>
							</Grid>
						</InputGroup>
					))}
				</DialogContent>
				<DialogActions>
					{isUpdateSupported && !isInstalling && !installSuccess && (
						<Button
							onClick={onInstallUpdate}
							color="primary"
							variant="contained"
							startIcon={<UpdateIcon />}
							disabled={isInstalling}
						>
							Install Update
						</Button>
					)}
					<Button onClick={onClose} color="secondary" variant="outlined">
						Close
					</Button>
				</DialogActions>
			</Dialog>
		);
	};

export default LatestReleaseInformationDialog;
