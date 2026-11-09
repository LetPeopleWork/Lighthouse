import Alert from "@mui/material/Alert";
import Box from "@mui/material/Box";
import Button from "@mui/material/Button";
import CircularProgress from "@mui/material/CircularProgress";
import Dialog from "@mui/material/Dialog";
import DialogActions from "@mui/material/DialogActions";
import DialogContent from "@mui/material/DialogContent";
import DialogContentText from "@mui/material/DialogContentText";
import DialogTitle from "@mui/material/DialogTitle";
import FormControlLabel from "@mui/material/FormControlLabel";
import Switch from "@mui/material/Switch";
import Typography from "@mui/material/Typography";
import { useQuery } from "@tanstack/react-query";
import type React from "react";
import { useContext, useState } from "react";
import type { EveryoneSignedIn } from "../../../models/Authorization/RbacModels";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import { ApiError } from "../../../services/Api/ApiError";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import { UsageDataEventName } from "../../../services/Api/UsageDataService";
import { useTerminology } from "../../../services/TerminologyContext";
import { useUsageDataReporter } from "../../../services/UsageData/usageDataReporter";
import { fireAndForget } from "../../../utils/fireAndForget";

const CONFLICT_STATUS = 409;

const SWITCH_CONFLICT_COPY =
	"This was changed by someone else since you opened it. The latest value is shown below; change it again if you still need to.";

const SWITCH_SAVE_FAILED_COPY =
	"Could not save this setting. Reload the page to try again.";

const isConflict = (error: unknown): boolean =>
	error instanceof ApiError && error.code === CONFLICT_STATUS;

/** The switch that lets everyone signed in view all Teams and Portfolios. Turning it on asks first; turning it off does not. */
const EveryoneSignedInSection: React.FC = () => {
	const { rbacService } = useContext(ApiServiceContext);
	const { getTerm } = useTerminology();
	const reportUsage = useUsageDataReporter();
	const [confirming, setConfirming] = useState(false);
	const [saving, setSaving] = useState(false);
	const [saveProblem, setSaveProblem] = useState<string>();
	const theSwitch = useQuery<EveryoneSignedIn>({
		queryKey: ["everyoneSignedIn"],
		queryFn: () => rbacService.getEveryoneSignedIn(),
		retry: false,
		staleTime: 0,
	});

	const teams = getTerm(TERMINOLOGY_KEYS.TEAMS);
	const portfolios = getTerm(TERMINOLOGY_KEYS.PORTFOLIOS);
	const switchLabel = `Everyone signed in can view all ${teams} and ${portfolios}`;

	const save = async (current: EveryoneSignedIn, everyoneCanView: boolean) => {
		setSaving(true);
		setSaveProblem(undefined);
		try {
			await rbacService.setEveryoneSignedIn({
				everyoneCanView,
				concurrencyToken: current.concurrencyToken,
			});
			if (everyoneCanView) {
				reportUsage({ name: UsageDataEventName.SystemWideAccessGranted });
			}
		} catch (error) {
			setSaveProblem(
				isConflict(error) ? SWITCH_CONFLICT_COPY : SWITCH_SAVE_FAILED_COPY,
			);
		} finally {
			await theSwitch.refetch();
			setSaving(false);
		}
	};

	const renderSwitch = () => {
		if (theSwitch.isError) {
			return (
				<Alert severity="warning">
					Could not load this setting. Reload the page to try again.
				</Alert>
			);
		}

		if (theSwitch.isPending) {
			return (
				<Box sx={{ display: "flex", alignItems: "center", gap: 1 }}>
					<CircularProgress size={16} />
					<Typography variant="body2" color="text.disabled">
						{switchLabel}
					</Typography>
				</Box>
			);
		}

		const current = theSwitch.data;
		return (
			<>
				<FormControlLabel
					label={switchLabel}
					control={
						<Switch
							checked={current.everyoneCanView}
							disabled={saving || theSwitch.isFetching}
							onChange={(_event, checked) =>
								checked
									? setConfirming(true)
									: fireAndForget(save(current, false))
							}
						/>
					}
				/>
				<Typography variant="body2" color="text.secondary">
					{current.everyoneCanView
						? `On: everyone signed in can view all ${teams} and ${portfolios}.`
						: "Off: people see only what they are given."}
				</Typography>
				<Dialog
					open={confirming}
					onClose={() => setConfirming(false)}
					aria-labelledby="everyone-signed-in-confirm-title"
				>
					<DialogTitle id="everyone-signed-in-confirm-title">
						{`Let everyone signed in view all ${teams} and ${portfolios}?`}
					</DialogTitle>
					<DialogContent>
						<DialogContentText>
							{`Everyone who can sign in, now and later, can read every ${getTerm(TERMINOLOGY_KEYS.TEAM)} and ${getTerm(TERMINOLOGY_KEYS.PORTFOLIO)}, and can vote and comment on ${getTerm(TERMINOLOGY_KEYS.REFINEMENT)}.`}
						</DialogContentText>
					</DialogContent>
					<DialogActions>
						<Button onClick={() => setConfirming(false)}>Cancel</Button>
						<Button
							variant="contained"
							onClick={() => {
								setConfirming(false);
								fireAndForget(save(current, true));
							}}
						>
							Turn on
						</Button>
					</DialogActions>
				</Dialog>
			</>
		);
	};

	return (
		<Box sx={{ mt: 4 }}>
			<Typography variant="h6" sx={{ mb: 1 }}>
				Everyone signed in
			</Typography>
			{saveProblem && (
				<Alert severity="warning" sx={{ mb: 1 }}>
					{saveProblem}
				</Alert>
			)}
			{renderSwitch()}
		</Box>
	);
};

export default EveryoneSignedInSection;
