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
import {
	claimPromptSlot,
	slotIsHeldByAnother,
} from "../../services/UsageData/promptSession";
import { evaluateNudgeEligibility } from "./nudgeEligibility";

export const SURVEY_URL = "https://letpeople.work/survey";

interface SurveyNudgeProps {
	now?: Date;
}

/**
 * One unsolicited prompt per session, and this popup is no longer the only one.
 *
 * The usage data dialog asks for consent, and consent collected alongside an unrelated request is
 * not freely given - so the two may not share a session. Both are relevant to Community users on
 * overlapping clocks, which makes meeting each other the normal case rather than an edge one.
 */
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

	// Only once this popup has decided it is actually appearing. Claiming while ineligible would
	// let a fortnight-old instance silence the usage data dialog on behalf of a nudge nobody sees.
	//
	// From an effect rather than during rendering, because a claim is a write and React may throw
	// away a render it never commits - which would leave the slot held by a popup nobody saw. The
	// ordering that settles a genuine tie survives the move: this sits above the usage data
	// component in the tree, so its effect runs first and takes the slot.
	const wantsToShow = decision.shouldShow && !closed;

	// Stryker disable next-line ArrayDeclaration: the dependency list only shows itself when the
	// dependency changes, and a test contrived to change it would be watching React re-run an
	// effect rather than anything this popup promises.
	useEffect(() => {
		if (wantsToShow) {
			claimPromptSlot("survey-nudge");
		}
	}, [wantsToShow]);

	if (!wantsToShow || slotIsHeldByAnother("survey-nudge")) {
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
				Usage data can tell us which parts of Lighthouse get opened, and nothing
				about whether they helped. Your answers are the only way we learn what
				is missing and what gets in the way. This short survey is completely
				optional and anonymous, and takes about two minutes. As a thank-you you
				can opt in to a free one-month Premium trial at the end.
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
