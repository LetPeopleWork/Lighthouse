import CloseIcon from "@mui/icons-material/Close";
import Alert from "@mui/material/Alert";
import Box from "@mui/material/Box";
import Button from "@mui/material/Button";
import IconButton from "@mui/material/IconButton";
import Snackbar from "@mui/material/Snackbar";
import type React from "react";
import { useContext, useEffect, useMemo, useState } from "react";
import { ApiServiceContext } from "../../services/Api/ApiServiceContext";
import { evaluateNudgeEligibility } from "./nudgeEligibility";

export const SURVEY_URL = "https://letpeople.work/survey";

interface SurveyNudgeProps {
	now?: Date;
}

const SurveyNudge: React.FC<SurveyNudgeProps> = ({ now }) => {
	const { licensingService, systemInfoService } = useContext(ApiServiceContext);

	const [isPremium, setIsPremium] = useState<boolean | undefined | null>(
		undefined,
	);
	const [installTimestamp, setInstallTimestamp] = useState<
		string | undefined | null
	>(undefined);
	const [dismissed, setDismissed] = useState(false);

	useEffect(() => {
		let cancelled = false;

		const load = async () => {
			const [license, systemInfo] = await Promise.all([
				licensingService.getLicenseStatus(),
				systemInfoService.getSystemInfo(),
			]);

			if (cancelled) {
				return;
			}

			setIsPremium(license?.canUsePremiumFeatures);
			setInstallTimestamp(systemInfo?.installTimestamp);
		};

		load().catch(() => {
			if (!cancelled) {
				setIsPremium(undefined);
				setInstallTimestamp(undefined);
			}
		});

		return () => {
			cancelled = true;
		};
	}, [licensingService, systemInfoService]);

	const decision = useMemo(
		() => evaluateNudgeEligibility({ isPremium, installTimestamp, now }),
		[isPremium, installTimestamp, now],
	);

	const open = decision.shouldShow && !dismissed;

	return (
		<Snackbar
			open={open}
			anchorOrigin={{ vertical: "bottom", horizontal: "center" }}
		>
			<Alert
				severity="info"
				action={
					<Box sx={{ display: "flex", alignItems: "center" }}>
						<Button
							color="inherit"
							size="small"
							href={SURVEY_URL}
							target="_blank"
							rel="noopener noreferrer"
						>
							Take the survey
						</Button>
						<IconButton
							size="small"
							color="inherit"
							aria-label="Dismiss"
							onClick={() => setDismissed(true)}
						>
							<CloseIcon fontSize="inherit" />
						</IconButton>
					</Box>
				}
			>
				Help shape Lighthouse — share two minutes of feedback.
			</Alert>
		</Snackbar>
	);
};

export default SurveyNudge;
