import CloseIcon from "@mui/icons-material/Close";
import Box from "@mui/material/Box";
import Button from "@mui/material/Button";
import IconButton from "@mui/material/IconButton";
import Paper from "@mui/material/Paper";
import Stack from "@mui/material/Stack";
import Typography from "@mui/material/Typography";
import type React from "react";
import { useCallback, useContext, useEffect, useMemo, useState } from "react";
import { ApiServiceContext } from "../../services/Api/ApiServiceContext";
import type { SurveyNudgeAction } from "../../services/Api/SurveyNudgeService";
import { evaluateNudgeEligibility } from "./nudgeEligibility";

export const SURVEY_URL = "https://letpeople.work/survey";

interface SurveyNudgeProps {
	now?: Date;
}

const SurveyNudge: React.FC<SurveyNudgeProps> = ({ now }) => {
	const { licensingService, systemInfoService, surveyNudgeService } =
		useContext(ApiServiceContext);

	const [isPremium, setIsPremium] = useState<boolean | undefined | null>(
		undefined,
	);
	const [installTimestamp, setInstallTimestamp] = useState<
		string | undefined | null
	>(undefined);
	const [nextEligibleAt, setNextEligibleAt] = useState<string | null>(null);
	const [closed, setClosed] = useState(false);

	useEffect(() => {
		let cancelled = false;

		const load = async () => {
			const [license, systemInfo, nudgeState] = await Promise.all([
				licensingService.getLicenseStatus(),
				systemInfoService.getSystemInfo(),
				surveyNudgeService.getState(),
			]);

			if (cancelled) {
				return;
			}

			setIsPremium(license?.canUsePremiumFeatures);
			setInstallTimestamp(systemInfo?.installTimestamp);
			setNextEligibleAt(nudgeState?.nextEligibleAt ?? null);
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
	}, [licensingService, systemInfoService, surveyNudgeService]);

	const decision = useMemo(
		() =>
			evaluateNudgeEligibility({
				isPremium,
				installTimestamp,
				nextEligibleAt,
				now,
			}),
		[isPremium, installTimestamp, nextEligibleAt, now],
	);

	const recordAndClose = useCallback(
		(action: SurveyNudgeAction) => {
			setClosed(true);
			surveyNudgeService.recordAction(action).catch(() => undefined);
		},
		[surveyNudgeService],
	);

	if (!decision.shouldShow || closed) {
		return null;
	}

	return (
		<Paper
			elevation={6}
			role="region"
			aria-label="Help shape Lighthouse"
			sx={{
				position: "fixed",
				bottom: 16,
				right: 16,
				zIndex: (theme) => theme.zIndex.snackbar,
				maxWidth: 360,
				p: 2,
			}}
		>
			<Box sx={{ display: "flex", justifyContent: "space-between", gap: 1 }}>
				<Typography variant="h6" component="h2">
					Help shape Lighthouse
				</Typography>
				<IconButton
					size="small"
					aria-label="Dismiss"
					onClick={() => recordAndClose("RemindLater")}
				>
					<CloseIcon fontSize="inherit" />
				</IconButton>
			</Box>
			<Typography variant="body2" sx={{ mt: 1 }}>
				Lighthouse never tracks how you use it, so your feedback is the only way
				we learn what to improve. This short survey is completely optional and
				anonymous, and takes about two minutes. As a thank-you you can opt in to
				a free one-month Premium trial at the end.
			</Typography>
			<Stack spacing={1} sx={{ mt: 2 }}>
				<Button
					variant="contained"
					href={SURVEY_URL}
					target="_blank"
					rel="noopener noreferrer"
					onClick={() => recordAndClose("TakeSurvey")}
				>
					Take the survey
				</Button>
				<Button variant="text" onClick={() => recordAndClose("RemindLater")}>
					Remind me later
				</Button>
				<Button variant="text" onClick={() => recordAndClose("NoInterest")}>
					Not interested
				</Button>
			</Stack>
		</Paper>
	);
};

export default SurveyNudge;
